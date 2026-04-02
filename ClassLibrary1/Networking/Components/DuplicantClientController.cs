using ONI_MP.DebugTools;
using ONI_MP.Networking.Packets.Core;
using ONI_MP.Networking.Packets.DuplicantActions;
using Shared.Profiling;
using System.Collections.Generic;
using UnityEngine;

namespace ONI_MP.Networking.Components
{
	public class DuplicantClientController : KMonoBehaviour
	{
		[MyCmpGet] private Navigator navigator;
		[MyCmpGet] private KBatchedAnimController animController;

		private bool isTransitioning;

		private readonly Queue<NavigatorTransitionPacket> buffer = new Queue<NavigatorTransitionPacket>(16);
		private const int MaxBufferSize = 10;
		private const float BufferTargetDelay = 0.08f;
		private bool receivedFirstPacket;
		private float firstPacketTime;
		private bool playbackStarted;

		private const float CorrectionSnapDistance = 3f;
		private NavType stopNavType;
		private bool pendingStop;

		public override void OnSpawn()
		{
			using var _ = Profiler.Scope();
			base.OnSpawn();

			if (!MultiplayerSession.InSession || MultiplayerSession.IsHost)
			{
				enabled = false;
				return;
			}

			if (navigator == null || animController == null)
			{
				enabled = false;
				return;
			}
		}

		private void Update()
		{
			using var _ = Profiler.Scope();

			if (!MultiplayerSession.InSession || MultiplayerSession.IsHost)
				return;

			if (isTransitioning && navigator != null && navigator.transitionDriver != null)
			{
				navigator.transitionDriver.UpdateTransition(Time.deltaTime);

				if (navigator.transitionDriver.GetTransition == null)
				{
					isTransitioning = false;
				}
			}

			if (!isTransitioning)
			{
				TryDequeueAndPlay();
			}
		}

		public void OnTransitionReceived(NavigatorTransitionPacket packet)
		{
			using var _ = Profiler.Scope();

			if (navigator == null || navigator.transitionDriver == null)
				return;

			pendingStop = false;

			if (!receivedFirstPacket)
			{
				receivedFirstPacket = true;
				firstPacketTime = Time.unscaledTime;
			}

			if (buffer.Count >= MaxBufferSize)
			{
				buffer.Clear();
				playbackStarted = true;
				PlayTransition(packet);
				return;
			}

			buffer.Enqueue(packet);
		}

		private void TryDequeueAndPlay()
		{
			using var _ = Profiler.Scope();

			if (buffer.Count == 0)
			{
				if (pendingStop)
				{
					ApplyStop();
				}
				return;
			}

			if (!playbackStarted)
			{
				float timeSinceFirst = Time.unscaledTime - firstPacketTime;
				if (timeSinceFirst < BufferTargetDelay && buffer.Count < 3)
					return;

				playbackStarted = true;
			}

			PlayTransition(buffer.Dequeue());
		}

		private void PlayTransition(NavigatorTransitionPacket packet)
		{
			using var _ = Profiler.Scope();

			if (packet == null || navigator == null || navigator.transitionDriver == null)
			{
				DebugConsole.LogError($"[Duplicate:{gameObject.name}/DuplicateClientController}] Invalid packet or missing components. Packet: {packet}, Navigator: {navigator}, TransitionDriver: {navigator?.transitionDriver}", false);
                return;
			}

            if (isTransitioning)
			{
				navigator.transitionDriver.EndTransition();
			}

			float drift = Vector3.Distance(transform.position, packet.SourcePosition);
			if (drift > 0.15f)
			{
				transform.SetPosition(packet.SourcePosition);
			}

			navigator.SetCurrentNavType((NavType)packet.StartNavType);

			navigator.transitionDriver.BeginTransition(
				navigator,
				new NavGrid.Transition
				{
					x = packet.TransitionX,
					y = packet.TransitionY,
					isLooping = packet.IsLooping,
					useXOffset = packet.UseXOffset,
					start = (NavType)packet.StartNavType,
					end = (NavType)packet.EndNavType,
					anim = packet.Anim,
					preAnim = packet.PreAnim,
					animSpeed = packet.AnimSpeed
				},
				packet.Speed
			);

			isTransitioning = navigator.transitionDriver.GetTransition != null;
		}

		public void OnStopReceived(NavType navType)
		{
			using var _ = Profiler.Scope();

			if (navigator == null)
				return;

			pendingStop = true;
			stopNavType = navType;
			buffer.Clear();

			if (!isTransitioning)
			{
				ApplyStop();
			}
		}

		private void ApplyStop()
		{
			using var _ = Profiler.Scope();

			pendingStop = false;

			if (isTransitioning)
			{
				navigator.transitionDriver.EndTransition();
				isTransitioning = false;
			}

			navigator.SetCurrentNavType(stopNavType);

			HashedString idleAnim = navigator.NavGrid.GetIdleAnim(stopNavType);
			if (animController != null)
			{
				animController.Play(idleAnim, KAnim.PlayMode.Loop);
			}
		}

		public void OnPositionCorrection(Vector3 serverPosition)
		{
			using var _ = Profiler.Scope();

			if (isTransitioning)
				return;

			float error = Vector3.Distance(transform.position, serverPosition);
			if (error > CorrectionSnapDistance)
			{
				transform.SetPosition(serverPosition);
			}
		}

		public void OnStateReceived(DuplicantActionState state, int targetCell, string animName, float animElapsedTime, bool isWorking)
		{
			using var _ = Profiler.Scope();

			if (isTransitioning)
				return;

			if (isWorking && !string.IsNullOrEmpty(animName) && animController != null)
			{
				animController.Play(new HashedString(animName), KAnim.PlayMode.Loop);
			}
		}
	}
}

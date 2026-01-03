using ONI_MP.DebugTools;
using ONI_MP.Networking.Packets.DuplicantActions;
using System.Collections.Generic;
using UnityEngine;

namespace ONI_MP.Networking.Components
{
	/// <summary>
	/// Controls client-side duplicant puppets. Receives position and animation data from the host
	/// and applies them smoothly, overriding any local animation/movement logic.
	/// </summary>
	public class DuplicantClientController : KMonoBehaviour
	{
		// Position interpolation
		private Vector3 targetPosition;
		private Vector3 previousPosition;
		private Vector3 velocity;
		private float interpolationTime;
		private float lastUpdateTime;
		private bool hasReceivedFirstPosition;

		// Animation control
		private KAnimControllerBase animController;
		private HashedString currentAnim;
		private KAnim.PlayMode currentMode;
		private bool animDirty;
		private Queue<AnimCommand> animQueue = new Queue<AnimCommand>();

		// Smooth movement settings
		private const float InterpolationDuration = 0.1f; // Increased buffer (was 0.06f) to smooth out jitter
		private const float MaxExtrapolationTime = 0.15f; // reduced to prevent overshooting
		private const float TeleportThreshold = 3f; // Reduced threshold to snap sooner if desync is large

		// Animation settings
		private HashedString walkAnim = new HashedString("walk_loop");
		private HashedString idleAnim = new HashedString("idle_loop");

		// Tracked state
		private bool isMoving;
		private bool facingLeft;
		private NavType currentNavType = NavType.Floor;
		private DuplicantActionState currentActionState = DuplicantActionState.Idle;
		private int currentTargetCell = -1;
		private bool isCurrentlyWorking;

		private struct AnimCommand
		{
			public HashedString AnimHash;
			public KAnim.PlayMode Mode;
			public float Speed;
			public bool IsQueue;
		}

		public override void OnSpawn()
		{
			base.OnSpawn();

			// Only active on clients
			if (!MultiplayerSession.InSession || MultiplayerSession.IsHost)
			{
				enabled = false;
				return;
			}

			animController = GetComponent<KAnimControllerBase>();
			if (animController == null)
			{
				DebugConsole.LogWarning($"[DuplicantClientController] {gameObject.name} missing KAnimControllerBase");
				enabled = false;
				return;
			}

			targetPosition = transform.position;
			previousPosition = transform.position;
			hasReceivedFirstPosition = false;

			DebugConsole.Log($"[DuplicantClientController] Initialized for {gameObject.name}");
		}

		private void Update()
		{
			if (!MultiplayerSession.InSession || MultiplayerSession.IsHost)
				return;

			UpdatePosition();
			UpdateAnimation();
		}

		private void LateUpdate()
		{
			// Process any queued animations
			ProcessAnimationQueue();
		}

		/// <summary>
		/// Called when receiving an EntityPositionPacket from the host
		/// </summary>
		public void OnPositionReceived(Vector3 newPosition, Vector3 newVelocity, bool newFacingLeft, NavType navType)
		{
			float timeSinceLastUpdate = Time.time - lastUpdateTime;
			lastUpdateTime = Time.time;

			// Calculate velocity if not provided
			if (newVelocity == Vector3.zero && hasReceivedFirstPosition)
			{
				velocity = (newPosition - targetPosition) / Mathf.Max(timeSinceLastUpdate, 0.01f);
			}
			else
			{
				velocity = newVelocity;
			}

			// Persistence: If we are currently working, ignore small velocity jitters
			// This prevents "starts then stops" behavior where a tiny velocity update interrupts the work loop
			if (isCurrentlyWorking && currentTargetCell != -1)
			{
				// If velocity is small, assume we should stay put at the work pos
				if (velocity.sqrMagnitude < 0.5f)
				{
					velocity = Vector3.zero;
					// Keep using our snapped target which should be the workable pos
				}
			}

			previousPosition = hasReceivedFirstPosition ? transform.position : newPosition;
			targetPosition = newPosition;
			facingLeft = newFacingLeft;
			currentNavType = navType;
			interpolationTime = 0f;
			hasReceivedFirstPosition = true;

			// Check if we're moving
			isMoving = velocity.sqrMagnitude > 0.01f;

			// Update facing direction
			if (animController != null)
			{
				animController.FlipX = facingLeft;
			}

			// Apply appropriate movement animation based on nav type
			ApplyMovementAnimation();
		}

		/// <summary>
		/// Called when receiving a PlayAnimPacket from the host
		/// </summary>
		public void OnAnimationReceived(HashedString animHash, KAnim.PlayMode mode, float speed, bool isQueue)
		{
			var cmd = new AnimCommand
			{
				AnimHash = animHash,
				Mode = mode,
				Speed = speed,
				IsQueue = isQueue
			};

			if (isQueue)
			{
				animQueue.Enqueue(cmd);
			}
			else
			{
				// Clear queue and set as current
				animQueue.Clear();
				currentAnim = animHash;
				currentMode = mode;
				animDirty = true;
			}
		}

		/// <summary>
		/// Called when receiving multiple animations from the host
		/// </summary>
		public void OnAnimationsReceived(HashedString[] animHashes, KAnim.PlayMode mode)
		{
			return;

			if (animController == null || animHashes == null || animHashes.Length == 0)
				return;

			animQueue.Clear();
			animController.Play(animHashes, mode);
			currentAnim = animHashes[0];
			currentMode = mode;
			animDirty = false;
		}

		private void UpdatePosition()
		{
			if (!hasReceivedFirstPosition)
				return;

			float timeSinceUpdate = Time.time - lastUpdateTime;
			interpolationTime += Time.deltaTime;

			// Calculate interpolation factor
			float t = Mathf.Clamp01(interpolationTime / InterpolationDuration);

			Vector3 newPos;

			// Check if we should teleport
			float distanceToTarget = Vector3.Distance(transform.position, targetPosition);
			if (distanceToTarget > TeleportThreshold)
			{
				newPos = targetPosition;
				t = 1f;
			}
			else if (timeSinceUpdate > MaxExtrapolationTime)
			{
				// Haven't received an update in a while, stop at target to avoid drifting too far
				newPos = targetPosition;
			}
			else if (t >= 1f)
			{
				// Extrapolate beyond target position using velocity
				// Damped velocity to prevent massive overshooting
				float extrapolationT = (timeSinceUpdate - InterpolationDuration) / InterpolationDuration;
				extrapolationT = Mathf.Clamp01(extrapolationT);
				newPos = targetPosition + (velocity * 0.8f) * Time.deltaTime; // 0.8f damping

				// If we are extrapolating, we might be drifting. 
				// We don't update transform immediately here if we want to "slide" into the next packet
				// But for now, let's just use the damped newPos
			}
			else
			{
				// Normal interpolation
				// Use a non-linear curve for smoother arrival
				float smoothT = Mathf.SmoothStep(0f, 1f, t);
				newPos = Vector3.Lerp(previousPosition, targetPosition, smoothT);
			}

			// Apply position
			transform.SetPosition(newPos);
		}

		private void UpdateAnimation()
		{
			return;
			if (animController == null)
				return;

			if (animDirty && currentAnim.IsValid)
			{
				animController.Play(currentAnim, currentMode);
				ForceAnimControllerUpdate();
				animDirty = false;
			}
		}

		private void ProcessAnimationQueue()
		{
			return;
			if (animController == null)
				return;

			// Check if current anim is stopped before dequeuing next
			while (animQueue.Count > 0 && animController.IsStopped())
			{
				var cmd = animQueue.Dequeue();
				animController.Queue(cmd.AnimHash, cmd.Mode, cmd.Speed, 0f);
			}
		}

		private void ApplyMovementAnimation()
		{
			return;
			if (animController == null)
				return;

			// Only apply movement animations if we don't have a specific animation playing
			if (animDirty || animQueue.Count > 0)
				return;

			HashedString targetAnim = isMoving ? GetNavTypeAnim(currentNavType) : idleAnim;

			// Only change if different - CurrentAnim returns the anim name hash
			if (animController.currentAnim != targetAnim)
			{
				animController.Play(targetAnim, KAnim.PlayMode.Loop);
				ForceAnimControllerUpdate();
			}
		}

		private HashedString GetNavTypeAnim(NavType navType)
		{
			if (navType == NavType.Ladder)
				return new HashedString("climb_loop");
			if (navType == NavType.Pole)
				return new HashedString("fireman_loop");
			if (navType == NavType.Tube)
				return new HashedString("tube_loop");
			if (navType == NavType.Hover)
				return new HashedString("hover_loop");
			if (navType == NavType.Ceiling)
				return new HashedString("ceiling_loop");
			if (navType == NavType.Swim)
				return new HashedString("swim_loop");
			return walkAnim;
		}

		private void ForceAnimControllerUpdate()
		{
			if (animController is KBatchedAnimController kbac)
			{
				try
				{
					kbac.SetVisiblity(true);

					// Force rebuild and enable updates
					kbac.forceRebuild = true;
					kbac.SuspendUpdates(false);
					kbac.ConfigureUpdateListener();
				}
				catch (System.Exception ex)
				{
					DebugConsole.LogError($"[DuplicantClientController] ForceAnimUpdate failed: {ex}");
				}
			}
		}

		/// <summary>
		/// Called when receiving a DuplicantStatePacket from the host
		/// </summary>
		public void OnStateReceived(DuplicantActionState state, int targetCell, string animName, float animElapsedTime, bool isWorking, string heldSymbol)
		{
			currentActionState = state;
			currentTargetCell = targetCell;
			isCurrentlyWorking = isWorking;

			// Apply held item override (simple gun check for now)
			if (heldSymbol == "gun")
			{
				EquipGun();
			}
			else
			{
				UnequipGun();
			}

			bool specificAnimSet = false;

			// If we received a specific animation name, play it
			if (!string.IsNullOrEmpty(animName))
			{
				var animHash = new HashedString(animName);
				if (currentAnim != animHash)
				{
					currentAnim = animHash;
					currentMode = isWorking ? KAnim.PlayMode.Loop : KAnim.PlayMode.Once;
					animDirty = true;
				}
				specificAnimSet = true;
			}

			// Only apply generic action animation if we didn't set a specific one
			// AND the specific one isn't just "idle" (unless we really are idle)
			if (isWorking && animController != null && state != DuplicantActionState.Walking && !specificAnimSet)
			{
				ApplyActionAnimation(state);

				// Snap to workable if valid
				if (currentTargetCell != -1)
				{
					SnapToWorkable(currentTargetCell);
				}
			}
			// Still snap to workable if we are working, even if we have a specific animation
			else if (isWorking && currentTargetCell != -1)
			{
				SnapToWorkable(currentTargetCell);
			}
		}

		private bool hasEquippedGun = false;
		private void EquipGun()
		{
			if (hasEquippedGun) return;
			hasEquippedGun = true;

			var symbolOverride = GetComponent<SymbolOverrideController>();
			if (symbolOverride != null)
			{
				// We need to verify what symbol to use. 
				// "build_tool" or "dig_tool" are common
				// Assets.GetAnim("build_tool_kanim")
				KAnimFile gunAnim = Assets.GetAnim("gun_kanim");
				if (gunAnim != null)
				{
					// Typically mapped to snapto_pivot
					var symbol = gunAnim.GetData().build.GetSymbol("gun");
					if (symbol != null)
					{
						symbolOverride.AddSymbolOverride("snapto_pivot", symbol, 0); // priority 0?
					}
				}
			}
		}

		private void UnequipGun()
		{
			if (!hasEquippedGun) return;
			hasEquippedGun = false;

			var symbolOverride = GetComponent<SymbolOverrideController>();
			if (symbolOverride != null)
			{
				symbolOverride.RemoveSymbolOverride("snapto_pivot", 0);
			}
		}

		private void SnapToWorkable(int cell)
		{
			if (!Grid.IsValidCell(cell)) return;

			// Try to find a workable at the cell
			// We usually look for buildings
			GameObject go = Grid.Objects[cell, (int)Grid.SceneLayer.Building];
			if (go == null)
			{
				return;
			}

			var workable = go.GetComponent<Workable>();
			if (workable != null)
			{
				// Use Grid.CellToPosCBC to get the standing position for the cell
				// SceneLayer.Move ensures correct Z-sorting for dupes
				Vector3 workPos = Grid.CellToPosCBC(cell, Grid.SceneLayer.Move);

				// Keep our Z if needed, or trust CellToPosCBC. Usually CBC gives correct visual pos.
				Vector3 newPos = new Vector3(workPos.x, workPos.y, transform.position.z);

				// Update targets so we don't drift away
				targetPosition = newPos;
				previousPosition = newPos;
				velocity = Vector3.zero; // Stop moving

				transform.SetPosition(newPos);

				// Face the building (simple heuristic)
				if (animController != null)
				{
					float buildingX = go.transform.position.x;
					animController.FlipX = (buildingX < newPos.x);
				}

				// TRIGGER BUILDING ANIMATION
				// This forces the building (e.g. Generator) to play its working loop
				// This is a client-side visual fix since building state might not be synced
				var buildingAnim = go.GetComponent<KBatchedAnimController>();
				if (buildingAnim != null)
				{
					// Most buildings use "working_loop" or "working"
					string currentAnimName = buildingAnim.currentAnim.ToString(); // Safe as verified in DuplicantStateSender
					if (currentAnimName != "working_loop" && currentAnimName != "working")
					{
						try
						{
							buildingAnim.Play(new HashedString("working_loop"), KAnim.PlayMode.Loop);
						}
						catch
						{
							// If it fails or doesn't exist, ignore
						}
					}
				}
			}
		}

		private void ApplyActionAnimation(DuplicantActionState state)
		{
			if (animController == null)
				return;

			HashedString targetAnim = GetActionStateAnim(state);
			if (targetAnim.IsValid && animController.currentAnim != targetAnim)
			{
				currentAnim = targetAnim;
				currentMode = KAnim.PlayMode.Loop;
				animDirty = true;
			}
		}

		private HashedString GetActionStateAnim(DuplicantActionState state)
		{
			if (state == DuplicantActionState.Building)
				return new HashedString("build_loop");
			if (state == DuplicantActionState.Digging)
				return new HashedString("dig_loop");
			if (state == DuplicantActionState.Working)
				return new HashedString("working_loop");
			if (state == DuplicantActionState.Eating)
				return new HashedString("eat_loop");
			if (state == DuplicantActionState.Sleeping)
				return new HashedString("sleep_pre");
			if (state == DuplicantActionState.Using)
				return new HashedString("working_loop");
			if (state == DuplicantActionState.Carrying)
				return new HashedString("carry_loop");
			return default;
		}
	}
}

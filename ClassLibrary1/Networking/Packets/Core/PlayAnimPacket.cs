using ONI_MP.Api.Networking.Packets.Architecture;
using ONI_MP.DebugTools;
using ONI_MP.Networking;
using ONI_MP.Networking.Components;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

public class PlayAnimPacket : IPacket
{
	public int NetId;
	public bool IsMulti;
	public List<int> AnimHashes = new List<int>();  // For multi
	public int SingleAnimHash;                      // For single
	public KAnim.PlayMode Mode;
	public float Speed;
	public float Offset;
	public bool IsQueue; // Supports Queue()

	// Static reflection cache
	private static readonly FieldInfo forceRebuildField =
			typeof(KBatchedAnimController).GetField("_forceRebuild", BindingFlags.Instance | BindingFlags.NonPublic);

	private static readonly MethodInfo suspendUpdatesMethod =
			typeof(KBatchedAnimController).GetMethod("SuspendUpdates", BindingFlags.Instance | BindingFlags.NonPublic);

	private static readonly MethodInfo configureUpdateListenerMethod =
			typeof(KBatchedAnimController).GetMethod("ConfigureUpdateListener", BindingFlags.Instance | BindingFlags.NonPublic);

	public void Serialize(BinaryWriter writer)
	{
		writer.Write(NetId);
		writer.Write(IsMulti);
		writer.Write((int)Mode);
		writer.Write(Speed);
		writer.Write(Offset);
		writer.Write(IsQueue);

		if (IsMulti)
		{
			writer.Write(AnimHashes.Count);
			foreach (var hash in AnimHashes)
				writer.Write(hash);
		}
		else
		{
			writer.Write(SingleAnimHash);
		}
	}

	public void Deserialize(BinaryReader reader)
	{
		NetId = reader.ReadInt32();
		IsMulti = reader.ReadBoolean();
		Mode = (KAnim.PlayMode)reader.ReadInt32();
		Speed = reader.ReadSingle();
		Offset = reader.ReadSingle();
		IsQueue = reader.ReadBoolean();

		if (IsMulti)
		{
			int count = reader.ReadInt32();
			AnimHashes = new List<int>(count);
			for (int i = 0; i < count; i++)
				AnimHashes.Add(reader.ReadInt32());
		}
		else
		{
			SingleAnimHash = reader.ReadInt32();
		}
	}

	public void OnDispatched()
	{
		if (MultiplayerSession.IsHost)
			return;

		if (!NetworkIdentityRegistry.TryGet(NetId, out var go))
			return;

		// Check for DuplicantClientController first (for duplicants)
		var clientController = go.GetComponent<DuplicantClientController>();
		if (clientController != null)
		{
			if (IsMulti)
			{
				var hashedStrings = AnimHashes.ConvertAll(hash => new HashedString(hash)).ToArray();
				clientController.OnAnimationsReceived(hashedStrings, Mode);
			}
			else
			{
				clientController.OnAnimationReceived(new HashedString(SingleAnimHash), Mode, Speed, IsQueue);
			}
			return;
		}

		// Fallback: direct animation control for non-duplicant entities
		if (!go.TryGetComponent(out KAnimControllerBase controller))
			return;

		if (IsMulti)
		{
			var hashedStrings = AnimHashes.ConvertAll(hash => new HashedString(hash)).ToArray();
			controller.Play(hashedStrings, Mode);
		}
		else
		{
			if (IsQueue)
				controller.Queue(new HashedString(SingleAnimHash), Mode, Speed, Offset);
			else
				controller.Play(new HashedString(SingleAnimHash), Mode, Speed, Offset);
		}

		// Force updates for animation to tick properly
		ForceAnimUpdate(controller);
	}

	private void ForceAnimUpdate(KAnimControllerBase controller)
	{
		if (controller is KBatchedAnimController batched)
		{
			try
			{
				batched.SetVisiblity(true);
				forceRebuildField?.SetValue(batched, true);
				suspendUpdatesMethod?.Invoke(batched, new object[] { false });
				configureUpdateListenerMethod?.Invoke(batched, null);
			}
			catch (Exception ex)
			{
				DebugConsole.LogError($"[PlayAnimPacket] Failed to force anim update for NetId {NetId}: {ex}");
			}
		}
	}
}


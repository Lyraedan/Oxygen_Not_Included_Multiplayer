using ONI_MP.DebugTools;
using ONI_MP.Networking;
using ONI_MP.Networking.Components;
using ONI_MP.Networking.Packets.Architecture;
using ONI_MP.Patches.KleiPatches;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;

public class PlayAnimPacket : IPacket
{

	public PlayAnimPacket() { }
	public PlayAnimPacket(int targetNetId, HashedString[] anims, bool queue, KAnim.PlayMode mode, float speed, float offset)
	{
		NetId = targetNetId;
		AnimHashes = anims;
		IsQueue = queue;
		Mode = mode;
		Speed = speed;
		TimeOffset = offset;
		TimeStamp = Time.time;
	}

	public int NetId;
	public float TimeStamp;
	public HashedString[] AnimHashes = [];
	public KAnim.PlayMode Mode;
	public float Speed;
	public float TimeOffset;
	public bool IsQueue; // Supports Queue()
	bool MultipleAnims => AnimHashes.Count() > 1;


	public void Serialize(BinaryWriter writer)
	{
		writer.Write(NetId);
		writer.Write(TimeStamp);
		writer.Write((int)Mode);
		writer.Write(Speed);
		writer.Write(TimeOffset);
		writer.Write(IsQueue);

		writer.Write(AnimHashes.Count());
		foreach (var hashedString in AnimHashes)
			writer.Write(hashedString.hash);
	}

	public void Deserialize(BinaryReader reader)
	{
		NetId = reader.ReadInt32();
		TimeStamp = reader.ReadSingle();
		Mode = (KAnim.PlayMode)reader.ReadInt32();
		Speed = reader.ReadSingle();
		TimeOffset = reader.ReadSingle();
		IsQueue = reader.ReadBoolean();

		int count = reader.ReadInt32();
		AnimHashes = new HashedString[count];
		for (int i = 0; i < count; i++)
			AnimHashes[i] = new HashedString(reader.ReadInt32());
	}

	Dictionary<int, float> LastIdUpdates = [];

	public void OnDispatched()
	{
		if (MultiplayerSession.IsHost)
			return;

		if (!NetworkIdentityRegistry.TryGet(NetId, out var go))
			return;

		if (LastIdUpdates.TryGetValue(NetId, out var lastTimeStamp) && lastTimeStamp > TimeStamp)
			return;
		LastIdUpdates[NetId] = TimeStamp;


		if (!AnimHashes.Any())
		{
			DebugConsole.LogWarning("emtpy anim list dispatched for " + go.name);
			return;
		}

		//// Check for DuplicantClientController first (for duplicants)
		//var clientController = go.GetComponent<DuplicantClientController>();
		//if (clientController != null)
		//{
		//	if (IsMulti)
		//	{
		//		var hashedStrings = AnimHashes.ConvertAll(hash => new HashedString(hash)).ToArray();
		//		clientController.OnAnimationsReceived(hashedStrings, Mode);
		//	}
		//	else
		//	{
		//		clientController.OnAnimationReceived(new HashedString(SingleAnimHash), Mode, Speed, IsQueue);
		//	}
		//	return;
		//}

		//// Fallback: direct animation control for non-duplicant entities
		if (!go.TryGetComponent(out KBatchedAnimController kbac))
			return;

		if (MultipleAnims)
		{
			KAnimControllerBasePatch.AllowAnims();
			kbac.Play(AnimHashes, Mode);
			KAnimControllerBasePatch.ForbidAnims();
		}
		else
		{
			if (IsQueue)
			{
				KAnimControllerBasePatch.AllowAnims();
				kbac.Queue(AnimHashes.FirstOrDefault(), Mode, Speed, TimeOffset);
				KAnimControllerBasePatch.ForbidAnims();
			}
			else
			{
				KAnimControllerBasePatch.AllowAnims();
				kbac.Play(AnimHashes.FirstOrDefault(), Mode, Speed, TimeOffset);
				KAnimControllerBasePatch.ForbidAnims();
			}

		}
		ForceAnimUpdate(kbac);
		// Force updates for animation to tick properly
	}

	private void ForceAnimUpdate(KBatchedAnimController kbac)
	{
		try
		{
			kbac.SetVisiblity(true);
			kbac.forceRebuild = true;
			kbac.SuspendUpdates(false);
			kbac.ConfigureUpdateListener();
		}
		catch (Exception ex)
		{
			DebugConsole.LogError($"[PlayAnimPacket] Failed to force anim update for NetId {NetId}: {ex}");
		}

	}
}


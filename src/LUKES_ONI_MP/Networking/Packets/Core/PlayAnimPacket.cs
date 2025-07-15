using ONI_MP.DebugTools;
using ONI_MP.Networking;
using ONI_MP.Networking.Packets.Architecture;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEngine;

public class PlayAnimPacket : IPacket
{
    public PacketType Type => PacketType.PlayAnim;

    public int NetId;
    public bool IsMulti;
    public List<int> AnimHashes = new List<int>();  // For multi
    public int SingleAnimHash;                      // For single
    public KAnim.PlayMode Mode;
    public float Speed;
    public float Offset;

    // 🧠 Static reflection cache
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

        if (!NetworkIdentityRegistry.TryGet(NetId, out var go) ||
            !go.TryGetComponent(out KAnimControllerBase controller))
            return;

        if (IsMulti)
        {
            var hashedStrings = AnimHashes.ConvertAll(hash => new HashedString(hash)).ToArray();
            controller.Play(hashedStrings, Mode);
        }
        else
        {
            controller.Play(new HashedString(SingleAnimHash), Mode, Speed, Offset);
        }

        // 🔄 Force updates for animation to tick properly
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
                DebugConsole.LogError($"[ONI_MP] Failed to force anim update for NetId {NetId}: {ex}");
            }
        }
    }
}

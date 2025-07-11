﻿using ONI_MP.DebugTools;
using ONI_MP.Networking.Packets.Architecture;
using ONI_MP.Networking;
using System.IO;
using System.Linq;
using ONI_MP.Misc.World;

public class ToggleMinionEffectPacket : IPacket
{
    public PacketType Type => PacketType.ToggleMinionEffect;

    public int NetId;
    public bool Enable;
    public string Context; // e.g. "dig", "sleep", "build"
    public string Event;   // e.g. "LaserOn", "LaserOff", "DreamsOn"

    public void Serialize(BinaryWriter writer)
    {
        writer.Write(NetId);
        writer.Write(Enable);
        writer.Write(Context);
        writer.Write(Event);
    }

    public void Deserialize(BinaryReader reader)
    {
        NetId = reader.ReadInt32();
        Enable = reader.ReadBoolean();
        Context = reader.ReadString();
        Event = reader.ReadString();
    }

    public void OnDispatched()
    {
        if (!NetworkIdentityRegistry.TryGet(NetId, out var go)) return;

        var toggler = go.GetComponentsInChildren<KBatchedAnimEventToggler>()
            .FirstOrDefault(t => t.enableEvent == Event || t.disableEvent == Event);

        if (toggler == null)
        {
            DebugConsole.LogWarning($"[ToggleMinionEffectPacket] Toggler with event '{Event}' not found");
            return;
        }

        toggler.GetComponentInParent<AnimEventHandler>()?.SetContext(Context);

        var hash = Hash.SDBMLower(Event);
        toggler.Trigger(hash, Enable);
        EventTrigger.TriggerWithAuthority(toggler.gameObject, hash, Enable);
    }
}

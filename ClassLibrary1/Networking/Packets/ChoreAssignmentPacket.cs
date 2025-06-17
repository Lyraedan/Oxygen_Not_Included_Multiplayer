﻿using ONI_MP.DebugTools;
using ONI_MP.Networking;
using ONI_MP.Networking.Packets.Architecture;
using ONI_MP.Patches.Chores;
using System.IO;
using UnityEngine;

public class ChoreAssignmentPacket : IPacket
{
    public int NetId;
    public string ChoreTypeId;

    public Vector3 TargetPosition;
    public int TargetCell = -1;
    public string TargetPrefabId; // optional

    public PacketType Type => PacketType.ChoreAssignment;

    public void Serialize(BinaryWriter writer)
    {
        writer.Write(NetId);
        writer.Write(ChoreTypeId ?? string.Empty);
        writer.Write(TargetPosition.x);
        writer.Write(TargetPosition.y);
        writer.Write(TargetPosition.z);
        writer.Write(TargetCell);
        writer.Write(TargetPrefabId ?? string.Empty);
    }

    public void Deserialize(BinaryReader reader)
    {
        NetId = reader.ReadInt32();
        ChoreTypeId = reader.ReadString();
        TargetPosition = new Vector3(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());
        TargetCell = reader.ReadInt32();
        TargetPrefabId = reader.ReadString();
    }

    public void OnDispatched()
    {
        // Disabled for now
        return;

        if (!NetEntityRegistry.TryGet(NetId, out var entity))
        {
            DebugConsole.LogWarning($"[ChoreAssignment] Could not find entity with NetId {NetId}");
            return;
        }

        var dupeGO = entity.gameObject;
        var consumer = dupeGO.GetComponent<ChoreConsumer>();
        var type = Db.Get().ChoreTypes.Get(ChoreTypeId);

        if (consumer == null || type == null)
        {
            DebugConsole.LogWarning($"[ChoreAssignment] Missing consumer or chore type: {ChoreTypeId}");
            return;
        }

        // Create the context to pass for precondition-aware chores
        var context = new Chore.Precondition.Context
        {
            consumerState = consumer.GetComponent<ChoreConsumerState>(),
            choreTypeForPermission = type
        };

        var chore = ChoreFactory.Create(ChoreTypeId, context, dupeGO, TargetPosition, TargetCell, TargetPrefabId);

        if (chore != null)
        {
            chore.AssignChoreToDuplicant(dupeGO);
        }
        else
        {
            DebugConsole.LogWarning($"[ChoreAssignment] Could not create chore: {ChoreTypeId} for {dupeGO.name}");
        }
    }


}

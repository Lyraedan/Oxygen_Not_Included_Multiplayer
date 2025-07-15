using ONI_MP.DebugTools;
using ONI_MP.Networking.Packets.Architecture;
using ONI_MP.Networking;
using System.IO;
using UnityEngine;
using ONI_MP.Networking.Components;

public class EntityPositionPacket : IPacket
{
    public int NetId;
    public Vector3 Position;
    public bool FacingLeft;

    public PacketType Type => PacketType.EntityPosition;

    public void Serialize(BinaryWriter writer)
    {
        writer.Write(NetId);
        writer.Write(Position.x);
        writer.Write(Position.y);
        writer.Write(Position.z);
        writer.Write(FacingLeft);
    }

    public void Deserialize(BinaryReader reader)
    {
        NetId = reader.ReadInt32();
        float x = reader.ReadSingle();
        float y = reader.ReadSingle();
        float z = reader.ReadSingle();
        Position = new Vector3(x, y, z);
        FacingLeft = reader.ReadBoolean();
    }

    public void OnDispatched()
    {
        if (MultiplayerSession.IsHost) return;

        if (NetworkIdentityRegistry.TryGet(NetId, out var entity))
        {
            var anim = entity.GetComponent<KBatchedAnimController>();
            if (anim == null)
            {
                DebugConsole.LogWarning($"[Packets] No KBatchedAnimController found on entity {NetId}");
                return;
            }

            entity.StopCoroutine("InterpolateKAnimPosition");
            entity.StartCoroutine(InterpolateKAnimPosition(anim, Position, FacingLeft));
        }
        else
        {
            DebugConsole.LogWarning($"[Packets] Could not find entity with NetId {NetId}");
        }
    }

    private System.Collections.IEnumerator InterpolateKAnimPosition(KBatchedAnimController anim, Vector3 targetPos, bool facingLeft)
    {
        Vector3 startPos = anim.transform.GetPosition();
        float duration = EntityPositionHandler.SendInterval;
        float elapsed = 0f;

        anim.FlipX = facingLeft;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = elapsed / duration;
            anim.transform.SetPosition(Vector3.Lerp(startPos, targetPos, t));
            yield return null;
        }

        // Snap at the end to prevent drift
        anim.transform.SetPosition(targetPos);
    }


}

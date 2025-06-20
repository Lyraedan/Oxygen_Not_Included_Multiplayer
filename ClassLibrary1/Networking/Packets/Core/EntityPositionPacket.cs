using System.IO;
using UnityEngine;
using ONI_MP.Networking;
using ONI_MP.Networking.Packets.Architecture;
using ONI_MP.DebugTools;
using ONI_MP.Networking.Components;

public class EntityPositionPacket : IPacket
{
    public int NetId;
    public Vector3 Position;
    public Vector3 Velocity;
    public bool FacingLeft;
    public float Timestamp;

    public PacketType Type => PacketType.EntityPosition;

    public void Serialize(BinaryWriter writer)
    {
        writer.Write(NetId);
        writer.Write(Position.x);
        writer.Write(Position.y);
        writer.Write(Position.z);

        writer.Write(Velocity.x);
        writer.Write(Velocity.y);
        writer.Write(Velocity.z);

        writer.Write(FacingLeft);
        writer.Write(Timestamp);
    }

    public void Deserialize(BinaryReader reader)
    {
        NetId = reader.ReadInt32();

        float px = reader.ReadSingle();
        float py = reader.ReadSingle();
        float pz = reader.ReadSingle();
        Position = new Vector3(px, py, pz);

        float vx = reader.ReadSingle();
        float vy = reader.ReadSingle();
        float vz = reader.ReadSingle();
        Velocity = new Vector3(vx, vy, vz);

        FacingLeft = reader.ReadBoolean();
        Timestamp = reader.ReadSingle();
    }

    public void OnDispatched()
    {
        if (MultiplayerSession.IsHost)
            return;

        if (!NetworkIdentityRegistry.TryGet(NetId, out var entity))
        {
            DebugConsole.LogWarning($"[Packets] Could not find entity with NetId {NetId}");
            return;
        }

        var anim = entity.GetComponent<KBatchedAnimController>();
        if (anim == null)
        {
            DebugConsole.LogWarning($"[Packets] No KBatchedAnimController on entity {NetId}");
            return;
        }

        Vector3 currentPos = anim.transform.GetPosition();
        float localTime = Time.unscaledTime;
        float timeSinceSent = Mathf.Clamp(localTime - Timestamp, 0f, 0.2f); // Prevent huge prediction steps

        // Predict where the entity should be now
        Vector3 predictedPos = Position + Velocity * timeSinceSent;
        float error = Vector3.Distance(currentPos, predictedPos);

        if (error > 10f)
        {
            // Snap if too far
            anim.transform.SetPosition(Position);
        }
        else
        {
            // Interpolate smoothly
            float interpFactor = Mathf.Clamp01(timeSinceSent / EntityPositionHandler.SendInterval);
            Vector3 correctedVelocity = (predictedPos - currentPos) / Mathf.Max(timeSinceSent, 0.016f);
            Vector3 smoothedVelocity = Vector3.Lerp(Vector3.zero, correctedVelocity, interpFactor);

            anim.transform.SetPosition(currentPos + smoothedVelocity * Time.unscaledDeltaTime);
        }

        anim.FlipX = FacingLeft;
    }
}

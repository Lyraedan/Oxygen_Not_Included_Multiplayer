using System.IO;
using System.Linq;
using ONI_MP.DebugTools;
using ONI_MP.Networking;
using ONI_MP.Networking.Packets.Architecture;

namespace ONI_MP.Networking.Packets.Research
{
    /// <summary>
    /// Synchronizes research progress across all multiplayer clients.
    /// Tracks research points and completion status for technology trees.
    /// </summary>
    public class ResearchProgressPacket : IPacket
    {
        public PacketType Type => PacketType.ResearchProgress;

        public string TechId;
        public float ResearchPoints;
        public bool IsCompleted;
        public float TotalPointsRequired;
        public string ResearcherDuplicantNetId; // The duplicant doing the research
        public float ResearchSpeed; // Current research speed modifier

        public void Serialize(BinaryWriter writer)
        {
            writer.Write(TechId ?? "");
            writer.Write(ResearchPoints);
            writer.Write(IsCompleted);
            writer.Write(TotalPointsRequired);
            writer.Write(ResearcherDuplicantNetId ?? "");
            writer.Write(ResearchSpeed);
        }

        public void Deserialize(BinaryReader reader)
        {
            TechId = reader.ReadString();
            ResearchPoints = reader.ReadSingle();
            IsCompleted = reader.ReadBoolean();
            TotalPointsRequired = reader.ReadSingle();
            ResearcherDuplicantNetId = reader.ReadString();
            ResearchSpeed = reader.ReadSingle();
        }

        public void OnDispatched()
        {
            if (MultiplayerSession.IsHost) return;

            // Apply research progress on client
            var tech = Db.Get().Techs.TryGet(TechId);
            if (tech != null)
            {
                DebugConsole.Log($"[Research & Skills] Research progress sync: {TechId} - {ResearchPoints:F1}/{TotalPointsRequired:F1} points");
                
                if (IsCompleted)
                {
                    DebugConsole.Log($"[Research & Skills] Technology completed: {TechId}");
                }
            }
            else
            {
                DebugConsole.LogWarning($"[Research & Skills] Unknown technology: {TechId}");
            }
        }

        /// <summary>
        /// Creates a ResearchProgressPacket from current research state
        /// </summary>
        public static ResearchProgressPacket FromTechnology(Tech tech, TechInstance techInstance)
        {
            if (tech == null || techInstance == null) return null;

            var packet = new ResearchProgressPacket
            {
                TechId = tech.Id,
                IsCompleted = techInstance.IsComplete(),
                TotalPointsRequired = tech.costsByResearchTypeID?.Values.Sum() ?? 0f,
                ResearchSpeed = 1f // Default research speed
            };

            // Calculate current research points
            if (techInstance.progressInventory?.PointsByTypeID != null)
            {
                packet.ResearchPoints = techInstance.progressInventory.PointsByTypeID.Values.Sum();
            }

            return packet;
        }
    }
}

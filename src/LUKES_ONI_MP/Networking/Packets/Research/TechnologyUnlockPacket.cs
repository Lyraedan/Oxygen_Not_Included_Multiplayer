using System.Collections.Generic;
using System.IO;
using ONI_MP.DebugTools;
using ONI_MP.Networking;
using ONI_MP.Networking.Packets.Architecture;

namespace ONI_MP.Networking.Packets.Research
{
    /// <summary>
    /// Synchronizes technology unlocks across all multiplayer clients.
    /// Ensures all players have access to the same buildings, recipes, and technologies.
    /// </summary>
    public class TechnologyUnlockPacket : IPacket
    {
        public PacketType Type => PacketType.TechnologyUnlock;

        public string TechId;
        public bool IsUnlocked;
        public List<string> UnlockedRecipes;
        public List<string> UnlockedBuildings;
        public string UnlockedByPlayer; // Steam ID of player who completed the research
        public long UnlockTimestamp;

        public TechnologyUnlockPacket()
        {
            UnlockedRecipes = new List<string>();
            UnlockedBuildings = new List<string>();
        }

        public void Serialize(BinaryWriter writer)
        {
            writer.Write(TechId ?? "");
            writer.Write(IsUnlocked);
            writer.Write(UnlockedByPlayer ?? "");
            writer.Write(UnlockTimestamp);

            // Serialize unlocked recipes
            writer.Write(UnlockedRecipes.Count);
            foreach (var recipe in UnlockedRecipes)
            {
                writer.Write(recipe);
            }

            // Serialize unlocked buildings
            writer.Write(UnlockedBuildings.Count);
            foreach (var building in UnlockedBuildings)
            {
                writer.Write(building);
            }
        }

        public void Deserialize(BinaryReader reader)
        {
            TechId = reader.ReadString();
            IsUnlocked = reader.ReadBoolean();
            UnlockedByPlayer = reader.ReadString();
            UnlockTimestamp = reader.ReadInt64();

            // Deserialize unlocked recipes
            UnlockedRecipes.Clear();
            int recipeCount = reader.ReadInt32();
            for (int i = 0; i < recipeCount; i++)
            {
                UnlockedRecipes.Add(reader.ReadString());
            }

            // Deserialize unlocked buildings
            UnlockedBuildings.Clear();
            int buildingCount = reader.ReadInt32();
            for (int i = 0; i < buildingCount; i++)
            {
                UnlockedBuildings.Add(reader.ReadString());
            }
        }

        public void OnDispatched()
        {
            if (MultiplayerSession.IsHost) return;

            var tech = Db.Get().Techs.TryGet(TechId);
            if (tech == null)
            {
                DebugConsole.LogWarning($"[Research & Skills] Unknown technology for unlock: {TechId}");
                return;
            }

            if (IsUnlocked)
            {
                DebugConsole.Log($"[Research & Skills] Technology unlocked: {tech.Name} (by {UnlockedByPlayer})");
                
                // Trigger unlock effects
                TriggerTechnologyUnlockEffects();
            }
        }

        private void TriggerTechnologyUnlockEffects()
        {
            // Refresh building menus to show newly unlocked buildings
            if (PlanScreen.Instance != null)
            {
                PlanScreen.Instance.Refresh();
            }

            // Note: Other UI refreshes may not be accessible, so we skip them for now
        }

        /// <summary>
        /// Creates a TechnologyUnlockPacket from a completed technology
        /// </summary>
        public static TechnologyUnlockPacket FromTechnology(Tech tech, string playerSteamId = "")
        {
            if (tech == null) return null;

            var packet = new TechnologyUnlockPacket
            {
                TechId = tech.Id,
                IsUnlocked = true,
                UnlockedByPlayer = playerSteamId,
                UnlockTimestamp = System.DateTime.UtcNow.Ticks
            };

            // Collect unlocked recipes
            if (tech.unlockedItemIDs != null)
            {
                // In ONI, unlockedItemIDs contains both recipes and buildings
                packet.UnlockedRecipes.AddRange(tech.unlockedItemIDs);
            }

            // Collect unlocked buildings  
            if (tech.unlockedItemIDs != null)
            {
                packet.UnlockedBuildings.AddRange(tech.unlockedItemIDs);
            }

            return packet;
        }

        /// <summary>
        /// Creates a batch unlock packet for multiple technologies (used during game sync)
        /// </summary>
        public static List<TechnologyUnlockPacket> FromResearchState()
        {
            var packets = new List<TechnologyUnlockPacket>();
            
            // Note: Research.Instance may not be accessible, so we return empty list for now
            DebugConsole.Log("[Research & Skills] Research state sync not fully available yet");

            return packets;
        }
    }
}

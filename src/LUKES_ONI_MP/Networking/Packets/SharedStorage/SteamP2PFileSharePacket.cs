using ONI_MP.DebugTools;
using ONI_MP.Menus;
using ONI_MP.Misc;
using ONI_MP.Networking.Packets.Architecture;
using ONI_MP.Networking.States;
using ONI_MP.SharedStorage;
using System;
using System.IO;

namespace ONI_MP.Networking.Packets.SharedStorage
{
    /// <summary>
    /// Packet for sharing Steam P2P file information between players.
    /// </summary>
    public class SteamP2PFileSharePacket : IPacket
    {
        public string FileName { get; set; } = "";
        public string P2PFileName { get; set; } = "";
        public int FileSize { get; set; } = 0;
        public System.DateTime Timestamp { get; set; } = System.DateTime.MinValue;

        public PacketType Type => PacketType.SteamP2PFileShare;

        public void Serialize(BinaryWriter writer)
        {
            writer.Write(FileName ?? "");
            writer.Write(P2PFileName ?? "");
            writer.Write(FileSize);
            writer.Write(Timestamp.ToBinary());
        }

        public void Deserialize(BinaryReader reader)
        {
            FileName = reader.ReadString();
            P2PFileName = reader.ReadString();
            FileSize = reader.ReadInt32();
            Timestamp = System.DateTime.FromBinary(reader.ReadInt64());
        }

        public void OnDispatched()
        {
            if (MultiplayerSession.IsHost)
            {
                return; // Host does nothing here - they already have the file
            }

            if (!Utils.IsInGame())
            {
                return; // Only process when in game
            }

            DebugConsole.Log($"[SteamP2PFileSharePacket] Received P2P file notification - {FileName} ({FileSize} bytes)");
            
            // Show notification to client
            MultiplayerOverlay.Show($"New file available via Steam P2P: {FileName}");
            
            // For Steam P2P, the file should already be available locally through the P2P network
            // The actual download mechanism would be implemented in the SteamP2PStorageProvider
            if (SharedStorageManager.Instance.CurrentProvider == "SteamP2P")
            {
                DebugConsole.Log($"[SteamP2PFileSharePacket] P2P file ready for download: {P2PFileName}");
                // Future: Could trigger automatic download or user prompt
            }
        }
    }
}

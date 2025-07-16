using ONI_MP.Networking.Packets.Architecture;
using System.IO;

namespace ONI_MP.Networking.Packets.SharedStorage
{
    /// <summary>
    /// Packet for sharing HTTP cloud file information between players.
    /// </summary>
    public class StorageServerFileSharePacket : IPacket
    {
        public string FileName { get; set; } = "";
        public string CloudFileName { get; set; } = "";
        public string ServerUrl { get; set; } = "";
        public string SessionId { get; set; } = "";
        public int FileSize { get; set; } = 0;
        public System.DateTime Timestamp { get; set; } = System.DateTime.MinValue;

        public PacketType Type => PacketType.HttpCloudFileShare;

        public void Serialize(BinaryWriter writer)
        {
            writer.Write(FileName ?? "");
            writer.Write(CloudFileName ?? "");
            writer.Write(ServerUrl ?? "");
            writer.Write(SessionId ?? "");
            writer.Write(FileSize);
            writer.Write(Timestamp.ToBinary());
        }

        public void Deserialize(BinaryReader reader)
        {
            FileName = reader.ReadString();
            CloudFileName = reader.ReadString();
            ServerUrl = reader.ReadString();
            SessionId = reader.ReadString();
            FileSize = reader.ReadInt32();
            Timestamp = System.DateTime.FromBinary(reader.ReadInt64());
        }

        public void OnDispatched()
        {
            // Handle the HTTP cloud file share notification
            // This could trigger automatic download or show notification to user
            ONI_MP.DebugTools.DebugConsole.Log($"StorageServerFileSharePacket: Received file share notification - {FileName} ({FileSize} bytes) from {ServerUrl}");
            
            // Auto-download if configured or notify user
            if (ONI_MP.SharedStorage.SharedStorageManager.Instance.CurrentProvider == "StorageServer")
            {
                // Could automatically download or show notification
                ONI_MP.Menus.MultiplayerOverlay.Show($"New file available: {FileName}");
            }
        }
    }
}

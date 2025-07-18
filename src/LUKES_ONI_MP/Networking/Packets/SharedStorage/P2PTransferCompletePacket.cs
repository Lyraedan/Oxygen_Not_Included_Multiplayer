using System.IO;
using ONI_MP.DebugTools;
using ONI_MP.Networking.Packets.Architecture;
using ONI_MP.SharedStorage;
using Steamworks;

namespace ONI_MP.Networking.Packets.SharedStorage
{
    /// <summary>
    /// Packet to notify completion of a P2P file transfer
    /// </summary>
    public class P2PTransferCompletePacket : IPacket
    {
        public PacketType Type => PacketType.P2PTransferComplete;
        
        public CSteamID SenderSteamID;
        public string FileName;
        public string FileHash;
        public string TransferID;
        public bool Success;
        public string ErrorMessage;
        
        public P2PTransferCompletePacket() { }
        
        public P2PTransferCompletePacket(CSteamID senderSteamID, string fileName, string fileHash, 
            string transferID, bool success, string errorMessage = null)
        {
            SenderSteamID = senderSteamID;
            FileName = fileName;
            FileHash = fileHash;
            TransferID = transferID;
            Success = success;
            ErrorMessage = errorMessage;
        }
        
        public void Serialize(BinaryWriter writer)
        {
            writer.Write(SenderSteamID.m_SteamID);
            writer.Write(FileName ?? "");
            writer.Write(FileHash ?? "");
            writer.Write(TransferID ?? "");
            writer.Write(Success);
            writer.Write(ErrorMessage ?? "");
        }
        
        public void Deserialize(BinaryReader reader)
        {
            SenderSteamID = new CSteamID(reader.ReadUInt64());
            FileName = reader.ReadString();
            FileHash = reader.ReadString();
            TransferID = reader.ReadString();
            Success = reader.ReadBoolean();
            ErrorMessage = reader.ReadString();
        }
        
        public void OnDispatched()
        {
            DebugConsole.Log($"[P2PTransferCompletePacket] Transfer {(Success ? "completed" : "failed")} for {FileName} from {SenderSteamID}");
            
            if (!Success && !string.IsNullOrEmpty(ErrorMessage))
            {
                DebugConsole.LogError($"[P2PTransferCompletePacket] Transfer error: {ErrorMessage}");
            }
            
            // Handle transfer completion in the storage provider
            var steamP2PProvider = SharedStorageManager.Instance.CurrentProviderInstance as SteamP2PStorageProvider;
            if (steamP2PProvider != null)
            {
                steamP2PProvider.HandleTransferComplete(this);
            }
        }
    }
}

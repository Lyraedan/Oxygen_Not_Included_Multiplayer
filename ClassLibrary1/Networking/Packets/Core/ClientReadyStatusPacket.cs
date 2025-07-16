using System.IO;
using ONI_MP.Cloud;
using ONI_MP.DebugTools;
using ONI_MP.Menus;
using ONI_MP.Networking.Packets.Architecture;
using ONI_MP.Networking.States;
using Steamworks;

namespace ONI_MP.Networking.Packets.Core
{
    class ClientReadyStatusPacket : IPacket
    {
        public PacketType Type => PacketType.ClientReadyStatus;

        public string SenderId;
        public ClientReadyState Status = ClientReadyState.Unready;

        public ClientReadyStatusPacket() { }

        public ClientReadyStatusPacket(string senderId, ClientReadyState status)
        {
            SenderId = senderId;
            Status = status;
        }

        public void Serialize(BinaryWriter writer)
        {
            writer.Write((int)Status);
            writer.Write(SenderId);
        }

        public void Deserialize(BinaryReader reader)
        {
            Status = (ClientReadyState)reader.ReadInt32();
            SenderId = reader.ReadString();
        }

        public void OnDispatched()
        {
            if (!MultiplayerSession.IsHost)
            {
                DebugConsole.LogWarning("[ClientReadyStatusPacket] Received on client — ignoring.");
                return;
            }

            ReadyManager.SetPlayerReadyState(SenderId, Status);
            DebugConsole.Log($"[ClientReadyStatusPacket] {SenderId} marked as {Status}");

            // Build the overlay message
            string message = "Waiting for players to be ready!\n";
            bool allReady = ReadyManager.AreAllPlayersReady(
                OnIteration: () => { MultiplayerOverlay.Show(message); },
                OnPlayerChecked: (steamName, readyState) =>
                {
                    message += $"{steamName} : {readyState}\n";
                });

            MultiplayerOverlay.Show(message);

            if(GameServerHardSync.IsHardSyncInProgress)
            {
                if(allReady)
                {
                    ReadyManager.MarkAllAsUnready(); // Reset player ready states
                    GoogleDriveUtils.UploadAndSendToAllClients();
                }
                return;
            }

            if (allReady)
            {
                ReadyManager.SendAllReadyPacket();
            }
            else
            {
                // Broadcast updated overlay message to all clients
                ReadyManager.SendStatusUpdatePacketToClients(message);
            }
        }
    }
}

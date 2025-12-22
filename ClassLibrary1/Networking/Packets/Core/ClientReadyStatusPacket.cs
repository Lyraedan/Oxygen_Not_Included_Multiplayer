using ONI_MP.DebugTools;
using ONI_MP.Menus;
using ONI_MP.Networking.Packets.Architecture;
using ONI_MP.Networking.Packets.World;
using ONI_MP.Networking.States;
using Steamworks;
using System.IO;

namespace ONI_MP.Networking.Packets.Core
{
	class ClientReadyStatusPacket : IPacket
	{
		public CSteamID SenderId;
		public ClientReadyState Status = ClientReadyState.Unready;

		public ClientReadyStatusPacket() { }

		public ClientReadyStatusPacket(CSteamID senderId, ClientReadyState status)
		{
			SenderId = senderId;
			Status = status;
		}

		public void Serialize(BinaryWriter writer)
		{
			writer.Write((int)Status);
			writer.Write(SenderId.m_SteamID);
		}

		public void Deserialize(BinaryReader reader)
		{
			Status = (ClientReadyState)reader.ReadInt32();
			SenderId = new CSteamID(reader.ReadUInt64());
		}

		public void OnDispatched()
		{
			if (!MultiplayerSession.IsHost)
			{
				DebugConsole.LogWarning("[ClientReadyStatusPacket] Received on client — ignoring.");
				return;
			}

			MultiplayerPlayer player;
			MultiplayerSession.ConnectedPlayers.TryGetValue(SenderId, out player);

			if (player == null)
			{
				DebugConsole.LogError("Tried to update ready state for a null player");
				return;
			}

            ReadyManager.SetPlayerReadyState(player, Status);
			DebugConsole.Log($"[ClientReadyStatusPacket] {SenderId} marked as {Status}");

			ReadyManager.RefreshScreen();
			bool allReady = ReadyManager.IsEveryoneReady();
            DebugConsole.Log($"[ClientReadyStatusPacket] Is everyone ready? {allReady}");

            //if (GameServerHardSync.IsHardSyncInProgress)
			//{
			//	if (allReady)
			//	{
			//		ReadyManager.MarkAllAsUnready(); // Reset player ready states
			//		SaveFileRequestPacket.SendSaveFileToAll();
			//	}
			//	return;
			//}

			if (allReady)
			{
				ReadyManager.SendAllReadyPacket();
			}
			else
			{
				// Broadcast updated overlay message to all clients
				ReadyManager.SendStatusUpdatePacketToClients();
			}
		}
	}
}

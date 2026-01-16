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
		public ulong SenderId;
		public ClientReadyState Status = ClientReadyState.Unready;

		public ClientReadyStatusPacket() { }

		public ClientReadyStatusPacket(ulong senderId, ClientReadyState status)
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
			SenderId = reader.ReadUInt64();
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
			ReadyManager.RefreshReadyState();
		}
	}
}

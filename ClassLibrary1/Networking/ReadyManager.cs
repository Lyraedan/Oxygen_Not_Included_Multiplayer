using ONI_MP.DebugTools;
using ONI_MP.Menus;
using ONI_MP.Networking.Packets.Core;
using ONI_MP.Networking.States;
using Steamworks;
using System;
using System.Collections.Generic;

namespace ONI_MP.Networking
{
	public class ReadyManager
	{

		public static void SetupListeners()
		{
			SteamLobby.OnLobbyMembersRefreshed += UpdateReadyStateTracking;
		}

		public static void SendAllReadyPacket()
		{
			if (!MultiplayerSession.IsHost)
				return;

			//CoroutineRunner.RunOne(DelayAllReadyBroadcast());
			PacketSender.SendToAllClients(new AllClientsReadyPacket());
			AllClientsReadyPacket.ProcessAllReady();
		}

		public static void SendStatusUpdatePacketToClients()
		{
			if (!MultiplayerSession.IsHost)
				return;

			string text = GetScreenText();
			var packet = new ClientReadyStatusUpdatePacket
			{
				Message = text
            };
			PacketSender.SendToAllClients(packet);
		}

		public static void SendReadyStatusPacket(ClientReadyState state)
		{
			// Host is always considered ready so it doesn't send these
			if (MultiplayerSession.IsHost)
				return;

			var packet = new ClientReadyStatusPacket
			{
				SenderId = SteamUser.GetSteamID(),
				Status = state
			};
			PacketSender.SendToHost(packet);
		}

		public static void MarkAllAsUnready()
		{
			if (!MultiplayerSession.IsHost)
				return;

			MultiplayerPlayer host;
			MultiplayerSession.ConnectedPlayers.TryGetValue(MultiplayerSession.HostSteamID, out host);
			host.readyState = ClientReadyState.Ready; // Host is always ready

			foreach (MultiplayerPlayer player in MultiplayerSession.ConnectedPlayers.Values)
			{
                if (player.SteamID == MultiplayerSession.HostSteamID)
                    continue;

				player.readyState = ClientReadyState.Unready;
			}
			RefreshScreen();
        }

		public static void SetPlayerReadyState(MultiplayerPlayer player, ClientReadyState state)
		{
            if (player.SteamID == MultiplayerSession.HostSteamID)
                return;

			player.readyState = state;
		}

		public static void RefreshScreen()
		{
			string text = GetScreenText();
            MultiplayerOverlay.Show(text);
        }

		private static string GetScreenText()
		{
			int readyCount = GetReadyCount();
			int maxPlayers = MultiplayerSession.ConnectedPlayers.Values.Count;
            string message = $"Waiting for players ({readyCount}/{maxPlayers} ready)...\n";
            foreach (MultiplayerPlayer player in MultiplayerSession.ConnectedPlayers.Values)
            {
                message += $"{player.SteamName}: {GetReadyText(player.readyState)}\n";
            }
			return message;
        }

        private static int GetReadyCount()
        {
			int count = 0;
			foreach(MultiplayerPlayer player in MultiplayerSession.ConnectedPlayers.Values)
			{
				if (player.readyState.Equals(ClientReadyState.Ready))
				{
					count++;
				}
			}
			return count;
        }

        private static string GetReadyText(ClientReadyState readyState)
        {
            switch (readyState)
			{
				case ClientReadyState.Ready:
					return "Ready";
				case ClientReadyState.Unready:
					return "Loading";
			}
			return "Unknown";
        }

        private static void UpdateReadyStateTracking(CSteamID id)
		{
			DebugConsole.LogAssert($"Update ready state tracking for {id}");
		}

		/// <summary>
		/// HOST ONLY - Check if all connected clients are ready
		/// </summary>
		/// <returns></returns>
		public static bool IsEveryoneReady()
		{
			bool result = true;
			foreach(MultiplayerPlayer player in MultiplayerSession.ConnectedPlayers.Values)
			{
				if (player.readyState == ClientReadyState.Unready)
				{
					result = false;

                    break;
				}
			}
			return result;
		}

	}
}

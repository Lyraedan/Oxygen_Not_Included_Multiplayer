using ONI_MP.Networking.Packets.Architecture;
using ONI_MP.Networking.States;
using Steamworks;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ONI_MP.Networking.Packets.Handshake
{
	public class GameStateRequestPacket : IPacket
	{
		public GameStateRequestPacket() { }
		public GameStateRequestPacket(CSteamID steamID) { ClientId = steamID; }

		public CSteamID ClientId;
		public HashSet<string> ActiveDlcIds = [];
		public List<ulong> ActiveModIds = [];


		public void Serialize(BinaryWriter writer)
		{
			writer.Write(ClientId.m_SteamID);
			writer.Write(ActiveDlcIds.Count);
			foreach (var id in ActiveDlcIds)
			{
				writer.Write(id);
			}
			writer.Write(ActiveModIds.Count);
			foreach (var id in ActiveModIds)
			{
				writer.Write(id);
			}
		}

		public void Deserialize(BinaryReader reader)
		{
			ClientId = new CSteamID(reader.ReadUInt64());
			int count = reader.ReadInt32();
			ActiveDlcIds = new HashSet<string>(count);
			for (int i = 0; i < count; i++)
			{
				ActiveDlcIds.Add(reader.ReadString());
			}

			count = reader.ReadInt32();
			ActiveModIds = new List<ulong>(count);
			for (int i = 0; i < count; i++)
			{
				ActiveModIds.Add(reader.ReadUInt64());
			}
		}

		public void OnDispatched()
		{
			if (!MultiplayerSession.InSession)
				return;

			if (MultiplayerSession.IsHost)
			{
				CreateStateResponse();
			}
			else
			{
				ConsumeStateResponse();
			}
		}

		void CreateStateResponse()
		{
			PacketSender.SendToPlayer(ClientId, AccumulateStateInfo());
		}
		static GameStateRequestPacket AccumulateStateInfo()
		{
			var packet = new GameStateRequestPacket();
			packet.ActiveDlcIds = SaveLoader.Instance.GameInfo.dlcIds.ToHashSet();
			packet.ActiveModIds.Clear();

			KMod.Manager modManager = Global.Instance.modManager;
			foreach (var mod in modManager.mods)
			{
				if (mod.IsEnabledForActiveDlc() && mod.label.distribution_platform == KMod.Label.DistributionPlatform.Steam && ulong.TryParse(mod.label.id, out var steamId))
				{
					packet.ActiveModIds.Add(steamId);
				}
			}
			return packet;
		}

		void ConsumeStateResponse()
		{
			GameClient.OnHostResponseReceived(this);
		}
	}
}

using System;
using System.IO;
using ONI_MP.Networking.Packets.Architecture;
using ONI_MP.DebugTools;
using Steamworks;

namespace ONI_MP.Networking.Packets.Handshake
{
    public class ModListRequestPacket : IPacket
    {
        public PacketType Type => PacketType.ModListRequest;

        public CSteamID RequesterSteamID;
        public CSteamID TargetSteamID;

        public ModListRequestPacket()
        {
        }

        public ModListRequestPacket(CSteamID requester, CSteamID target)
        {
            RequesterSteamID = requester;
            TargetSteamID = target;
        }

        public void Serialize(BinaryWriter writer)
        {
            writer.Write(RequesterSteamID.m_SteamID);
            writer.Write(TargetSteamID.m_SteamID);
        }

        public void Deserialize(BinaryReader reader)
        {
            RequesterSteamID = new CSteamID(reader.ReadUInt64());
            TargetSteamID = new CSteamID(reader.ReadUInt64());
        }

        public void OnDispatched()
        {
            DebugConsole.Log($"[ModListRequestPacket] Mod list requested by {RequesterSteamID} for {TargetSteamID}");

            // Verificar se é uma requisição válida (host ou para si mesmo)
            bool isValidRequest = MultiplayerSession.IsHost ||
                                  TargetSteamID == SteamUser.GetSteamID() ||
                                  RequesterSteamID == SteamUser.GetSteamID();

            if (!isValidRequest)
            {
                DebugConsole.LogWarning("[ModListRequestPacket] Invalid request - ignoring");
                return;
            }

            // Se a requisição é para nós mesmos, criar e enviar packet de verificação
            if (TargetSteamID == SteamUser.GetSteamID())
            {
                var verificationPacket = new ModVerificationPacket(SteamUser.GetSteamID());
                PacketSender.SendToPlayer(RequesterSteamID, verificationPacket);

                DebugConsole.Log($"[ModListRequestPacket] Sent mod list to {RequesterSteamID}");
            }
            // Se somos o host, podemos encaminhar a requisição
            else if (MultiplayerSession.IsHost)
            {
                PacketSender.SendToPlayer(TargetSteamID, this);
                DebugConsole.Log($"[ModListRequestPacket] Forwarded request to {TargetSteamID}");
            }
        }
    }
}
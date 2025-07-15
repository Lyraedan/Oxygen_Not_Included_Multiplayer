using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using ONI_MP.Cloud;
using ONI_MP.DebugTools;
using ONI_MP.Networking.Packets.Architecture;
using Steamworks;
using UnityEngine;

namespace ONI_MP.Networking.Packets.World
{
    public class SaveFileRequestPacket : IPacket
    {
        public CSteamID Requester;

        public const float SAVE_DATA_SEND_DELAY = 1f;

        public PacketType Type => PacketType.SaveFileRequest;

        public void Serialize(BinaryWriter writer)
        {
            writer.Write(Requester.m_SteamID);
        }

        public void Deserialize(BinaryReader reader)
        {
            Requester = new CSteamID(reader.ReadUInt64());
        }

        public void OnDispatched()
        {
            if (!MultiplayerSession.IsHost)
                return;

            DebugConsole.Log($"[Packets/SaveFileRequest] Received request from {Requester}");
            GoogleDriveUtils.UploadAndSendToClient(Requester);
            //SendSaveFile(Requester);
        }

        public static void SendSaveFile(CSteamID requester)
        {
            if (!MultiplayerSession.IsHost)
                return;

            try
            {
                string name = SaveHelper.WorldName;
                byte[] data = SaveHelper.GetWorldSave();
                string fileName = name + ".sav";

                int ChunkSize = SaveHelper.SAVEFILE_CHUNKSIZE_KB * 1024; // Split into xkb chunks
                var chunkPackets = new List<SaveFileChunkPacket>();

                for (int offset = 0; offset < data.Length; offset += ChunkSize)
                {
                    int size = Math.Min(ChunkSize, data.Length - offset);
                    byte[] chunk = new byte[size];
                    Buffer.BlockCopy(data, offset, chunk, 0, size);

                    var chunkPacket = new SaveFileChunkPacket
                    {
                        FileName = fileName,
                        Offset = offset,
                        TotalSize = data.Length,
                        Chunk = chunk
                    };

                    chunkPackets.Add(chunkPacket);
                }

                CoroutineRunner.RunOne(SendChunksThrottled(chunkPackets, requester));
                DebugConsole.Log($"[SaveFileRequest] Sent '{fileName}' in {Math.Ceiling(data.Length / (float)ChunkSize)} chunks to {requester}");

            }
            catch (Exception ex)
            {
                DebugConsole.LogError($"[SaveFileRequest] Failed to send save file: {ex}");
            }
        }

        private static IEnumerator SendChunksThrottled(List<SaveFileChunkPacket> chunkPackets, CSteamID steamID)
        {
            foreach (var chunkPacket in chunkPackets)
            {
                PacketSender.SendToPlayer(steamID, chunkPacket);
                yield return new WaitForSeconds(SAVE_DATA_SEND_DELAY);
            }
            DebugConsole.Log($"[SaveFileRequest] All {chunkPackets.Count} chunks sent to {steamID}.");
        }

    }
}

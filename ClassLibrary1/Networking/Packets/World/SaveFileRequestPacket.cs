using ONI_MP.Api.Networking.Packets.Architecture;
using ONI_MP.DebugTools;
using ONI_MP.Misc;
using Steamworks;
using System;
using System.Collections;
using System.IO;

namespace ONI_MP.Networking.Packets.World
{
	public class SaveFileRequestPacket : IPacket
	{
		public CSteamID Requester;

		public const float SAVE_DATA_SEND_DELAY = 0.05f;

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
			//GoogleDriveUtils.UploadAndSendToClient(Requester);
			SendSaveFile(Requester);
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

				// Start the streaming coroutine
				CoroutineRunner.RunOne(StreamChunks(data, fileName, requester));
			}
			catch (Exception ex)
			{
				DebugConsole.LogError($"[SaveFileRequest] Failed to send save file: {ex}");
			}
		}

		private static IEnumerator StreamChunks(byte[] data, string fileName, CSteamID steamID)
		{
			int chunkSize = SaveHelper.SAVEFILE_CHUNKSIZE_KB * 1024;
			int totalChunks = (int)Math.Ceiling((double)data.Length / chunkSize);

			// Optimization: Send multiple chunks per frame to maximize throughput
			// 2 chunks * 256KB = 512KB per frame. At 60FPS -> ~30MB/s theoretical max.
			int chunksPerFrame = 2;
			int chunksSentThisFrame = 0;

			DebugConsole.Log($"[SaveFileRequest] Starting transfer of '{fileName}' ({Utils.FormatBytes(data.Length)}) to {steamID} in {totalChunks} chunks.");

			for (int offset = 0; offset < data.Length; /* increments manually */)
			{
				int size = Math.Min(chunkSize, data.Length - offset);
				byte[] chunk = new byte[size];
				Buffer.BlockCopy(data, offset, chunk, 0, size);

				var chunkPacket = new SaveFileChunkPacket
				{
					FileName = fileName,
					Offset = offset,
					TotalSize = data.Length,
					Chunk = chunk
				};

				bool success = PacketSender.SendToPlayer(steamID, chunkPacket);

				if (success)
				{
					offset += chunkSize; // Only advance if sent successfully
					chunksSentThisFrame++;
					if (chunksSentThisFrame >= chunksPerFrame)
					{
						chunksSentThisFrame = 0;
						yield return null; // Wait for next frame
					}
				}
				else
				{
					// Backpressure: Failed to send (buffer likely full). Wait and retry same offset.
					//DebugConsole.LogWarning($"[SaveFileRequest] Buffer full/Send failed. Retrying...");
					chunksSentThisFrame = 0;
					yield return null;
				}
			}

			DebugConsole.Log($"[SaveFileRequest] Transfer complete. Sent {totalChunks} chunks to {steamID}.");
		}

	}
}

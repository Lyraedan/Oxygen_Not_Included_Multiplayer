using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using ONI_MP.DebugTools;
using ONI_MP.Misc;
using Steamworks;
using ONI_MP.Networking;
using ONI_MP.Menus;
using ONI_MP.Networking.Packets.World;

namespace ONI_MP.Misc.World
{
    public static class SaveChunkAssembler
    {
        private class InProgressSave
        {
            public byte[] Data;
            public int ReceivedBytes;
        }

        private static readonly Dictionary<string, InProgressSave> InProgress = new Dictionary<string, InProgressSave>();

        public static void ReceiveChunk(SaveFileChunkPacket chunk)
        {
            if (!InProgress.TryGetValue(chunk.FileName, out var save))
            {
                save = new InProgressSave
                {
                    Data = new byte[chunk.TotalSize],
                    ReceivedBytes = 0
                };
                InProgress[chunk.FileName] = save;
            }

            Buffer.BlockCopy(chunk.Chunk, 0, save.Data, chunk.Offset, chunk.Chunk.Length);
            save.ReceivedBytes += chunk.Chunk.Length;

            DebugConsole.Log($"[ChunkReceiver] Received {chunk.Chunk.Length} bytes for '{chunk.FileName}' (offset {chunk.Offset})");
            int percent = save.ReceivedBytes * 100 / chunk.TotalSize;
            MultiplayerOverlay.Show($"Downloading world: {percent}%");

            if (save.ReceivedBytes >= chunk.TotalSize)
            {
                DebugConsole.Log($"[ChunkReceiver] Completed receive of '{chunk.FileName}' ({Utils.FormatBytes(save.ReceivedBytes)})");
                InProgress.Remove(chunk.FileName);

                var fullSave = new WorldSave(chunk.FileName, save.Data);
                CoroutineRunner.RunOne(DelayedLoad(fullSave));
            }
        }

        private static System.Collections.IEnumerator DelayedLoad(WorldSave save)
        {
            yield return new WaitForSecondsRealtime(1f);
            SaveHelper.RequestWorldLoad(save);
        }
    }
}

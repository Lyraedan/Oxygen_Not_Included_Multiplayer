﻿using System.Collections;
using System.Collections.Generic;
using System.Linq;
using ONI_MP.DebugTools;
using ONI_MP.Menus;
using ONI_MP.Networking.Packets.Architecture;
using ONI_MP.Networking.Packets.Core;
using ONI_MP.Networking.Packets.World;
using ONI_MP.Networking.States;
using Steamworks;
using UnityEngine;

namespace ONI_MP.Networking
{
    public static class GameServerHardSync
    {
        private static bool hardSyncInProgress = false;
        private static int numberOfClientsAtTimeOfSync = 0;

        public static void PerformHardSync()
        {
            if (hardSyncInProgress)
            {
                DebugConsole.Log("[HardSync] A hard sync is already in progress.");
                return;
            }

            SpeedControlScreen.Instance?.Pause(false); // Pause the game
            MultiplayerOverlay.Show("Hard sync in progress!");

            numberOfClientsAtTimeOfSync = MultiplayerSession.ConnectedPlayers.Count;
            var packet = new HardSyncPacket();
            PacketSender.SendToAllClients(packet);

            DebugConsole.Log($"[HardSync] Starting hard sync for {numberOfClientsAtTimeOfSync} client(s)...");
            CoroutineRunner.RunOne(HardSyncCoroutine());
        }

        private static IEnumerator HardSyncCoroutine()
        {
            hardSyncInProgress = true;

            int fileSize = SaveHelper.GetWorldSave().Length;
            int chunkSize = SaveHelper.SAVEFILE_CHUNKSIZE_KB * 1024;
            int chunkCount = Mathf.CeilToInt(fileSize / (float)chunkSize);
            float estimatedTransferDuration = chunkCount * SaveFileRequestPacket.SAVE_DATA_SEND_DELAY;
            yield return new WaitForSeconds(estimatedTransferDuration * numberOfClientsAtTimeOfSync);

            hardSyncInProgress = false;
            SpeedControlScreen.Instance?.Unpause(false);
            MultiplayerOverlay.Close();
        }
    }
}

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ONI_MP.Menus;
using ONI_MP.Networking;
using ONI_MP.Networking.Packets.Architecture;
using ONI_MP.Networking.Packets.Cloud;
using Steamworks;
using static TechInstance;

namespace ONI_MP.Cloud
{
    public class GoogleDriveUtils
    {
        public static void UploadAndSendToAllClients()
        {
            GoogleDrive.Instance.Uploader.OnUploadFinished.RemoveAllListeners();
            GoogleDrive.Instance.Uploader.OnUploadFinished.AddListener((link) =>
            {
                string fileName = Path.GetFileName(SaveLoader.GetActiveSaveFilePath());

                var packet = new GoogleDriveFileSharePacket
                {
                    FileName = fileName,
                    ShareLink = link
                };

                PacketSender.SendToAllClients(packet);

                if (GameServerHardSync.IsHardSyncInProgress) {
                    GameServerHardSync.IsHardSyncInProgress = false;
                }
            });
            UploadSaveFile();
        }

        public static void UploadAndSendToClient(CSteamID requester)
        {
            GoogleDrive.Instance.Uploader.OnUploadFinished.RemoveAllListeners();
            GoogleDrive.Instance.Uploader.OnUploadFinished.AddListener((link) =>
            {
                string fileName = Path.GetFileName(SaveLoader.GetActiveSaveFilePath());

                var packet = new GoogleDriveFileSharePacket
                {
                    FileName = fileName,
                    ShareLink = link
                };

                PacketSender.SendToPlayer(requester, packet);
            });
            UploadSaveFile();
        }

        public static async void UploadSaveFile()
        {
            var path = SaveLoader.GetActiveSaveFilePath();
            SaveLoader.Instance.Save(path); // Saves current state to that file

            var folderId = await GoogleDrive.Instance.Uploader.GetOrCreateFolderAsync("Oxygen Not Included Multiplayer Saves");
            GoogleDrive.Instance.Uploader.UploadFile(path, folderId);
        }

    }
}

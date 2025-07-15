using ONI_MP.DebugTools;
using ONI_MP.Menus;
using ONI_MP.Misc;
using ONI_MP.Misc.World;
using ONI_MP.Networking.Packets.Architecture;
using ONI_MP.Networking.States;
using System;
using System.IO;

namespace ONI_MP.Networking.Packets.Cloud
{
    public class GoogleDriveFileSharePacket : IPacket
    {
        public string FileName;
        public string ShareLink;

        public PacketType Type => PacketType.GoogleDriveFileShare;

        public void Serialize(BinaryWriter writer)
        {
            writer.Write(FileName);
            writer.Write(ShareLink);
        }

        public void Deserialize(BinaryReader reader)
        {
            FileName = reader.ReadString();
            ShareLink = reader.ReadString();
        }

        public void OnDispatched()
        {
            if(MultiplayerSession.IsHost)
            {
                return; // Host does nothing here
            }

            if (Utils.IsInGame())
            {
                return;
            }

            DebugConsole.Log($"[GoogleDriveFileSharePacket] Received file share link for {FileName}: {ShareLink}");

            _ = SaveHelper.DownloadSaveAsync(
                    ShareLink,
                    FileName,
                    OnCompleted: () =>
                    {
                        DebugConsole.Log($"[GoogleDriveFileSharePacket] Download complete, loading {FileName}");
                        SaveHelper.LoadDownloadedSave(FileName);
                    },
                    OnFailed: () =>
                    {
                        DebugConsole.LogError($"[GoogleDriveFileSharePacket] Download failed for {FileName}");
                        //MultiplayerOverlay.Show("Could not download the world file from the host.");
                    }
                );

        }

    }
}

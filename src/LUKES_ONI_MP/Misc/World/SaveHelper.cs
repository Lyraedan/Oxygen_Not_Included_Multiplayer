using ONI_MP;
using ONI_MP.DebugTools;
using ONI_MP.Menus;
using ONI_MP.Misc;
using ONI_MP.Misc.World;
using ONI_MP.Networking;
using ONI_MP.Networking.Components;
using ONI_MP.Networking.Packets.Architecture;
using ONI_MP.Networking.States;
using Steamworks;
using System;
using System.Collections;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class SaveHelper
{

    public static int SAVEFILE_CHUNKSIZE_KB {
        get {
            return Configuration.GetHostProperty<int>("SaveFileTransferChunkKB");
        }
    }
    public static void RequestWorldLoad(WorldSave world)
    {
        SteamNetworkingComponent.scheduler.Run(() => LoadWorldSave(Path.GetFileNameWithoutExtension(world.Name), world.Data));
    }

    private static void LoadWorldSave(string name, byte[] data)
    {
        var savePath = SaveLoader.GetCloudSavesDefault() ? SaveLoader.GetCloudSavePrefix() : SaveLoader.GetSavePrefixAndCreateFolder();

        var baseName = Path.GetFileNameWithoutExtension(name);
        var path = SecurePath.Combine(savePath, baseName, $"{baseName}.sav");

        Directory.CreateDirectory(Path.GetDirectoryName(path));

        // Write save data safely
        using (var fs = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None))
        {
            using (var writer = new BinaryWriter(fs))
            {
                writer.Write(data);
                writer.Flush();
            }
        }

        // We've saved a copy of the downloaded world now load it
        GameClient.CacheCurrentServer();
        GameClient.Disconnect();
        GameClient.SetState(ClientState.LoadingWorld);
        PacketHandler.readyToProcess = false;
        MultiplayerOverlay.Show("Loading...");

        LoadScreen.DoLoad(path);
    }

    public static string WorldName
    {
        get
        {
            var activePath = SaveLoader.GetActiveSaveFilePath();
            return Path.GetFileNameWithoutExtension(activePath);
        }
    }

    public static byte[] GetWorldSave()
    {
        var path = SaveLoader.GetActiveSaveFilePath();
        SaveLoader.Instance.Save(path); // Saves current state to that file
        return File.ReadAllBytes(path);
    }

    /// <summary>
    /// Downloads a save file from a Google Drive share link to a known location. GOOGLE DRIVE DOES NOT NEED TO BE INITIALIZED HERE
    /// </summary>
    public static async Task DownloadSaveAsync(
    string shareLink,
    string fileName,
    System.Action OnCompleted,
    System.Action OnFailed
)
    {
        MultiplayerOverlay.Show("Downloading world from host...");

        try
        {
            var savePath = SaveLoader.GetCloudSavesDefault()
                ? SaveLoader.GetCloudSavePrefix()
                : SaveLoader.GetSavePrefixAndCreateFolder();

            var targetFile = SecurePath.Combine(
                savePath,
                Path.GetFileNameWithoutExtension(fileName),
                $"{Path.GetFileNameWithoutExtension(fileName)}.sav"
            );

            Directory.CreateDirectory(Path.GetDirectoryName(targetFile));

            var handler = new System.Net.Http.HttpClientHandler
            {
                AllowAutoRedirect = true
            };

            using (var http = new System.Net.Http.HttpClient(handler))
            {
                http.DefaultRequestHeaders.UserAgent.ParseAdd(
                    "Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:124.0) Gecko/20100101 Firefox/124.0"
                );

                using (var response = await http.GetAsync(
                           shareLink,
                           System.Net.Http.HttpCompletionOption.ResponseHeadersRead
                       ))
                {
                    response.EnsureSuccessStatusCode();

                    var totalSize = response.Content.Headers.ContentLength ?? 0L;
                    var downloaded = 0L;
                    var startTime = System.DateTime.UtcNow;

                    using (var stream = await response.Content.ReadAsStreamAsync())
                    using (var fs = new FileStream(targetFile, FileMode.Create, FileAccess.Write, FileShare.None))
                    {
                        var buffer = new byte[65536]; // 64KB buffer
                        int read;
                        while ((read = await stream.ReadAsync(buffer, 0, buffer.Length)) > 0)
                        {
                            await fs.WriteAsync(buffer, 0, read);
                            downloaded += read;

                            int percent = totalSize > 0 ? (int)(downloaded * 100.0 / totalSize) : 0;

                            var elapsed = System.DateTime.UtcNow - startTime;
                            double elapsedSeconds = elapsed.TotalSeconds > 0 ? elapsed.TotalSeconds : 1;
                            double bytesPerSecond = downloaded / elapsedSeconds;

                            var remainingBytes = totalSize - downloaded;
                            var estimatedRemainingSeconds = bytesPerSecond > 0
                                ? remainingBytes / bytesPerSecond
                                : 0;

                            var timeLeft = TimeSpan.FromSeconds(estimatedRemainingSeconds);
                            string timeLeftStr = $"{Utils.FormatTime(timeLeft.TotalSeconds)} remaining";

                            string speedStr = Utils.FormatBytes((long)bytesPerSecond) + "/s";

                            MultiplayerOverlay.Show(
                                $"Downloading world from host: {percent}%\n({speedStr}, {timeLeftStr})"
                            );
                        }
                    }
                }
            }

            DebugConsole.Log("Download complete!");
            MultiplayerOverlay.Show("Download complete!");
            DebugConsole.Log($"[SaveHelper] Downloaded to: {targetFile}");
            OnCompleted?.Invoke();
        }
        catch (Exception ex)
        {
            MultiplayerOverlay.Show($"Download failed: {ex.Message}");
            DebugConsole.LogError($"[SaveHelper] Download failed: {ex.Message}");
            OnFailed?.Invoke();
        }
    }


    public static void LoadDownloadedSave(string fileName)
    {
        var savePath = SaveLoader.GetCloudSavesDefault()
            ? SaveLoader.GetCloudSavePrefix()
            : SaveLoader.GetSavePrefixAndCreateFolder();

        var targetFile = SecurePath.Combine(
            savePath,
            Path.GetFileNameWithoutExtension(fileName),
            $"{Path.GetFileNameWithoutExtension(fileName)}.sav"
        );

        if (!File.Exists(targetFile))
        {
            MultiplayerOverlay.Show("Downloaded save file not found.");
            DebugConsole.LogError($"[SaveHelper] Could not find file to load: {targetFile}");
            return;
        }

        // We've saved a copy of the downloaded world, now load it
        GameClient.CacheCurrentServer();
        GameClient.Disconnect();
        GameClient.SetState(ClientState.LoadingWorld);
        PacketHandler.readyToProcess = false;
        MultiplayerOverlay.Show("Loading...");

        LoadScreen.DoLoad(targetFile); // use the correct variable
    }

}

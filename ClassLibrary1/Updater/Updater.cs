using System;
using System.IO;
using ONI_MP.DebugTools;
using Steamworks;
using UnityEngine;

namespace ONI_MP.Updater
{
    public static class Updater
    {
        private static readonly PublishedFileId_t WORKSHOP_ID = new PublishedFileId_t(3630759126);

        public static System.Action OnUpdateAvailable;
        public static System.Action OnUptoDate;

        public static void CheckForUpdate()
        {
            if (!SteamAPI.IsSteamRunning() || !SteamAPI.Init())
            {
                DebugConsole.LogError("[Updater] Failed to initialize SteamAPI.", false);
                return;
            }

            UGCQueryHandle_t queryHandle = SteamUGC.CreateQueryUGCDetailsRequest(new PublishedFileId_t[] { WORKSHOP_ID }, 1);
            SteamAPICall_t apiCall = SteamUGC.SendQueryUGCRequest(queryHandle);
            CallResult<SteamUGCQueryCompleted_t> result = CallResult<SteamUGCQueryCompleted_t>.Create(OnUGCQueryCompleted);
            result.Set(apiCall);

            DebugConsole.Log("[Updater] Sent workshop query for update check.");
        }

        private static void OnUGCQueryCompleted(SteamUGCQueryCompleted_t data, bool bIOFailure)
        {
            if (bIOFailure || data.m_eResult != EResult.k_EResultOK)
            {
                DebugConsole.LogError("[Updater] Workshop query failed!", false);
                return;
            }

            SteamUGCDetails_t details;
            bool ok = SteamUGC.GetQueryUGCResult(data.m_handle, 0, out details);

            if (!ok)
            {
                DebugConsole.LogError("[Updater] Failed to get UGC result.", false);
                return;
            }

            System.DateTime workshopUpdated = System.DateTimeOffset.FromUnixTimeSeconds(details.m_rtimeUpdated).UtcDateTime;

            DebugConsole.Log($"[Updater] Workshop last updated at {workshopUpdated.ToLocalTime()}");
            CompareLocalModVersion(details.m_nPublishedFileId.m_PublishedFileId, workshopUpdated);
        }

        private static void CompareLocalModVersion(ulong fileId, System.DateTime workshopUpdated)
        {
            string SteamPath = Path.Combine(KMod.Manager.GetDirectory(), "Steam");
#if DEBUG
            SteamPath = Path.Combine(KMod.Manager.GetDirectory(), "dev"); // Goto the dev folder instead
#endif

            string localPath = Path.Combine(SteamPath, fileId.ToString());
            if (!Directory.Exists(localPath))
            {
                // Just in case
                Debug.LogWarning("[Updater] Local mod folder not found. It may not be installed.");
                return;
            }

            System.DateTime localModTime = Directory.GetLastWriteTimeUtc(localPath);

            if (workshopUpdated > localModTime)
            {
                DebugConsole.Log("[Updater] Update available!");
                OnUpdateAvailable?.Invoke();
            }
            else
            {
                DebugConsole.Log("[Updater] Mod is up to date.");
                OnUptoDate?.Invoke();
            }
        }
    }
}

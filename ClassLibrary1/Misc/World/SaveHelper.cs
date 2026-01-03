using HarmonyLib;
using Klei;
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
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

public static class SaveHelper
{

	public static int SAVEFILE_CHUNKSIZE_KB
	{
		get
		{
			return Math.Max(64, Configuration.GetHostProperty<int>("SaveFileTransferChunkKB"));
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

		if (!SavegameDlcListValid(data, out string errorMsg))
		{
			ShowMessageAndReturnToMainMenu(errorMsg);
			return;
		}

		// We've saved a copy of the downloaded world now load it
		GameClient.CacheCurrentServer();
		GameClient.Disconnect();
		GameClient.SetState(ClientState.LoadingWorld);
		PacketHandler.readyToProcess = false;
		MultiplayerOverlay.Show(global::STRINGS.UI.FRONTEND.LOADING);

		LoadScreen.DoLoad(path);
	}
	public static void ShowMessageAndReturnToMainMenu(string msg)
	{
		CoroutineRunner.RunOne(ShowMessageAndReturnToTitle(msg));
	}

	private static IEnumerator ShowMessageAndReturnToTitle(string msg = null)
	{
		// This is stupid
		try
		{
			if (msg == null)
				msg = MP_STRINGS.UI.MP_OVERLAY.CLIENT.LOST_CONNECTION;

			MultiplayerOverlay.Show(msg);
		}
		catch (Exception e)
		{
			// Something went wrong
			MultiplayerOverlay.Close();
			App.LoadScene("frontend");
		}

		yield return new WaitForSeconds(5);

		try
		{
			MultiplayerOverlay.Close();
			NetworkIdentityRegistry.Clear();
			SteamLobby.LeaveLobby();

			App.LoadScene("frontend");
		}
		catch (Exception e)
		{
			MultiplayerOverlay.Close();
			// Something else went wrong
			App.LoadScene("frontend");
		}
	}


	[HarmonyPatch(typeof(KMod.Manager), nameof(KMod.Manager.Install))]
	public class Manager_Install_Patch
	{
		public static void Postfix(KMod.Mod mod)
		{
			if (mod.label.distribution_platform != KMod.Label.DistributionPlatform.Steam
			|| !ulong.TryParse(mod.label.id, out var localId))
				return;

			RefreshMissingModList();
		}
	}

	static void RefreshMissingModList()
	{
		var mng = Global.Instance.modManager;
		foreach (var mod in Global.Instance.modManager.mods)
		{
			if (mod.label.distribution_platform != KMod.Label.DistributionPlatform.Steam
			|| !ulong.TryParse(mod.label.id, out var localId))
				continue;
			if(MissingModIds.Contains(localId) && mod.status == KMod.Mod.Status.Installed)
			{
				MissingModIds.Remove(localId);
				DebugConsole.Log("enabling freshly installed mod " + mod.title + ", remaining missing mods: " + MissingModIds.Count);
				mod.SetEnabledForActiveDlc(true);
			}
		}
		mng.Save();
		if (!MissingModIds.Any())
		{
			App.instance.Restart();
		}
	}

	static HashSet<ulong> MissingModIds = [];
	internal static void SyncModsAndRestart(HashSet<ulong> notEnabled, HashSet<ulong> notDisabled, HashSet<ulong> missingMods)
	{
		var mng = Global.Instance.modManager;
		foreach (var mod in Global.Instance.modManager.mods)
		{
			if (mod.label.distribution_platform != KMod.Label.DistributionPlatform.Steam
			|| !ulong.TryParse(mod.label.id, out var localId))
				continue;
			if (notDisabled.Contains(localId))
				mod.SetEnabledForActiveDlc(false);
			else if (notEnabled.Contains(localId))
				mod.SetEnabledForActiveDlc(true);
		}
		MissingModIds = missingMods;
		mng.Save();
		SubToAllMissing();
	}
	public static void SubToAllMissing()
	{
		foreach (ulong id in MissingModIds)
			SubToMissing(id);
	}

	static void SubToMissing(ulong steamID)
	{
		SteamUGC.SubscribeItem(new PublishedFileId_t(steamID));
	}
	static StringBuilder sb = new();
	public static bool SteamModListSynced(List<ulong> steamMods, out HashSet<ulong> toEnable, out HashSet<ulong> toDisable, out HashSet<ulong> missingMods)
	{
		//response = null;
		//return true;
		sb.Clear();

		HashSet<ulong> modsToBeActive = steamMods.ToHashSet();
		HashSet<ulong> currentlyActiveSteamMods = [];
		toEnable = [];
		toDisable = [];
		int diffCount = 0;


		foreach (var mod in Global.Instance.modManager.mods)
		{
			if (mod.label.distribution_platform != KMod.Label.DistributionPlatform.Steam
			|| !ulong.TryParse(mod.label.id, out var localId))
				continue;

			bool isCurrentlyActive = mod.IsEnabledForActiveDlc();
			if (modsToBeActive.Contains(localId) != isCurrentlyActive)
			{
				diffCount++;
				DebugConsole.Log("mod " + mod.title + " is different from host, it should be: " + (isCurrentlyActive ? "disabled" : "enabled"));

				if (isCurrentlyActive)
					toDisable.Add(localId);
				else
					toEnable.Add(localId);
			}
			modsToBeActive.Remove(localId);
		}
		missingMods = [.. modsToBeActive];


		return diffCount == 0;
	}


	public static bool SavegameDlcListValid(IEnumerable<string> dlcIds, out string errorMsg)
	{
		errorMsg = string.Empty;
		HashSet<string> missingDLCs = new HashSet<string>();

		bool spacedOutSave = dlcIds.Contains(DlcManager.EXPANSION1_ID);

		foreach (var dlcId in dlcIds)
		{
			if (!DlcManager.IsContentSubscribed(dlcId))
			{
				DebugConsole.LogWarning($"[SaveHelper] Missing DLC required by savegame: {dlcId}");
				missingDLCs.Add(dlcId);
			}
		}
		if (spacedOutSave != DlcManager.IsExpansion1Active())
		{
			errorMsg = spacedOutSave
				? MP_STRINGS.UI.MP_OVERLAY.SYNC.DLCSYNC.WRONGDLC_BASEGAME
				: MP_STRINGS.UI.MP_OVERLAY.SYNC.DLCSYNC.WRONGDLC_SPACEDOUT;
			return false;
		}


		if (missingDLCs.Any())
		{
			errorMsg = MP_STRINGS.UI.MP_OVERLAY.SYNC.DLCSYNC.WRONGDLC_LISTHEADER + "\n" + string.Join(", ", missingDLCs.Select(id => DlcManager.GetDlcTitleNoFormatting(id)));
			return false;
		}
		return true;
	}

	public static bool SavegameDlcListValid(byte[] saveBytes, out string errorMsg)
	{
		errorMsg = null;
		IReader reader = new FastReader(saveBytes);
		//read the gameInfo to advance the filereader
		SaveGame.GameInfo gameInfo = SaveGame.GetHeader(reader, out SaveGame.Header header, "MP-Mod-Server-Save");
		///check if all dlcs of the savegame are currently active


		return SavegameDlcListValid(gameInfo.dlcIds, out errorMsg);

		///this is for later use if we want game mod syncing
		KSerialization.Manager.DeserializeDirectory(reader);
		if (header.IsCompressed)
		{
			int length = saveBytes.Length - reader.Position;
			byte[] compressedBytes = new byte[length];
			Array.Copy((Array)saveBytes, reader.Position, compressedBytes, 0, length);
			byte[] uncompressedBytes = SaveLoader.DecompressContents(compressedBytes);
			reader = new FastReader(uncompressedBytes);
		}

		Debug.Assert(reader.ReadKleiString() == "world");
		KSerialization.Deserializer deserializer = new KSerialization.Deserializer(reader);
		SaveFileRoot saveFileRoot = new();
		deserializer.Deserialize(saveFileRoot);
		if ((gameInfo.saveMajorVersion == 7 || gameInfo.saveMinorVersion < 8) && saveFileRoot.requiredMods != null)
		{
			saveFileRoot.active_mods = new List<KMod.Label>();
			foreach (ModInfo requiredMod in saveFileRoot.requiredMods)
				saveFileRoot.active_mods.Add(new KMod.Label()
				{
					id = requiredMod.assetID,
					version = (long)requiredMod.lastModifiedTime,
					distribution_platform = KMod.Label.DistributionPlatform.Steam,
					title = requiredMod.description
				});
			saveFileRoot.requiredMods.Clear();
		}

		var activeSaveMods = saveFileRoot.active_mods;

		KMod.Manager modManager = Global.Instance.modManager;
		_differenceCount = 0;
		HashSet<string> activeModsInSave = activeSaveMods.Select(mod => mod.defaultStaticID).ToHashSet(); //change to "id" if the check should be platform agnostic
		_activeModListIdOrder = new(activeModsInSave);
		ActiveModlistModIds = new(activeModsInSave);

		foreach (var mod in modManager.mods)
		{
			bool isCurrentlyActive = mod.IsEnabledForActiveDlc();
			if (activeModsInSave.Contains(mod.label.defaultStaticID))
			{
				if (!isCurrentlyActive)
					++_differenceCount;
				activeModsInSave.Remove(mod.label.defaultStaticID);
			}
			else
			{
				if (isCurrentlyActive)
					++_differenceCount;
			}
		}
		_missingMods = [.. activeModsInSave];
	}
	static int _differenceCount = 0;
	static HashSet<string> _missingMods = [];
	static List<string> _activeModListIdOrder = [];
	static HashSet<string> ActiveModlistModIds = [];

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
	/// Saves the current world snapshot
	/// </summary>
	public static void CaptureWorldSnapshot()
	{
		if (Utils.IsInMenu())
		{
			// We are not in game, ignore
			return;
		}

		var path = SaveLoader.GetActiveSaveFilePath();
		SaveLoader.Instance.Save(path); // Saves current state to that file
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
			MultiplayerOverlay.Show(MP_STRINGS.UI.MP_OVERLAY.CLIENT.MISSING_SAVE_FILE);
			DebugConsole.LogError($"[SaveHelper] Could not find file to load: {targetFile}");
			return;
		}

		// We've saved a copy of the downloaded world, now load it
		GameClient.CacheCurrentServer();
		GameClient.Disconnect();
		GameClient.SetState(ClientState.LoadingWorld);
		PacketHandler.readyToProcess = false;
		MultiplayerOverlay.Show(global::STRINGS.UI.FRONTEND.LOADING);

		LoadScreen.DoLoad(targetFile); // use the correct variable
	}

}

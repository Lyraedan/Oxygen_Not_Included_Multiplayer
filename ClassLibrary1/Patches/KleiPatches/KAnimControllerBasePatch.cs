using HarmonyLib;
using Klei.AI;
using ONI_MP.DebugTools;
using ONI_MP.Networking;
using ONI_MP.Networking.Components;
using ONI_MP.Networking.Packets.Core;
using ONI_MP.Networking.Packets.DuplicantActions;
using System;
using System.Linq;

namespace ONI_MP.Patches.KleiPatches
{
	class KAnimControllerBasePatch
	{
		// Patch: Play(HashedString, KAnim.PlayMode, float, float)

		[HarmonyPatch(typeof(KAnimControllerBase))]
		[HarmonyPrefix]
		[HarmonyPatch(nameof(KAnimControllerBase.Play), [
						typeof(HashedString), typeof(KAnim.PlayMode), typeof(float), typeof(float)
				])]
		static void Play_Single_Prefix(KAnimControllerBase __instance, HashedString anim_name, KAnim.PlayMode mode, float speed, float time_offset)
		{
			try
			{
				if (__instance == null || !__instance.enabled)
					return;

				var go = __instance.gameObject;
				if (go.TryGetComponent<KPrefabID>(out var id) &&
						id.HasTag(GameTags.Minions.Models.Standard) &&
						MultiplayerSession.IsHost &&
						go.TryGetComponent<NetworkIdentity>(out var netIdentity))
				{
					var packet = new PlayAnimPacket
					{
						NetId = netIdentity.NetId,
						IsMulti = false,
						SingleAnimHash = anim_name.HashValue,
						Mode = mode,
						Speed = speed,
						Offset = time_offset
					};

					PacketSender.SendToAllClients(packet);
				}
			}
			catch (System.Exception) { }
		}

		// Patch: Play(HashedString[], KAnim.PlayMode)

		[HarmonyPatch(typeof(KAnimControllerBase))]
		[HarmonyPrefix]
		[HarmonyPatch(nameof(KAnimControllerBase.Play), [
						typeof(HashedString[]), typeof(KAnim.PlayMode)
				])]
		static void Play_Multi_Prefix(KAnimControllerBase __instance, HashedString[] anim_names, KAnim.PlayMode mode)
		{
			try
			{
				if (__instance == null || anim_names == null || anim_names.Length == 0 || !__instance.enabled)
					return;

				var go = __instance.gameObject;
				if (go.TryGetComponent<KPrefabID>(out var id) &&
						id.HasTag(GameTags.Minions.Models.Standard) &&
						MultiplayerSession.IsHost &&
						go.TryGetComponent<NetworkIdentity>(out var netIdentity))
				{
					string allAnims = string.Join(", ", anim_names.Select(a => a.ToString()));
					//DebugConsole.Log($"[ONI_MP] Dupe '{go.name}' playing anims [{allAnims}] | Mode: {mode}");

					var packet = new PlayAnimPacket
					{
						NetId = netIdentity.NetId,
						IsMulti = true,
						AnimHashes = anim_names.Select(a => a.HashValue).ToList(),
						Mode = mode,
						Speed = 1f,   // Defaults, Play(string[]) doesn’t use them
						Offset = 0f
					};

					PacketSender.SendToAllClients(packet);
				}
			}
			catch (System.Exception) { }
		}
		// Patch: Queue(HashedString, KAnim.PlayMode, float, float)

		[HarmonyPatch(typeof(KAnimControllerBase))]
		[HarmonyPrefix]
		[HarmonyPatch(nameof(KAnimControllerBase.Queue), [
						typeof(HashedString), typeof(KAnim.PlayMode), typeof(float), typeof(float)
				])]
		static void Queue_Single_Prefix(KAnimControllerBase __instance, HashedString anim_name, KAnim.PlayMode mode, float speed, float time_offset)
		{
			try
			{
				if (__instance == null || !__instance.enabled) return;

				var go = __instance.gameObject;
				if (go.TryGetComponent<KPrefabID>(out var id) &&
						id.HasTag(GameTags.BaseMinion) &&
						MultiplayerSession.IsHost &&
						go.TryGetComponent<NetworkIdentity>(out var netIdentity))
				{
					var packet = new PlayAnimPacket
					{
						NetId = netIdentity.NetId,
						IsMulti = false,
						SingleAnimHash = anim_name.HashValue,
						Mode = mode,
						Speed = speed,
						Offset = time_offset,
						IsQueue = true
					};

					PacketSender.SendToAllClients(packet);
				}
			}
			catch (System.Exception) { }
		}



		private static bool TogglingOverrideFromPacket = false;
		internal static void AddKanimOverride(KAnimControllerBase kbac, string kanim, object shouldSave)
		{
			TogglingOverrideFromPacket = true;
			TogglingOverrideFromPacket = false;
		}

		internal static void RemoveKanimOverride(KAnimControllerBase kbac, string kanim)
		{
			TogglingOverrideFromPacket = true;
			TogglingOverrideFromPacket = false;
		}


		[HarmonyPatch(typeof(KAnimControllerBase), nameof(KAnimControllerBase.AddAnimOverrides))]
		public class KAnimControllerBase_AddAnimOverrides_Patch
		{
			public static bool Prefix(KAnimControllerBase __instance, KAnimFile kanim_file, float priority = 0f)
			{
				if (!MultiplayerSession.InSession) return true;

				//leave to minions for now, potentially remove later
				if (!__instance.HasTag(GameTags.BaseMinion))
					return true;

				if (MultiplayerSession.IsClient)
					return TogglingOverrideFromPacket;
				
				PacketSender.SendToAllClients(new ToggleAnimOverridePacket(__instance.gameObject, kanim_file, priority));
				return true;
			}
		}

		[HarmonyPatch(typeof(KAnimControllerBase), nameof(KAnimControllerBase.RemoveAnimOverrides))]
		public class KAnimControllerBase_RemoveAnimOverrides_Patch
		{
			public static bool Prefix(KAnimControllerBase __instance, KAnimFile kanim_file)
			{
				if (!MultiplayerSession.InSession) return true;

				//leave to minions for now, potentially remove later
				if (!__instance.HasTag(GameTags.BaseMinion))
					return true;

				if (MultiplayerSession.IsClient)
					return TogglingOverrideFromPacket;
				
				PacketSender.SendToAllClients(new ToggleAnimOverridePacket(__instance.gameObject, kanim_file));
				return true;
			}
		}
	}
}

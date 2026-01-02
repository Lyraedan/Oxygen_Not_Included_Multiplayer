using HarmonyLib;
using ONI_MP.Networking;
using ONI_MP.Networking.Components;
using System.Linq;

namespace ONI_MP.Patches.KleiPatches
{
	[HarmonyPatch(typeof(KAnimControllerBase))]
	class KAnimControllerBasePatch
	{
		// Patch: Play(HashedString, KAnim.PlayMode, float, float)
		[HarmonyPrefix]
		[HarmonyPatch(nameof(KAnimControllerBase.Play), new[] {
						typeof(HashedString), typeof(KAnim.PlayMode), typeof(float), typeof(float)
				})]
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
		[HarmonyPrefix]
		[HarmonyPatch(nameof(KAnimControllerBase.Play), new[] {
						typeof(HashedString[]), typeof(KAnim.PlayMode)
				})]
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
		[HarmonyPrefix]
		[HarmonyPatch(nameof(KAnimControllerBase.Queue), new[] {
						typeof(HashedString), typeof(KAnim.PlayMode), typeof(float), typeof(float)
				})]
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
	}
}

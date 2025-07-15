using ONI_MP.DebugTools;
using ONI_MP.Networking;
using ONI_MP.Networking.Components;
using ONI_MP.Networking.Packets.Architecture;
using UnityEngine;
using System.Linq;
using HarmonyLib;

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
            if (__instance == null || !__instance.enabled)
                return;

            var go = __instance.gameObject;
            if (go.TryGetComponent<KPrefabID>(out var id) &&
                id.HasTag(GameTags.Minions.Models.Standard) &&
                MultiplayerSession.IsHost &&
                go.TryGetComponent<NetworkIdentity>(out var netIdentity))
            {
                //DebugConsole.Log($"[ONI_MP] Dupe '{go.name}' playing anim '{anim_name}' | Mode: {mode}, Speed: {speed}, Offset: {time_offset}");

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

        // Patch: Play(HashedString[], KAnim.PlayMode)
        [HarmonyPrefix]
        [HarmonyPatch(nameof(KAnimControllerBase.Play), new[] {
            typeof(HashedString[]), typeof(KAnim.PlayMode)
        })]
        static void Play_Multi_Prefix(KAnimControllerBase __instance, HashedString[] anim_names, KAnim.PlayMode mode)
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
    }
}

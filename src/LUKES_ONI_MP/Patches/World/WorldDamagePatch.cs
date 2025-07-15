using System.Security.Principal;
using HarmonyLib;
using ONI_MP.DebugTools;
using ONI_MP.Networking;
using ONI_MP.Networking.Components;
using ONI_MP.Networking.Packets.Architecture;
using ONI_MP.Networking.Packets.World;
using UnityEngine;
using static STRINGS.UI.SANDBOXTOOLS.SETTINGS;

namespace ONI_MP.Patches.World
{
    [HarmonyPatch(typeof(WorldDamage), nameof(WorldDamage.OnDigComplete))]
    public static class WorldDamagePatch
    {
        [HarmonyPrefix]
        public static bool Prefix(int cell,float mass,float temperature,ushort element_idx,byte disease_idx, int disease_count)
        {
            OnDigCompletedUpdated(cell, mass, temperature, element_idx, disease_idx, disease_count);
            return false;
        }

        private static void OnDigCompletedUpdated(int cell, float mass, float temperature, ushort element_idx, byte disease_idx, int disease_count)
        {
            Vector3 vector = Grid.CellToPos(cell, CellAlignment.RandomInternal, Grid.SceneLayer.Ore);
            Element element = ElementLoader.elements[element_idx];
            Grid.Damage[cell] = 0f;
            InvokePlaySoundForSubstance(element, vector);
            //Instance.PlaySoundForSubstance(element, vector);
            float num = mass * 0.5f;
            if (!(num <= 0f))
            {
                GameObject gameObject = element.substance.SpawnResource(vector, num, temperature, disease_idx, disease_count);
                NetworkIdentity networkIdentity = gameObject.GetComponent<NetworkIdentity>();

                Pickupable component = gameObject.GetComponent<Pickupable>();
                if (component != null && component.GetMyWorld() != null && component.GetMyWorld().worldInventory.IsReachable(component))
                {
                    PopFXManager.Instance.SpawnFX(PopFXManager.Instance.sprite_Resource, Mathf.RoundToInt(num) + " " + element.name, gameObject.transform);
                }

                if (MultiplayerSession.IsHost)
                {
                    Vector3 pos = Grid.CellToPos(cell, CellAlignment.RandomInternal, Grid.SceneLayer.Ore);

                    var packet = new WorldDamageSpawnResourcePacket
                    {
                        NetId = networkIdentity.NetId,
                        Position = pos,
                        Mass = mass * 0.5f,
                        Temperature = temperature,
                        ElementIndex = element_idx,
                        DiseaseIndex = disease_idx,
                        DiseaseCount = disease_count
                    };

                    PacketSender.SendToAllClients(packet);
                    DebugConsole.Log("Sent spawn resource packet with netid " + networkIdentity.NetId);
                }
            }
        }

        private static void InvokePlaySoundForSubstance(Element element, Vector3 position)
        {
            var method = typeof(WorldDamage).GetMethod("PlaySoundForSubstance", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            if (method == null)
            {
                DebugConsole.LogWarning("[Multiplayer] Could not find PlaySoundForSubstance via reflection.");
                return;
            }

            var worldDamage = WorldDamage.Instance;

            if (worldDamage == null)
            {
                DebugConsole.LogWarning("[Multiplayer] WorldDamage.Instance is null.");
                return;
            }

            method.Invoke(worldDamage, new object[] { element, position });
        }
    }
}

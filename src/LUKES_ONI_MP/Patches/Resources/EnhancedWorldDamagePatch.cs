using HarmonyLib;
using ONI_MP.DebugTools;
using ONI_MP.Networking;
using ONI_MP.Networking.Components;
using ONI_MP.Networking.Packets.Resources;
using ONI_MP.Networking.Packets.Architecture;
using UnityEngine;

namespace ONI_MP.Patches.Resources
{
    /// <summary>
    /// Enhanced WorldDamage patch that prevents double resource spawning using the new ResourceDropPacket system.
    /// </summary>
    [HarmonyPatch(typeof(WorldDamage), nameof(WorldDamage.OnDigComplete))]
    public static class EnhancedWorldDamagePatch
    {
        [HarmonyPrefix]
        public static bool Prefix(int cell, float mass, float temperature, ushort element_idx, byte disease_idx, int disease_count)
        {
            ProcessDigComplete(cell, mass, temperature, element_idx, disease_idx, disease_count);
            return false; // Prevent original method from running
        }

        private static void ProcessDigComplete(int cell, float mass, float temperature, ushort element_idx, byte disease_idx, int disease_count)
        {
            Vector3 position = Grid.CellToPos(cell, CellAlignment.RandomInternal, Grid.SceneLayer.Ore);
            Element element = ElementLoader.elements[element_idx];
            Grid.Damage[cell] = 0f;
            
            // Play sound effect
            InvokePlaySoundForSubstance(element, position);
            
            float dropMass = mass * 0.5f;
            if (dropMass <= 0f) return;

            if (MultiplayerSession.IsHost)
            {
                // Host: spawn resource and send packet to clients
                var gameObject = element.substance.SpawnResource(position, dropMass, temperature, disease_idx, disease_count);
                var networkIdentity = gameObject.GetComponent<NetworkIdentity>();
                
                if (networkIdentity == null)
                {
                    networkIdentity = gameObject.AddComponent<NetworkIdentity>();
                    networkIdentity.RegisterIdentity();
                }

                // Show popup for host
                ShowResourcePopup(gameObject, dropMass, element);

                // Send packet to prevent clients from spawning their own copy
                var packet = new ResourceDropPacket(
                    MultiplayerSession.LocalSteamID,
                    networkIdentity.NetId,
                    element.tag.Name,
                    dropMass,
                    temperature,
                    disease_idx,
                    disease_count,
                    position,
                    ResourceDropPacket.DropReason.ToolUsage,
                    -1, // No specific source
                    true  // Prevent local spawn on clients
                );
                
                PacketSender.SendToAllClients(packet);
                DebugConsole.Log($"[EnhancedWorldDamagePatch] Host spawned {dropMass:F2}kg {element.name} from digging at {position}");
            }
            else
            {
                // Clients: DO NOT spawn resource locally - wait for packet from host
                DebugConsole.Log($"[EnhancedWorldDamagePatch] Client ignoring dig resource spawn - waiting for host packet");
            }
        }

        private static void ShowResourcePopup(GameObject gameObject, float mass, Element element)
        {
            var pickupable = gameObject.GetComponent<Pickupable>();
            if (pickupable != null && pickupable.GetMyWorld() != null && pickupable.GetMyWorld().worldInventory.IsReachable(pickupable))
            {
                PopFXManager.Instance.SpawnFX(
                    PopFXManager.Instance.sprite_Resource,
                    Mathf.RoundToInt(mass) + " " + element.name,
                    gameObject.transform
                );
            }
        }

        private static void InvokePlaySoundForSubstance(Element element, Vector3 position)
        {
            var method = typeof(WorldDamage).GetMethod("PlaySoundForSubstance", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            if (method == null)
            {
                DebugConsole.LogWarning("[EnhancedWorldDamagePatch] Could not find PlaySoundForSubstance via reflection.");
                return;
            }

            var worldDamage = WorldDamage.Instance;
            if (worldDamage == null)
            {
                DebugConsole.LogWarning("[EnhancedWorldDamagePatch] WorldDamage.Instance is null.");
                return;
            }

            method.Invoke(worldDamage, new object[] { element, position });
        }
    }
}

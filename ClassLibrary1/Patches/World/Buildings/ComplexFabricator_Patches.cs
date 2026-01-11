using HarmonyLib;
using ONI_MP.DebugTools;
using ONI_MP.Networking;
using ONI_MP.Networking.Packets.World.Buildings;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ONI_MP.Patches.World.Buildings
{
	internal class ComplexFabricator_Patches
	{

        [HarmonyPatch(typeof(ComplexFabricator), nameof(ComplexFabricator.SpawnOrderProduct))]
        public class ComplexFabricator_SpawnOrderProduct_Patch
        {
            public static void Postfix(ComplexFabricator __instance)
			{
				DebugConsole.Log("ComplexFabricator_SpawnOrderProduct_Patch called");
				if (!MultiplayerSession.InSession || !MultiplayerSession.IsHost)
					return;

				DebugConsole.Log("ComplexFabricator_SpawnOrderProduct_Patch spawning product");
				PacketSender.SendToAllClients(new ComplexFabricatorSpawnProductPacket(__instance));
			}
        }
	}
}

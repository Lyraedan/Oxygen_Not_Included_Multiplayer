
using HarmonyLib;
using ONI_MP.Misc;
using ONI_MP.Networking;
using ONI_MP.Networking.Packets.Animation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ONI_MP.Patches.KleiPatches
{
	internal class KBatchedAnimController_Patches
	{

        [HarmonyPatch(typeof(KBatchedAnimController), nameof(KBatchedAnimController.SetSymbolVisiblity))]
        public class KBatchedAnimController_SetSymbolVisiblity_Patch
        {
            public static void Prefix(KBatchedAnimController __instance, KAnimHashedString symbol, bool is_visible)
            {
                if(!Utils.IsHostMinion(__instance))
                    return;


				PacketSender.SendToAllClients(new SymbolVisibilityTogglePacket(__instance, symbol, is_visible));
			}
        }
	}   
}

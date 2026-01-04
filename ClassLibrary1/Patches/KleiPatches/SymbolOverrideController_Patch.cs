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
	internal class SymbolOverrideController_Patch
	{

        [HarmonyPatch(typeof(SymbolOverrideController), nameof(SymbolOverrideController.AddSymbolOverride))]
        public class SymbolOverrideController_AddSymbolOverride_Patch
        {
            public static void Prefix(SymbolOverrideController __instance, HashedString target_symbol, KAnim.Build.Symbol source_symbol, int priority = 0)
            {
                if(!Utils.IsHostMinion(__instance))
                    return;
                PacketSender.SendToAllClients(new SymbolOverridePacket(__instance, SymbolOverridePacket.Mode.AddSymbolOverride, target_symbol,source_symbol,priority));
			}
        }

        [HarmonyPatch(typeof(SymbolOverrideController), nameof(SymbolOverrideController.RemoveSymbolOverride))]
        public class SymbolOverrideController_RemoveSymbolOverride_Patch
        {
            public static void Prefix(SymbolOverrideController __instance, HashedString target_symbol, int priority)
			{
				if (!Utils.IsHostMinion(__instance))
					return;
				PacketSender.SendToAllClients(new SymbolOverridePacket(__instance, SymbolOverridePacket.Mode.RemoveSymbolOverride, target_symbol, priority: priority));
			}
        }

        [HarmonyPatch(typeof(SymbolOverrideController), nameof(SymbolOverrideController.RemoveAllSymbolOverrides))]
        public class SymbolOverrideController_RemoveAllSymbolOverrides_Patch
        {
            public static void Prefix(SymbolOverrideController __instance, int priority)
			{
				if (!Utils.IsHostMinion(__instance))
					return;
				PacketSender.SendToAllClients(new SymbolOverridePacket(__instance, SymbolOverridePacket.Mode.RemoveAllSymbolsOverrides, priority: priority));
			}
        }
	}
}

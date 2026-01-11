using HarmonyLib;
using ONI_MP.Networking;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ONI_MP.Patches.Duplicant
{
	internal class ConversationManager_Patches
	{

        [HarmonyPatch(typeof(ConversationManager), nameof(ConversationManager.Sim200ms))]
        public class ConversationManager_Sim200ms_Patch
        {
            public static bool Prefix(ConversationManager __instance)
            {
                return !MultiplayerSession.IsClient;
            }
        }
	}
}

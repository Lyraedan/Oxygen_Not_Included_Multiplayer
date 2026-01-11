using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ONI_MP.Networking.Packets.World
{
	internal class ColonyDiagnostic_Patches
	{

        [HarmonyPatch(typeof(ColonyDiagnostic), nameof(ColonyDiagnostic.Evaluate))]
        public class ColonyDiagnostic_Evaluate_Patch
        {
            public static bool Prefix(ColonyDiagnostic __instance) => !MultiplayerSession.IsClient;
        }
	}
}

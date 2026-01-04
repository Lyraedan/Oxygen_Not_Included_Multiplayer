using HarmonyLib;
using ONI_MP.Networking;
using ONI_MP.Networking.Packets.World;
using ONI_MP.Networking.Packets.World.Buildings;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ONI_MP.Patches.World
{
	internal class MinionIdentity_Patches
	{
		static bool ApplyingPacket = false;
		public static void ApplyPacketName(MinionIdentity nameable, string name)
		{
			ApplyingPacket = true;
			nameable.SetName(name);
			ApplyingPacket = false;
		}

		[HarmonyPatch(typeof(MinionIdentity), nameof(MinionIdentity.SetName))]
		public class MinionIdentity_SetName_Patch
		{
			public static void Postfix(MinionIdentity __instance, string name)
			{
				if (MultiplayerSession.NotInSession)
					return;

				if (ApplyingPacket)
					return;
				PacketSender.SendToAllOtherPeers(new MinionIdentitySetNamePacket(__instance.GetNetId(), name));
			}
		}
	}
}

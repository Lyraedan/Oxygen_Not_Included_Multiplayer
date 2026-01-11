using ONI_MP.Networking;
using ONI_MP.Networking.Components;
using ONI_MP.Networking.Packets.World.Buildings;
using ONI_MP.Networking.States;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ONI_MP.Scripts.Buildings
{
	internal class ClientReceiver_Operational : KMonoBehaviour
	{
		[MyCmpGet] NetworkIdentity o;

		public override void OnSpawn()
		{
			base.OnSpawn();
			if (MultiplayerSession.IsClient)
				PacketSender.SendToHost(new RequestOperationalStatePacket(this));
		}

		public bool IsFunctional { get; set; }

		public bool IsOperational { get; set; }

		public bool IsActive { get; set; }
	}
}

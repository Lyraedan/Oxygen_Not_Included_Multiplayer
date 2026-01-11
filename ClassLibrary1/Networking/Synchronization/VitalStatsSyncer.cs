using Klei.AI;
using ONI_MP.Networking.Components;
using ONI_MP.Networking.Packets.DuplicantActions;
using UnityEngine;
using static STRINGS.DUPLICANTS.STATS;

namespace ONI_MP.Networking.Synchronization
{
	// Attached to minions on the Host side.
	// Periodically checks if vitals have changed significantly and sends updates.
	public class VitalStatsSyncer : KMonoBehaviour, ISim1000ms
	{
		[MyCmpGet]
		private NetworkIdentity _identity;
		[MyCmpGet]
		private PrimaryElement _element;
		private Amounts _amounts;

		public override void OnSpawn()
		{
			base.OnSpawn();
			_amounts = gameObject.GetAmounts();
		}

		public void Sim1000ms(float dt)
		{
			if (!MultiplayerSession.IsHost) return;
			if (_identity == null || _amounts == null) return;

			// Skip if no clients connected
			if (MultiplayerSession.ConnectedPlayers.Count == 0) return;

			foreach(var amountInstance in _amounts)
			{
				PacketSender.SendToAllClients(new VitalStatsPacket(_identity.NetId,_amounts, _element));
			}
		}
	}
}

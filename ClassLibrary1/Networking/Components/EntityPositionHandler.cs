using System;
using ONI_MP.DebugTools;
using UnityEngine;

namespace ONI_MP.Networking.Components
{
	public class EntityPositionHandler : KMonoBehaviour
	{
		private Vector3 lastSentPosition;
		private Vector3 previousPosition;
		private float timer;
		public static float SendIntervalMoving = 0.05f; // 50ms
        public static float SendIntervalStationary = 2.0f; // 2 seconds 

		[MyCmpGet] KBatchedAnimController kbac;

        private NetworkIdentity networkedEntity;
		private Navigator navigator;
		private bool facingLeft;
		private Vector3 velocity;

		public long lastPositionTimestamp = 0;

		public override void OnSpawn()
		{
			base.OnSpawn();

			networkedEntity = GetComponent<NetworkIdentity>();
			if (networkedEntity == null)
			{
				DebugConsole.LogWarning("[EntityPositionSender] Missing NetworkedEntityComponent. This component requires it to function.");
			}

			navigator = GetComponent<Navigator>();

			lastSentPosition = transform.position;
			previousPosition = transform.position;
			facingLeft = false;
		}

		private void Update()
		{
			if (networkedEntity == null)
				return;

			if (!MultiplayerSession.InSession || MultiplayerSession.IsClient)
				return;

			// Skip if no clients connected
			if (MultiplayerSession.ConnectedPlayers.Count == 0)
				return;

			SendPositionPacket();
		}

		private void SendPositionPacket()
		{
			try
			{
				timer += Time.unscaledDeltaTime;
                Vector3 currentPosition = transform.position;
				bool hasMoved = Vector3.Distance(currentPosition, lastSentPosition) > 0.01f;

                float SendInterval = hasMoved ? SendIntervalMoving : SendIntervalStationary;
				if (timer < SendInterval)
					return;

				float actualDeltaTime = timer;
				timer = 0f;

				// Calculate velocity
				velocity = (currentPosition - previousPosition) / actualDeltaTime;
				previousPosition = currentPosition;

				float deltaX = currentPosition.x - lastSentPosition.x;
				PrepAndSendMovementPacket(deltaX, currentPosition, SendInterval);

			}
			catch (System.Exception)
			{
				// Silently ignore - entity may not be ready yet
			}
		}

		private void PrepAndSendMovementPacket(float deltaX, Vector3 currentPosition, float sendInterval)
		{
            Vector2 direction = new Vector2(deltaX, 0f);
            if (direction.sqrMagnitude > 0.01f)
            {
                Vector2 right = Vector2.right;
                float dot = Vector2.Dot(direction.normalized, right);

                bool newFacingLeft = dot < 0;
                if (newFacingLeft != facingLeft)
                {
                    facingLeft = newFacingLeft;
                }
            }

            lastSentPosition = currentPosition;

            // Get current NavType from navigator if available
            NavType navType = NavType.Floor;
            if (navigator != null && navigator.CurrentNavType != NavType.NumNavTypes)
            {
                navType = navigator.CurrentNavType;
            }

            var packet = new EntityPositionPacket
            {
                NetId = networkedEntity.NetId,
                Position = currentPosition,
                Velocity = velocity,
                FacingLeft = kbac.FlipX,
                NavType = navType,
				SendInterval = sendInterval,
				Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
            };

            PacketSender.SendToAllClients(packet, sendType: SteamNetworkingSend.Unreliable);
        }
	}
}

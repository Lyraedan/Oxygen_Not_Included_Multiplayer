using System;
using System.Collections.Generic;
using System.Linq;
using ONI_MP.DebugTools;
using TMPro;
using UnityEngine;

namespace ONI_MP.Networking.Components
{
	public class EntityPositionHandler : KMonoBehaviour
	{
        public struct PositionSample
        {
            public Vector3 Position;
            public Vector3 Velocity;
            public bool FlipX;
            public bool FlipY;
            public long Timestamp;
        }

        [MyCmpGet] KBatchedAnimController kbac;
        [MyCmpGet] Navigator navigator;

        private Vector3 lastSentPosition;
		private Vector3 previousPosition;
		private float timer;
		public static float SendIntervalMoving = 0.05f; // 50ms
        public static float SendIntervalStationary = 2.0f; // 2 seconds 

		private Vector3 velocity;
        private Queue<PositionSample> positionQueue = new Queue<PositionSample>();

        #region Position Sync Tuning

        /// <summary>
        /// Distance at which we hard-snap instead of smoothing (world units)
        /// </summary>
        private const float SNAP_DISTANCE = 10f;

        /// <summary>
        /// How fast velocity converges to the corrected velocity
        /// Higher = more responsive, lower = smoother
        /// </summary>
        private const float VELOCITY_SMOOTHING = 10f;

        #endregion

        public override void OnSpawn()
		{
			base.OnSpawn();

			lastSentPosition = transform.position;
			previousPosition = transform.position;

			DebugConsole.Log($"[EntityPositionHandler] Spawned on {name}");
		}

		private void Update()
		{
			if (this.GetNetId() == 0)
				return;

			if (!MultiplayerSession.InSession)
				return;

			if (MultiplayerSession.IsClient)
			{
				UpdatePosition();
                return;
			}

			// Skip if no clients connected
			if (MultiplayerSession.ConnectedPlayers.Count == 0)
				return;

			SendPositionPacket();
		}

        private void UpdatePosition()
        {
            if (positionQueue.Count == 0)
                return;

            Vector3 currentPosition = transform.position;
            var nextSample = positionQueue.Peek();

            kbac.FlipX = nextSample.FlipX;
            kbac.FlipY = nextSample.FlipY;

            Vector3 targetPosition = nextSample.Position;
            float distanceToTarget = Vector3.Distance(currentPosition, targetPosition);

            if (distanceToTarget > SNAP_DISTANCE)
            {
                currentPosition = targetPosition;
                positionQueue.Dequeue();
            }
            else
            {
                float gameSpeed = SpeedControlScreen.Instance.GetSpeed() + 1f;
                float moveDistance = VELOCITY_SMOOTHING * Time.unscaledDeltaTime * gameSpeed;

                currentPosition = Vector3.MoveTowards(currentPosition, targetPosition, moveDistance);

                if (Vector3.Distance(currentPosition, targetPosition) < 0.01f)
                    positionQueue.Dequeue();
            }

            transform.SetPosition(currentPosition);
        }

        public void EnqueuePosition(PositionSample sample)
        {
            // Ignore outdated packets
            if (positionQueue.Count > 0 && sample.Timestamp <= positionQueue.Last().Timestamp)
                return;

            positionQueue.Enqueue(sample);
        }

        private void UpdatePositionOLd()
        {
            if (positionQueue.Count == 0)
                return;

            PositionSample sample = positionQueue.Last();
            Vector3 clientVelocity = sample.Velocity;

            float MAX_PREDICTION_TIME = 0.25f;
            float MIN_INTERPOLATION = 0.1f;
            float MAX_INTERPOLATION = 1f;
            float MIN_DT = 0.001f;
            if (sample.Timestamp == 0)
                return;

            kbac.FlipX = sample.FlipX;
            kbac.FlipY = sample.FlipY;

            float localTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            float packetTime = sample.Timestamp;

            float dt = Mathf.Clamp(localTime - packetTime, 0f, MAX_PREDICTION_TIME);

            Vector3 predictedPosition = sample.Position + sample.Velocity * dt;

            float error = Vector3.Distance(transform.position, predictedPosition);

            if (error > SNAP_DISTANCE)
            {
                transform.SetPosition(sample.Position);
                clientVelocity = sample.Velocity;
                return;
            }

            float interpolationFactor = Mathf.Clamp(Time.unscaledDeltaTime * VELOCITY_SMOOTHING, MIN_INTERPOLATION, MAX_INTERPOLATION);

            Vector3 correctedVelocity = (predictedPosition - transform.position) / Mathf.Max(dt, MIN_DT);

            clientVelocity = Vector3.Lerp(clientVelocity, correctedVelocity, interpolationFactor);

            transform.SetPosition(transform.position + clientVelocity * Time.unscaledDeltaTime);
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
                lastSentPosition = currentPosition;

                // Get current NavType from navigator if available
                NavType navType = NavType.Floor;
                if (navigator != null && navigator.CurrentNavType != NavType.NumNavTypes)
                {
                    navType = navigator.CurrentNavType;
                }

                var packet = new EntityPositionPacket
                {
                    NetId = this.GetNetId(),
                    Position = currentPosition,
                    Velocity = velocity,
                    FlipX = kbac.FlipX,
                    FlipY = kbac.FlipY,
                    NavType = navType,
                    SendInterval = SendInterval,
                    Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
                };

                PacketSender.SendToAllClients(packet, sendType: SteamNetworkingSend.Unreliable);

            }
			catch (System.Exception)
			{
				// Silently ignore - entity may not be ready yet
			}
		}
	}
}

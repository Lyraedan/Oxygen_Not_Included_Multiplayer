using System;
using ONI_MP.DebugTools;
using Shared.Profiling;
using TMPro;
using UnityEngine;

namespace ONI_MP.Networking.Components
{
	public class EntityPositionHandler : KMonoBehaviour
	{
        [MyCmpGet] KBatchedAnimController kbac;
        [MyCmpGet] Navigator navigator;

        private Vector3 lastSentPosition;
		private Vector3 previousPosition;
		private float timer;
		public static float SendIntervalMoving = 0.05f; // 50ms
        public static float SendIntervalStationary = 2.0f; // 2 seconds

		private Vector3 velocity;

        // Client position syncing, these are all updated from the EntityPositionPacket
        public Vector3 clientVelocity;
        public Vector3 serverPosition;
        public Vector3 serverVelocity;
        public long serverTimestamp;
        public bool serverFlipX;
        public bool serverFlipY;

        public static float SendIntervalDuplicantMoving = 0.5f;
        private bool? isDuplicantCached;

        // Only duplicants have this
        private DuplicantClientController duplicantController;
        private bool HasDuplicantController => duplicantController != null;

        #region Position Sync Tuning

        /// <summary>
        /// Maximum time (seconds) we allow for prediction forward
        /// Prevents extreme warping on lag spikes
        /// </summary>
        private const float MAX_PREDICTION_TIME = 0.25f;

        /// <summary>
        /// Distance at which we hard-snap instead of smoothing (world units)
        /// </summary>
        private const float SNAP_DISTANCE = 1.5f;

        /// <summary>
        /// How fast velocity converges to the corrected velocity
        /// Higher = more responsive, lower = smoother
        /// </summary>
        private const float VELOCITY_SMOOTHING = 10f;

        /// <summary>
        /// Minimum & maximum lerp factor per frame
        /// </summary>
        private const float MIN_INTERPOLATION = 0.05f;
        private const float MAX_INTERPOLATION = 0.25f;

        /// <summary>
        /// Prevent divide-by-zero and extreme velocity spikes
        /// </summary>
        private const float MIN_DT = 0.016f;

        #endregion

        public override void OnSpawn()
		{
			using var _ = Profiler.Scope();

			base.OnSpawn();

			lastSentPosition = transform.position;
			previousPosition = transform.position;

            duplicantController = GetComponent<DuplicantClientController>();
            DebugConsole.Log($"[EntityPositionHandler] Spawned on {name}");
		}

		private void Update()
		{
			using var _ = Profiler.Scope();

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

        // Ported this from my own Godot game, its not perfect. But it feels better
        private void UpdatePosition()
        {
	        using var _ = Profiler.Scope();

            if (serverTimestamp == 0)
                return;

            if (HasDuplicantController)
            {
                duplicantController.OnPositionCorrection(serverPosition);
                kbac.FlipX = serverFlipX;
                kbac.FlipY = serverFlipY;
                return;
            }

            kbac.FlipX = serverFlipX;
            kbac.FlipY = serverFlipY;

            float localTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            float packetTime = serverTimestamp;

            // Clamp prediction time
            float dt = Mathf.Clamp(localTime - packetTime, 0f, MAX_PREDICTION_TIME);

            // Predict forward using server velocity
            Vector3 predictedPosition = serverPosition + serverVelocity * dt;

            float error = Vector3.Distance(transform.position, predictedPosition);

            // Large desync -> snap
            if (error > SNAP_DISTANCE)
            {
                transform.SetPosition(serverPosition);
                clientVelocity = serverVelocity;
                return;
            }

            // Smooth correction
            float interpolationFactor = Mathf.Clamp(Time.unscaledDeltaTime * VELOCITY_SMOOTHING, MIN_INTERPOLATION, MAX_INTERPOLATION);

            Vector3 correctedVelocity = (predictedPosition - transform.position) / Mathf.Max(dt, MIN_DT);
            clientVelocity = Vector3.Lerp(clientVelocity, correctedVelocity, interpolationFactor);
            transform.SetPosition(transform.position + clientVelocity * Time.unscaledDeltaTime);
        }

        private void SendPositionPacket()
		{
			using var _ = Profiler.Scope();

			try
			{
				timer += Time.unscaledDeltaTime;
                Vector3 currentPosition = transform.position;
				bool hasMoved = Vector3.Distance(currentPosition, lastSentPosition) > 0.01f;

                if (!isDuplicantCached.HasValue)
                    isDuplicantCached = GetComponent<DuplicantStateSender>() != null;

                float SendInterval;
                if (isDuplicantCached.Value)
                {
                    SendInterval = hasMoved ? SendIntervalDuplicantMoving : SendIntervalStationary;
                }
                else
                {
                    SendInterval = hasMoved ? SendIntervalMoving : SendIntervalStationary;
                }
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

                PacketSender.SendToAllClients(packet, sendType: PacketSendMode.Unreliable);

            }
			catch (System.Exception)
			{
				// Silently ignore - entity may not be ready yet
			}
		}
	}
}

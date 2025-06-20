using System;
using UnityEngine;
using ONI_MP.Networking.Packets;
using ONI_MP.DebugTools;
using ONI_MP.Networking.Packets.Architecture;
using ONI_MP.Networking.Packets.Core;

namespace ONI_MP.Networking.Components
{
    public class EntityPositionHandler : KMonoBehaviour
    {
        private Vector3 lastSentPosition;
        private float timer;
        public static float SendInterval = 0.1f; // 100ms

        private NetworkIdentity networkedEntity;
        private bool facingLeft;

        private Vector3 lastVelocity;
        private float lastUpdateTime;
        private const float velocityThreshold = 0.05f;

        protected override void OnSpawn()
        {
            base.OnSpawn();

            networkedEntity = GetComponent<NetworkIdentity>();
            if (networkedEntity == null)
            {
                DebugConsole.LogWarning("[EntityPositionSender] Missing NetworkedEntityComponent. This component requires it to function.");
            }

            lastSentPosition = transform.position;
            facingLeft = false; // default facing direction
        }

        private void Update()
        {
            if (networkedEntity == null)
                return;

            if (!MultiplayerSession.InSession || MultiplayerSession.IsClient)
                return;

            SendPositionPacketRev2();
        }

        private void SendPositionPacketRev2()
        {
            timer += Time.unscaledDeltaTime;
            if (timer < SendInterval)
                return;

            timer = 0f;

            float currentTime = Time.unscaledTime;
            float deltaTime = Mathf.Max(currentTime - lastUpdateTime, 1e-6f);
            Vector3 currentPosition = transform.position;
            Vector3 velocity = (currentPosition - lastSentPosition) / deltaTime;

            bool shouldSendDeltaUpdate =
                Vector3.Distance(currentPosition, lastSentPosition) > 0.01f ||
                Vector3.Distance(velocity, lastVelocity) > velocityThreshold;

            if (!shouldSendDeltaUpdate)
                return;

            // Update last-known values before sending
            lastUpdateTime = currentTime;
            lastVelocity = velocity;
            lastSentPosition = currentPosition;

            // Determine facing direction based on horizontal movement
            if (Mathf.Abs(velocity.x) > 0.001f)
                facingLeft = velocity.x < 0;

            var packet = new EntityPositionPacket
            {
                NetId = networkedEntity.NetId,
                Position = currentPosition,
                Velocity = velocity,
                FacingLeft = facingLeft,
                Timestamp = currentTime
            };

            PacketSender.SendToAllClients(packet, sendType: SteamNetworkingSend.Unreliable);
        }



        // Test function to see how the game handles every interval. Conclusion. Not very well
        private void SendPositionPacketEveryInterval()
        {
            timer += Time.unscaledDeltaTime;
            if (timer < SendInterval)
                return;

            timer = 0f;

            Vector3 currentPosition = transform.position;
            float deltaX = currentPosition.x - lastSentPosition.x;

            if (Mathf.Abs(deltaX) > 0.001f)
            {
                facingLeft = deltaX < 0;
            }

            lastSentPosition = currentPosition;

            var packet = new EntityPositionPacket
            {
                NetId = networkedEntity.NetId,
                Position = currentPosition,
                FacingLeft = facingLeft
            };

            PacketSender.SendToAllClients(packet, sendType: SteamNetworkingSend.Unreliable);
        }

        // The old original SendPosition function. It was ok but had some jittiness to it
        private void SendPositionPacketRev1()
        {
            timer += Time.unscaledDeltaTime;
            if (timer < SendInterval)
                return;

            timer = 0f;

            Vector3 currentPosition = transform.position;
            float deltaX = currentPosition.x - lastSentPosition.x;

            if (Vector3.Distance(currentPosition, lastSentPosition) > 0.01f)
            {
                // Determine facing direction by horizontal movement
                if (Mathf.Abs(deltaX) > 0.001f)
                {
                    facingLeft = deltaX < 0;
                }

                lastSentPosition = currentPosition;

                var packet = new EntityPositionPacket
                {
                    NetId = networkedEntity.NetId,
                    Position = currentPosition,
                    FacingLeft = facingLeft
                };

                PacketSender.SendToAllClients(packet, sendType: SteamNetworkingSend.Unreliable);
            }
        }
    }
}

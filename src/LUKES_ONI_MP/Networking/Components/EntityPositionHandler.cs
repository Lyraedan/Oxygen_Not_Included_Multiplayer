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

            SendPositionPacket();
        }

        private void SendPositionPacket()
        {
            timer += Time.unscaledDeltaTime;
            if (timer < SendInterval)
                return;

            timer = 0f;

            Vector3 currentPosition = transform.position;
            float deltaX = currentPosition.x - lastSentPosition.x;

            if (Vector3.Distance(currentPosition, lastSentPosition) > 0.01f)
            {
                Vector2 direction = new Vector2(deltaX, 0f);
                if (direction.sqrMagnitude > 0.01f) // only check if there's meaningful movement
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

                var packet = new EntityPositionPacket
                {
                    NetId = networkedEntity.NetId,
                    Position = currentPosition,
                    FacingLeft = facingLeft
                };

                PacketSender.SendToAllClients(packet, sendType: SteamNetworkingSend.UnreliableNoNagle);
            }
        }
    }
}

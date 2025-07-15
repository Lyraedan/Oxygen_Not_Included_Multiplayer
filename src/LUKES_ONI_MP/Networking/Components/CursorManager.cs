using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ONI_MP.DebugTools;
using ONI_MP.Misc;
using ONI_MP.Networking.Packets.Architecture;
using ONI_MP.Networking.Packets.Core;
using ONI_MP.Networking.States;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ONI_MP.Networking.Components
{
    public class CursorManager : MonoBehaviour
    {
        public static CursorManager Instance { get; private set; }

        public static float SendInterval = 0.1f;

        private float timeSinceLastSend = 0f;

        public Color color;

        public CursorState cursorState = CursorState.NONE;

        private void Awake()
        {
            if (Instance != null)
            {
                Destroy(this);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void Start()
        {
            bool useRandom = Configuration.GetClientProperty<bool>("UseRandomPlayerColor");
            if(useRandom)
                color = UnityEngine.Random.ColorHSV(0f, 1f, 0.6f, 1f, 0.8f, 1f);
            else
            {
                ColorRGB color_rgb = Configuration.GetClientProperty<ColorRGB>("PlayerColor");
                color = color_rgb.ToColor();

            }
        }

        private void Update()
        {
            if (!Utils.IsInGame())
                return;

            if (!MultiplayerSession.InSession || !MultiplayerSession.LocalSteamID.IsValid())
                return;

            timeSinceLastSend += Time.unscaledDeltaTime;
            if (timeSinceLastSend >= SendInterval)
            {
                SendCursorPosition();
                timeSinceLastSend = 0f;
            }
        }

        private void SendCursorPosition()
        {
            Vector3 cursorWorldPos = GetCursorWorldPosition();
            var packet = new PlayerCursorPacket
            {
                SteamID = MultiplayerSession.LocalSteamID,
                Position = cursorWorldPos,
                Color = color,
                CursorState = cursorState
            };

            if(MultiplayerSession.IsHost)
            {
                PacketSender.SendToAllClients(packet, SteamNetworkingSend.Unreliable);
            } else
            {
                PacketSender.SendToHost(packet, SteamNetworkingSend.Unreliable);
            }
        }

        private Vector3 GetCursorWorldPosition()
        {
            var camera = GameScreenManager.Instance.GetCamera(GameScreenManager.UIRenderTarget.ScreenSpaceCamera);
            if (camera == null) return Vector3.zero;

            var canvas = GameScreenManager.Instance.ssCameraCanvas ?.GetComponent<Canvas>();
            var planeZ = canvas != null && canvas.renderMode == RenderMode.ScreenSpaceCamera ? canvas.planeDistance : 10f; // default fallback

            Vector3 screenPos = Input.mousePosition;
            screenPos.z = planeZ; // match the UI plane

            return camera.ScreenToWorldPoint(screenPos);
        }

    }
}

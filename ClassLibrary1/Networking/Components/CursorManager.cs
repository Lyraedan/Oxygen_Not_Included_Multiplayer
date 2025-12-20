using ONI_MP.Api.Networking;
using ONI_MP.Misc;
using ONI_MP.Networking.Packets.Core;
using ONI_MP.Networking.States;
using UnityEngine;

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
			AssignColor();
        }

		public void ResetColor()
		{
			color = Color.white;
		}

		public void AssignColor()
		{
            bool useRandom = Configuration.GetClientProperty<bool>("UseRandomPlayerColor");
            if (useRandom)
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

			// Calculate Viewport
			int minX = 0, minY = 0, maxX = 0, maxY = 0;
			if (Camera.main != null)
			{
				Camera cam = Camera.main;
				// Get corners
				Vector3 bl = cam.ViewportToWorldPoint(new Vector3(0, 0, 0));
				Vector3 tr = cam.ViewportToWorldPoint(new Vector3(1, 1, 0));

				minX = Grid.PosToCell(bl);
				maxX = Grid.PosToCell(tr);
				// Grid.PosToCell returns cell index, not XY.
				// We want XY coordinates to define a rectangle.

				Grid.PosToXY(bl, out int x1, out int y1);
				Grid.PosToXY(tr, out int x2, out int y2);

				minX = x1; minY = y1;
				maxX = x2; maxY = y2;
			}

			var packet = new PlayerCursorPacket
			{
				SteamID = MultiplayerSession.LocalSteamID,
				Position = cursorWorldPos,
				Color = color,
				CursorState = cursorState,
				ViewMinX = minX,
				ViewMinY = minY,
				ViewMaxX = maxX,
				ViewMaxY = maxY
			};

			if (MultiplayerSession.IsHost)
			{
				PacketSender.SendToAllClients(packet, SteamNetworkingSend.Unreliable);
			}
			else
			{
				PacketSender.SendToHost(packet, SteamNetworkingSend.Unreliable);
			}
		}

		private Vector3 GetCursorWorldPosition()
		{
			var camera = GameScreenManager.Instance.GetCamera(GameScreenManager.UIRenderTarget.ScreenSpaceCamera);
			if (camera == null) return Vector3.zero;

			var canvas = GameScreenManager.Instance.ssCameraCanvas?.GetComponent<Canvas>();
			var planeZ = canvas != null && canvas.renderMode == RenderMode.ScreenSpaceCamera ? canvas.planeDistance : 10f; // default fallback

			Vector3 screenPos = Input.mousePosition;
			screenPos.z = planeZ; // match the UI plane

			return camera.ScreenToWorldPoint(screenPos);
		}

	}
}

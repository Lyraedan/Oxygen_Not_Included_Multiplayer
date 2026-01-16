using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using ONI_MP.Misc;
using ONI_MP.Networking;
using ONI_MP.Networking.Relay.Steam;
using Steamworks;
using UnityEngine;
using UnityEngine.UI;
using SteamClient = ONI_MP.Networking.Relay.Steam.SteamClient;

namespace ONI_MP.Menus
{
    public class NetworkIndicatorsScreen
    {
        private static GameObject indicators;

        public static GameObject networkJitter_DEGRADED;
        public static GameObject networkJitter_BAD;

        public static GameObject latency_DEGRADED;
        public static GameObject latency_BAD;

        public static GameObject packetloss_DEGRADED;
        public static GameObject packetloss_BAD;

        public static GameObject serverPerformance_DEGRADED;
        public static GameObject serverPerformance_BAD;

        private const int JITTER_SAMPLE_COUNT = 20;
        private static readonly Queue<int> _pingSamples = new();

        public enum NetworkState
        {
            GOOD, DEGRADED, BAD
        }

        public static NetworkState jitterState;
        public static NetworkState latencyState;
        public static NetworkState packetlossState;
        public static NetworkState serverPerformanceState;

        private static TextStyleSetting tooltipStyle;

        public static void Show()
        {
            var parent = GameScreenManager.Instance.ssOverlayCanvas.transform;
            indicators = ResourceLoader.InstantiateGameObjectFromBundle("networkindicators", "assets/networkindicators/prefabs/network indicators.prefab",
                parent,
                Vector3.zero, // place at bottom center of screen
                Quaternion.identity,
                new Vector3(0.75f, 0.75f, 0.75f));
            var rect = indicators.GetComponent<RectTransform>();

            // Bottom-center anchor
            rect.anchorMin = new Vector2(0.5f, 0f);
            rect.anchorMax = new Vector2(0.5f, 0f);
            rect.pivot = new Vector2(0.5f, 0f);

            // Offset from bottom center
            rect.anchoredPosition = new Vector2(0f, 10f);

            networkJitter_DEGRADED = indicators.FindChild("High_Jitter/1");
            networkJitter_BAD = indicators.FindChild("High_Jitter/2");

            latency_DEGRADED = indicators.FindChild("High_Latency/1");
            latency_BAD = indicators.FindChild("High_Latency/2");

            packetloss_DEGRADED = indicators.FindChild("Packet_Loss/1");
            packetloss_BAD = indicators.FindChild("Packet_Loss/2");

            serverPerformance_DEGRADED = indicators.FindChild("Server_Performance/1");
            serverPerformance_BAD = indicators.FindChild("Server_Performance/2");

            FieldInfo styleField = typeof(SpeedControlScreen).GetField("TooltipTextStyle", BindingFlags.Instance | BindingFlags.NonPublic);
            var templateSetting = styleField?.GetValue(SpeedControlScreen.Instance) as TextStyleSetting;

            tooltipStyle = new TextStyleSetting();
            tooltipStyle.fontSize = templateSetting.fontSize;
            tooltipStyle.sdfFont = templateSetting.sdfFont;
            tooltipStyle.style = templateSetting.style;
            tooltipStyle.enableWordWrapping = true;
            tooltipStyle.textColor = Color.white;

            AddTooltipTo(networkJitter_DEGRADED, STRINGS.UI.MP_OVERLAY.CLIENT.NETWORKINDICATORS.DEGRADED_JITTER);
            AddTooltipTo(networkJitter_BAD, STRINGS.UI.MP_OVERLAY.CLIENT.NETWORKINDICATORS.BAD_JITTER);

            AddTooltipTo(latency_DEGRADED, STRINGS.UI.MP_OVERLAY.CLIENT.NETWORKINDICATORS.DEGRADED_LATENCY);
            AddTooltipTo(latency_BAD, STRINGS.UI.MP_OVERLAY.CLIENT.NETWORKINDICATORS.BAD_LATENCY);

            AddTooltipTo(packetloss_DEGRADED, STRINGS.UI.MP_OVERLAY.CLIENT.NETWORKINDICATORS.DEGRADED_PACKETLOSS);
            AddTooltipTo(packetloss_BAD, STRINGS.UI.MP_OVERLAY.CLIENT.NETWORKINDICATORS.BAD_PACKETLOSS);

            AddTooltipTo(serverPerformance_DEGRADED, STRINGS.UI.MP_OVERLAY.CLIENT.NETWORKINDICATORS.DEGRADED_SERVERPERFORMANCE);
            AddTooltipTo(serverPerformance_BAD, STRINGS.UI.MP_OVERLAY.CLIENT.NETWORKINDICATORS.BAD_SERVERPERFORMANCE);

            // Default to good, hiding the icons
            UpdateIndicatorIconState(NetworkState.GOOD, networkJitter_DEGRADED, networkJitter_BAD);
            UpdateIndicatorIconState(NetworkState.GOOD, latency_DEGRADED, latency_BAD);
            UpdateIndicatorIconState(NetworkState.GOOD, packetloss_DEGRADED, packetloss_BAD);
            UpdateIndicatorIconState(NetworkState.GOOD, serverPerformance_DEGRADED, serverPerformance_BAD);
        }

        private static void AddTooltipTo(GameObject go, string message)
        {
            var tooltip = go.AddOrGet<ToolTip>();
            tooltip.ClearMultiStringTooltip();
            tooltip.AddMultiStringTooltip(message, tooltipStyle);
        }

        public static void Update()
        {
            if (!MultiplayerSession.InSession)
                return;

            if (indicators == null)
                return;

            jitterState = GetJitterState();
            latencyState = GetLatencyState();
            packetlossState = GetPacketlossState();
            serverPerformanceState = GetServerPerformanceState();

            UpdateIndicatorIconState(jitterState, networkJitter_DEGRADED, networkJitter_BAD);
            UpdateIndicatorIconState(latencyState, latency_DEGRADED, latency_BAD);
            UpdateIndicatorIconState(packetlossState, packetloss_DEGRADED, packetloss_BAD);
            UpdateIndicatorIconState(serverPerformanceState, serverPerformance_DEGRADED, serverPerformance_BAD);
        }

        private static void UpdateIndicatorIconState(NetworkState state, GameObject okObject, GameObject badObject)
        {
            switch(state)
            {
                case NetworkState.GOOD:
                    okObject.SetActive(false);
                    badObject.SetActive(false);
                    break;
                case NetworkState.DEGRADED:
                    okObject.SetActive(true);
                    badObject.SetActive(false);
                    break;
                case NetworkState.BAD:
                    okObject.SetActive(false);
                    badObject.SetActive(true);
                    break;
                default:
                    okObject.SetActive(false);
                    badObject.SetActive(false);
                    break;
            }
        }

        public static NetworkState GetJitterState()
        {
            if (!MultiplayerSession.InSession)
                return NetworkState.GOOD;

            if (MultiplayerSession.IsHost)
                return NetworkState.GOOD;

            var connhealth = SteamClient.GetConnectionHealth();
            if (!connhealth.HasValue)
                return NetworkState.BAD;

            int ping = connhealth.Value.m_nPing;
            if (ping <= 0)
                return NetworkState.BAD;

            // Collect samples
            _pingSamples.Enqueue(ping);
            while (_pingSamples.Count > JITTER_SAMPLE_COUNT)
                _pingSamples.Dequeue();

            // Not enough data yet
            if (_pingSamples.Count < 5)
                return NetworkState.DEGRADED;

            // Calculate mean
            float mean = 0f;
            foreach (var p in _pingSamples)
                mean += p;
            mean /= _pingSamples.Count;

            // Calculate standard deviation
            float variance = 0f;
            foreach (var p in _pingSamples)
            {
                float diff = p - mean;
                variance += diff * diff;
            }

            float jitter = Mathf.Sqrt(variance / _pingSamples.Count);

            if (jitter <= 10f)
                return NetworkState.GOOD;

            if (jitter <= 30f)
                return NetworkState.DEGRADED;

            return NetworkState.BAD;
        }

        public static NetworkState GetLatencyState()
        {
            if (!MultiplayerSession.InSession)
                return NetworkState.GOOD;

            if (MultiplayerSession.IsHost)
                return NetworkState.GOOD;

            var connhealth = SteamClient.GetConnectionHealth();
            if (!connhealth.HasValue)
                return NetworkState.BAD;

            int ping = connhealth.Value.m_nPing;

            // Invalid or unknown ping
            if (ping <= 0)
                return NetworkState.BAD;

            if (ping <= 60)
                return NetworkState.GOOD;

            if (ping <= 120)
                return NetworkState.DEGRADED;

            return NetworkState.BAD;
        }

        public static NetworkState GetPacketlossState()
        {
            if (!MultiplayerSession.InSession)
                return NetworkState.GOOD;

            if (MultiplayerSession.IsHost)
                return NetworkState.GOOD;

            var connhealth = SteamClient.GetConnectionHealth();
            if (!connhealth.HasValue)
                return NetworkState.BAD;

            float quality = connhealth.Value.m_flConnectionQualityLocal;

            if (quality >= 0.95f)
                return NetworkState.GOOD;

            if (quality >= 0.85f)
                return NetworkState.DEGRADED;

            return NetworkState.BAD;
        }

        public static NetworkState GetServerPerformanceState()
        {
            if (!MultiplayerSession.InSession)
                return NetworkState.GOOD;

            if (MultiplayerSession.IsHost)
                return NetworkState.GOOD;

            if (!SteamClient.Connection.HasValue)
                return NetworkState.BAD;

            var connHealth = SteamClient.GetConnectionHealth();

            // Connection no longer exists
            if (!connHealth.HasValue)
                return NetworkState.BAD;

            // Authoritative disconnect states
            switch (connHealth.Value.m_eState)
            {
                case ESteamNetworkingConnectionState.k_ESteamNetworkingConnectionState_ClosedByPeer:
                case ESteamNetworkingConnectionState.k_ESteamNetworkingConnectionState_ProblemDetectedLocally:
                    return NetworkState.BAD;
            }

            // Hard transport failure
            if (connHealth.Value.m_cbSentUnackedReliable > 64 * 1024 || // 64kb unacked by server
                (long)connHealth.Value.m_usecQueueTime > 1_000_000 || // 1 second old unsent packets
                connHealth.Value.m_flConnectionQualityRemote <= 0.85f) // server side reports badly degraded quality
            {
                return NetworkState.BAD;
            }

            // Soft degradation
            if (connHealth.Value.m_cbSentUnackedReliable > 16 * 1024 || // 16kb unacked by server
                (long)connHealth.Value.m_usecQueueTime > 500_000 || // 500ms old unsent packets
                connHealth.Value.m_flConnectionQualityRemote <= 0.95f) // server side reports degraded quality
            {
                return NetworkState.DEGRADED;
            }

            return NetworkState.GOOD;
        }

    }
}

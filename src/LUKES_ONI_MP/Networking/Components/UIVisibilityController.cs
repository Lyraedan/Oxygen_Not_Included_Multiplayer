using System.Reflection;
using ONI_MP.Networking;
using UnityEngine;

namespace ONI_MP.Components
{
    public class UIVisibilityController : MonoBehaviour
    {

        private KToggle pauseButton;
        private ToolTip pauseTooltip;
        private TextStyleSetting tooltipTextStyle;

        private void Start()
        {
            // TODO LATER Plug the visibility tweaks to the lobby entered / left events on SteamLobby

        }

        private void Update()
        {
            //UpdatePauseButton();
        }

        void UpdatePauseButton()
        {
            if (SpeedControlScreen.Instance == null)
                return;

            if (pauseButton == null)
            {
                pauseButton = SpeedControlScreen.Instance?.pauseButtonWidget.GetComponent<KToggle>();
                pauseTooltip = SpeedControlScreen.Instance?.pauseButtonWidget.GetComponent<ToolTip>();

                FieldInfo styleField = typeof(SpeedControlScreen).GetField("TooltipTextStyle", BindingFlags.Instance | BindingFlags.NonPublic);
                tooltipTextStyle = styleField?.GetValue(SpeedControlScreen.Instance) as TextStyleSetting;
            }

            bool allowPause = !MultiplayerSession.InSession;

            pauseButton.interactable = allowPause;

            pauseTooltip.ClearMultiStringTooltip();

            if (MultiplayerSession.InSession)
            {
                // Show custom multiplayer-disabled tooltip
                pauseTooltip.AddMultiStringTooltip("<color=#F44A4A>Can't pause in Multiplayer</color>", tooltipTextStyle);
            }
            else
            {
                // Show default tooltip
                string tip = GameUtil.ReplaceHotkeyString("Pause <color=#F44A4A>[SPACE]</color>", Action.TogglePause);
                pauseTooltip.AddMultiStringTooltip(tip, tooltipTextStyle);
            }
        }
    }
}

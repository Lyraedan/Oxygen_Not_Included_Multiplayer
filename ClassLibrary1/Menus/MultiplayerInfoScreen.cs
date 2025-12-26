using System;
using ONI_MP.DebugTools;
using ONI_MP.Networking;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ONI_MP.Menus
{
    /// <summary>
    /// In-game multiplayer info screen with host controls.
    /// Shows lobby code, connected players, and action buttons.
    /// </summary>
    public class MultiplayerInfoScreen : MonoBehaviour
    {
        private static MultiplayerInfoScreen _instance;
        private static GameObject _screenGO;
        private static GameObject _panelGO;
        private Toggle _hardsyncToggle;
        private TextMeshProUGUI _hardSyncLabel;
        public static void Show(Transform parent)
        {
            if (_instance != null)
            {
                Close();
                return; // Toggle off
            }

            _panelGO = CreateScreen(parent);
            _instance = _panelGO.AddComponent<MultiplayerInfoScreen>();
            _instance.Initialize();
        }

        public static void Close()
        {
            if (_screenGO != null)
            {
                Destroy(_screenGO);
                _screenGO = null;
                _panelGO = null;
                _instance = null;
            }
        }

        private static GameObject CreateScreen(Transform parent)
        {
            // Find the root canvas for true fullscreen overlay
            Canvas rootCanvas = null;
            if (GameScreenManager.Instance != null)
            {
                var ssLayerCanvas = GameScreenManager.Instance.ssOverlayCanvas;
                if (ssLayerCanvas != null)
                    rootCanvas = ssLayerCanvas.GetComponent<Canvas>();
            }
            if (rootCanvas == null)
                rootCanvas = parent.GetComponentInParent<Canvas>();

            Transform overlayParent = rootCanvas != null ? rootCanvas.transform : parent;

            // Fullscreen overlay to block interaction with game
            GameObject overlay = new GameObject("MultiplayerInfoOverlay", typeof(RectTransform), typeof(Image), typeof(CanvasGroup));
            overlay.transform.SetParent(overlayParent, false);

            var overlayRT = overlay.GetComponent<RectTransform>();
            overlayRT.anchorMin = Vector2.zero;
            overlayRT.anchorMax = Vector2.one;
            overlayRT.sizeDelta = Vector2.zero;
            overlayRT.anchoredPosition = Vector2.zero;

            var overlayImage = overlay.GetComponent<Image>();
            overlayImage.color = new Color(0.02f, 0.02f, 0.05f, 0.85f);

            var overlayCanvasGroup = overlay.GetComponent<CanvasGroup>();
            overlayCanvasGroup.interactable = true;
            overlayCanvasGroup.blocksRaycasts = true;

            // Centered panel (fixed position at dead center)
            GameObject screen = new GameObject("MultiplayerInfoScreen", typeof(RectTransform), typeof(Image));
            screen.transform.SetParent(overlay.transform, false);

            var rt = screen.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = new Vector2(420, 360);

            var image = screen.GetComponent<Image>();
            image.color = new Color(0.1f, 0.1f, 0.15f, 1f);

            // Store overlay as the main GO to destroy
            _screenGO = overlay;

            return screen;
        }

        private static void CreateCloseXButton(Transform panelTransform)
        {
            var btnGO = new GameObject("CloseXButton", typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
            btnGO.transform.SetParent(panelTransform, false);

            // Ignore layout so it positions independently
            var layoutElem = btnGO.GetComponent<LayoutElement>();
            layoutElem.ignoreLayout = true;

            var rt = btnGO.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(1f, 1f);
            rt.anchorMax = new Vector2(1f, 1f);
            rt.pivot = new Vector2(1f, 1f);
            rt.anchoredPosition = new Vector2(-8, -8);
            rt.sizeDelta = new Vector2(28, 28);

            var image = btnGO.GetComponent<Image>();
            image.color = new Color(0.6f, 0.25f, 0.25f, 1f);

            var textGO = new GameObject("X", typeof(RectTransform), typeof(TextMeshProUGUI));
            textGO.transform.SetParent(btnGO.transform, false);
            var textRT = textGO.GetComponent<RectTransform>();
            textRT.anchorMin = Vector2.zero;
            textRT.anchorMax = Vector2.one;
            textRT.sizeDelta = Vector2.zero;

            var tmp = textGO.GetComponent<TextMeshProUGUI>();
            tmp.text = "X";
            tmp.fontSize = 16;
            tmp.fontStyle = FontStyles.Bold;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = Color.white;

            var button = btnGO.GetComponent<Button>();
            button.onClick.AddListener(Close);
        }

        private void Initialize()
        {
            // Main layout on the panel
            var layout = _panelGO.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(20, 20, 20, 20);
            layout.spacing = 15;
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.childControlHeight = false;
            layout.childControlWidth = true;

            // Title
            CreateLabel(MP_STRINGS.UI.SERVERBROWSER.MULTIPLAYER_SESSION_TITLE, 24, FontStyles.Bold, 35);

            // Divider
            CreateDivider();

            // Lobby Code section
            CreateLobbyCodeSection();

            // Connected players
            CreatePlayersSection();

            // Divider
            CreateDivider();

            // Action buttons
            CreateActionButtons();

            // X close button at top-right (created last for proper raycast order)
            CreateCloseXButton(_panelGO.transform);
        }

        private void CreateLobbyCodeSection()
        {
            var container = new GameObject("CodeSection", typeof(RectTransform), typeof(HorizontalLayoutGroup));
            container.transform.SetParent(_panelGO.transform, false);

            var containerRT = container.GetComponent<RectTransform>();
            containerRT.sizeDelta = new Vector2(0, 50);

            var layout = container.GetComponent<HorizontalLayoutGroup>();
            layout.spacing = 10;
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlWidth = false;

            // Label
            var labelGO = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
            labelGO.transform.SetParent(container.transform, false);
            var labelRT = labelGO.GetComponent<RectTransform>();
            labelRT.sizeDelta = new Vector2(100, 40);
            var labelTMP = labelGO.GetComponent<TextMeshProUGUI>();
            labelTMP.text = MP_STRINGS.UI.SERVERBROWSER.LOBBY_CODE;
            labelTMP.fontSize = 16;
            labelTMP.alignment = TextAlignmentOptions.MidlineRight;
            labelTMP.color = new Color(0.7f, 0.7f, 0.7f);

            // Code display
            var codeGO = new GameObject("Code", typeof(RectTransform), typeof(Image));
            codeGO.transform.SetParent(container.transform, false);
            var codeRT = codeGO.GetComponent<RectTransform>();
            codeRT.sizeDelta = new Vector2(180, 40);
            var codeImage = codeGO.GetComponent<Image>();
            codeImage.color = new Color(0.15f, 0.15f, 0.2f);

            var codeTextGO = new GameObject("CodeText", typeof(RectTransform), typeof(TextMeshProUGUI));
            codeTextGO.transform.SetParent(codeGO.transform, false);
            var codeTextRT = codeTextGO.GetComponent<RectTransform>();
            codeTextRT.anchorMin = Vector2.zero;
            codeTextRT.anchorMax = Vector2.one;
            codeTextRT.sizeDelta = Vector2.zero;
            var codeTMP = codeTextGO.GetComponent<TextMeshProUGUI>();
            string formattedCode = LobbyCodeHelper.FormatCodeForDisplay(SteamLobby.CurrentLobbyCode);
            codeTMP.text = formattedCode;
            codeTMP.fontSize = 20;
            codeTMP.alignment = TextAlignmentOptions.Center;
            codeTMP.color = new Color(0.5f, 0.9f, 1f);
            codeTMP.fontStyle = FontStyles.Bold;

            // Copy button
            CreateCopyButton(container.transform, SteamLobby.CurrentLobbyCode);
        }

        private void CreateCopyButton(Transform parent, string textToCopy)
        {
            var btnGO = new GameObject("CopyButton", typeof(RectTransform), typeof(Image), typeof(Button));
            btnGO.transform.SetParent(parent, false);

            var rt = btnGO.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(70, 35);

            var image = btnGO.GetComponent<Image>();
            image.color = new Color(0.3f, 0.5f, 0.7f);

            var textGO = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
            textGO.transform.SetParent(btnGO.transform, false);
            var textRT = textGO.GetComponent<RectTransform>();
            textRT.anchorMin = Vector2.zero;
            textRT.anchorMax = Vector2.one;
            textRT.sizeDelta = Vector2.zero;
            var textTMP = textGO.GetComponent<TextMeshProUGUI>();
            textTMP.text = MP_STRINGS.UI.SERVERBROWSER.COPY;
            textTMP.fontSize = 14;
            textTMP.alignment = TextAlignmentOptions.Center;
            textTMP.color = Color.white;

            var button = btnGO.GetComponent<Button>();
            button.onClick.AddListener(() =>
            {
                GUIUtility.systemCopyBuffer = textToCopy;
                textTMP.text = MP_STRINGS.UI.SERVERBROWSER.COPIED;
                // Reset after delay
                StartCoroutine(ResetCopyButtonText(textTMP));
            });
        }

        private System.Collections.IEnumerator ResetCopyButtonText(TextMeshProUGUI tmp)
        {
            yield return new WaitForSeconds(1.5f);
            if (tmp != null)
                tmp.text = MP_STRINGS.UI.SERVERBROWSER.COPY;
        }

        private void CreatePlayersSection()
        {
            var container = new GameObject("PlayersSection", typeof(RectTransform));
            container.transform.SetParent(_panelGO.transform, false);
            var containerRT = container.GetComponent<RectTransform>();
            containerRT.sizeDelta = new Vector2(0, 30);

            var textGO = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
            textGO.transform.SetParent(container.transform, false);
            var textRT = textGO.GetComponent<RectTransform>();
            textRT.anchorMin = Vector2.zero;
            textRT.anchorMax = Vector2.one;
            textRT.sizeDelta = Vector2.zero;

            var tmp = textGO.GetComponent<TextMeshProUGUI>();
            int playerCount = MultiplayerSession.ConnectedPlayers.Count + 1; // +1 for host
            tmp.text = string.Format(MP_STRINGS.UI.SERVERBROWSER.CONNECTED_PLAYERS, playerCount);
            tmp.fontSize = 16;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = new Color(0.8f, 0.8f, 0.8f);
        }

        private void CreateActionButtons()
        {
            var container = new GameObject("ActionButtons", typeof(RectTransform), typeof(VerticalLayoutGroup));
            container.transform.SetParent(_panelGO.transform, false);

            var containerRT = container.GetComponent<RectTransform>();
            containerRT.sizeDelta = new Vector2(0, 150);

            var layout = container.GetComponent<VerticalLayoutGroup>();
            layout.spacing = 10;
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.childControlWidth = false;
            layout.childControlHeight = false;

            if (MultiplayerSession.IsHost)
            {
                CreateHardSyncToggle(container.transform, MP_STRINGS.UI.PAUSESCREEN.HARDSYNC.LABEL);

                // Invite button
                CreateActionButton(container.transform, MP_STRINGS.UI.PAUSESCREEN.INVITE.LABEL, () =>
                {
                    Steamworks.SteamFriends.ActivateGameOverlayInviteDialog(SteamLobby.CurrentLobby);
                });

                // Hard Sync button
                if (!GameServerHardSync.hardSyncDoneThisCycle)
                {
                    CreateActionButton(container.transform, MP_STRINGS.UI.PAUSESCREEN.DOHARDSYNC.LABEL, () =>
                    {
                        Close();
                        if (MultiplayerSession.ConnectedPlayers.Count > 0)
                        {
                            GameServerHardSync.PerformHardSync();
                        }
                        else
                        {
                            GameServerHardSync.hardSyncDoneThisCycle = true;
                        }
                    });
                }
                else
                {
                    CreateActionButton(container.transform, MP_STRINGS.UI.PAUSESCREEN.HARDSYNCNOTAVAILABLE.LABEL, null, true);
                }

                // End Session button
                CreateActionButton(container.transform, MP_STRINGS.UI.PAUSESCREEN.ENDSESSION.LABEL, () =>
                {
                    Close();
                    SteamLobby.LeaveLobby();
                    SpeedControlScreen.Instance?.Unpause(false);
                }, false, new Color(0.7f, 0.3f, 0.3f));
            }
            else
            {
                // Leave Session button for clients
                CreateActionButton(container.transform, MP_STRINGS.UI.PAUSESCREEN.LEAVESESSION.LABEL, () =>
                {
                    Close();
                    SteamLobby.LeaveLobby();
                }, false, new Color(0.7f, 0.3f, 0.3f));
            }
        }

        private void CreateHardSyncToggle(Transform parent, string label)
        {
            var container = new GameObject("HardSyncOption", typeof(RectTransform), typeof(HorizontalLayoutGroup));
            container.transform.SetParent(parent, false);

            var layout = container.GetComponent<HorizontalLayoutGroup>();
            layout.spacing = 15;
            layout.childAlignment = TextAnchor.MiddleLeft;
            layout.childControlWidth = false;
            layout.childForceExpandWidth = false;

            var rt = container.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(0, 40);

            // Label
            var labelGO = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
            labelGO.transform.SetParent(container.transform, false);
            var labelRT = labelGO.GetComponent<RectTransform>();
            labelRT.sizeDelta = new Vector2(200, 40);

            var labelTmp = labelGO.GetComponent<TextMeshProUGUI>();
            labelTmp.text = label;
            labelTmp.fontSize = 16;
            labelTmp.alignment = TextAlignmentOptions.MidlineLeft;
            labelTmp.color = Color.white;

            // Toggle container
            var toggleContainer = new GameObject("ToggleContainer", typeof(RectTransform), typeof(HorizontalLayoutGroup));
            toggleContainer.transform.SetParent(container.transform, false);
            var toggleLayout = toggleContainer.GetComponent<HorizontalLayoutGroup>();
            toggleLayout.spacing = 10;
            toggleLayout.childAlignment = TextAnchor.MiddleLeft;
            toggleLayout.childControlWidth = false;

            var toggleContainerRT = toggleContainer.GetComponent<RectTransform>();
            toggleContainerRT.sizeDelta = new Vector2(180, 40);

            // Create toggle background
            var toggleBG = new GameObject("ToggleBG", typeof(RectTransform), typeof(Image), typeof(Toggle));
            toggleBG.transform.SetParent(toggleContainer.transform, false);
            var toggleBGRT = toggleBG.GetComponent<RectTransform>();
            toggleBGRT.sizeDelta = new Vector2(50, 30);

            var bgImage = toggleBG.GetComponent<Image>();
            bgImage.color = new Color(0.2f, 0.2f, 0.25f);

            // Create checkmark
            var checkmark = new GameObject("Checkmark", typeof(RectTransform), typeof(Image));
            checkmark.transform.SetParent(toggleBG.transform, false);
            var checkRT = checkmark.GetComponent<RectTransform>();
            checkRT.anchorMin = Vector2.zero;
            checkRT.anchorMax = Vector2.one;
            checkRT.offsetMin = new Vector2(4, 4);
            checkRT.offsetMax = new Vector2(-4, -4);

            var checkImage = checkmark.GetComponent<Image>();
            checkImage.color = new Color(0.4f, 0.7f, 1f);

            _hardsyncToggle = toggleBG.GetComponent<Toggle>();
            _hardsyncToggle.graphic = checkImage;
            _hardsyncToggle.isOn = Configuration.Instance.Host.HardSyncAfterCycleComplete;
            _hardsyncToggle.onValueChanged.AddListener(OnHardSyncToggleChanged);

            // Status label
            var statusGO = new GameObject("StatusLabel", typeof(RectTransform), typeof(TextMeshProUGUI));
            statusGO.transform.SetParent(toggleContainer.transform, false);
            var statusRT = statusGO.GetComponent<RectTransform>();
            statusRT.sizeDelta = new Vector2(100, 30);

            _hardSyncLabel = statusGO.GetComponent<TextMeshProUGUI>();
            _hardSyncLabel.fontSize = 16;
            _hardSyncLabel.alignment = TextAlignmentOptions.Left;
            UpdateHardSyncLabel();
        }

        private void CreateActionButton(Transform parent, string text, System.Action onClick, bool disabled = false, Color? bgColor = null)
        {
            var btnGO = new GameObject($"Btn_{text}", typeof(RectTransform), typeof(Image), typeof(Button));
            btnGO.transform.SetParent(parent, false);

            var rt = btnGO.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(280, 38);

            var image = btnGO.GetComponent<Image>();
            image.color = bgColor ?? (disabled ? new Color(0.2f, 0.2f, 0.25f) : new Color(0.25f, 0.4f, 0.55f));

            var textGO = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
            textGO.transform.SetParent(btnGO.transform, false);
            var textRT = textGO.GetComponent<RectTransform>();
            textRT.anchorMin = Vector2.zero;
            textRT.anchorMax = Vector2.one;
            textRT.sizeDelta = Vector2.zero;

            var tmp = textGO.GetComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = 16;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = disabled ? new Color(0.5f, 0.5f, 0.5f) : Color.white;

            var button = btnGO.GetComponent<Button>();
            button.interactable = !disabled && onClick != null;
            if (onClick != null)
                button.onClick.AddListener(() => onClick());
        }

        private void CreateLabel(string text, int fontSize, FontStyles style, float height)
        {
            var labelGO = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
            labelGO.transform.SetParent(_panelGO.transform, false);

            var rt = labelGO.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(0, height);

            var tmp = labelGO.GetComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.fontStyle = style;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = Color.white;
        }

        private void CreateDivider()
        {
            var dividerGO = new GameObject("Divider", typeof(RectTransform), typeof(Image));
            dividerGO.transform.SetParent(_panelGO.transform, false);

            var rt = dividerGO.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(0, 2);

            var image = dividerGO.GetComponent<Image>();
            image.color = new Color(0.3f, 0.3f, 0.4f, 0.5f);
        }

        public void OnHardSyncToggleChanged(bool DoHardSync)
        {
            UpdateHardSyncLabel();
            Configuration.SetHostProperty<bool>("HardSyncAfterCycleComplete", DoHardSync);
        }

        private void UpdateHardSyncLabel()
        {
            if (_hardsyncToggle.isOn)
            {
                _hardSyncLabel.text = MP_STRINGS.UI.HOSTLOBBYCONFIGSCREEN.HARD_SYNC_ENABLED;
                _hardSyncLabel.color = new Color(0.0f, 1.0f, 0.0f);
            }
            else
            {
                _hardSyncLabel.text = MP_STRINGS.UI.HOSTLOBBYCONFIGSCREEN.HARD_SYNC_DISABLED;
                _hardSyncLabel.color = new Color(1.0f, 0.0f, 0.0f);
            }
        }
    }
}

using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using UnityEngine.Diagnostics;
using ONI_MP.DebugTools;
using ONI_MP.Networking;
using ONI_MP.Networking.Packets;
using ONI_MP.Networking.Components;
using ONI_MP.Misc;
using Utils = ONI_MP.Misc.Utils;
using Steamworks;
using ONI_MP.Networking.Packets.Architecture;
using ONI_MP.Networking.Packets.Social;

namespace ONI_MP.UI
{
    public class ChatScreen : KScreen
    {
        public TMP_InputField inputField;
        private RectTransform messageContainer;
        private List<TextMeshProUGUI> messages = new List<TextMeshProUGUI>();
        private RectTransform panelRectTransform;

        public static ChatScreen Instance;

        private GameObject header;
        private GameObject chatbox;
        private bool expanded = false;

        private static List<string> pendingMessages = new List<string>();

        public static void Show()
        {
            if (Instance != null)
                return;

            var go = new GameObject("ChatScreen", typeof(RectTransform));
            Instance = go.AddComponent<ChatScreen>();
            var parent = GameScreenManager.Instance.ssOverlayCanvas.transform;
            go.transform.SetParent(parent, false);

            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = new Vector2(0, 0);

            Instance.SetupUI();
        }
        private void SetupUI()
        {
            var chatWindowRoot = new GameObject("ChatWindowRoot", typeof(RectTransform));
            chatWindowRoot.transform.SetParent(transform, false);
            var rootRT = chatWindowRoot.GetComponent<RectTransform>();
            rootRT.anchorMin = new Vector2(0.5f, 0);
            rootRT.anchorMax = new Vector2(0.5f, 0);
            rootRT.pivot = new Vector2(0.5f, 0);
            rootRT.anchoredPosition = new Vector2(0, 250);
            rootRT.sizeDelta = new Vector2(400, 230);

            chatWindowRoot.AddComponent<UIDragHandler>();

            var chatboxContents = new GameObject("Chatbox_Contents");
            chatboxContents.transform.SetParent(chatWindowRoot.transform, false);
            chatbox = chatboxContents;

            var contentsRT = chatboxContents.AddComponent<RectTransform>();
            contentsRT.anchorMin = new Vector2(0, 0);
            contentsRT.anchorMax = new Vector2(1, 1);
            contentsRT.pivot = new Vector2(0.5f, 0);
            contentsRT.offsetMin = new Vector2(0, 0);
            contentsRT.offsetMax = new Vector2(0, -30);

            var panel = CreatePanel("ChatPanel", chatboxContents.transform, new Vector2(400, 200));
            panel.GetComponent<Image>().color = new Color(0, 0, 0, 0.7f);
            var panelRT = panel.GetComponent<RectTransform>();
            panelRectTransform = panelRT;

            var scroll = CreateScrollArea("Scroll", panel.transform, out messageContainer);
            var scrollRT = scroll.GetComponent<RectTransform>();
            scrollRT.anchorMin = new Vector2(0, 0);
            scrollRT.anchorMax = new Vector2(1, 1);
            scrollRT.pivot = new Vector2(0.5f, 1f);
            scrollRT.offsetMin = new Vector2(10, 50);
            scrollRT.offsetMax = new Vector2(-10, -10);

            messageContainer.anchorMin = new Vector2(0, 0);
            messageContainer.anchorMax = new Vector2(1, 1);
            messageContainer.offsetMin = Vector2.zero;
            messageContainer.offsetMax = Vector2.zero;
            messageContainer.pivot = new Vector2(0.5f, 1f);

            inputField = CreateInputField("ChatInput", panel.transform, new Vector2(10, 15), new Vector2(380, 30));
            inputField.onEndEdit.AddListener(OnInputSubmitted);

            header = new GameObject("ChatHeader", typeof(RectTransform), typeof(Image), typeof(Button));
            header.transform.SetParent(chatWindowRoot.transform, false);
            var headerRT = header.GetComponent<RectTransform>();
            headerRT.anchorMin = new Vector2(0, 1);
            headerRT.anchorMax = new Vector2(1, 1);
            headerRT.pivot = new Vector2(0.5f, 1);
            headerRT.anchoredPosition = new Vector2(0, 20);
            headerRT.sizeDelta = new Vector2(0, 30);
            header.GetComponent<Image>().color = new Color(0.4f, 0.2f, 0.3f, 0.9f);

            var headerTextGO = new GameObject("HeaderText", typeof(TextMeshProUGUI));
            headerTextGO.transform.SetParent(header.transform, false);
            var headerText = headerTextGO.GetComponent<TextMeshProUGUI>();
            headerText.text = "Chat -";
            headerText.alignment = TextAlignmentOptions.MidlineLeft;
            headerText.fontSize = 20;
            headerText.color = Color.white;
            headerText.margin = new Vector4(10, 0, 0, 0);

            var headerTextRT = headerText.GetComponent<RectTransform>();
            headerTextRT.anchorMin = new Vector2(0, 0);
            headerTextRT.anchorMax = new Vector2(1, 1);
            headerTextRT.offsetMin = new Vector2(5, 0);
            headerTextRT.offsetMax = new Vector2(-5, 0);


            expanded = true;
            header.GetComponent<Button>().onClick.AddListener(() =>
            {
                expanded = !expanded;
                chatbox.SetActive(expanded);
                headerText.text = expanded ? "Chat -" : "Chat +";
            });

            header.transform.SetAsLastSibling();

            QueueMessage("<color=yellow>System:</color> Chat initialized.");
            ProcessMessageQueue();

            StartCoroutine(FixInputFieldDisplay());
        }

        public void ProcessMessageQueue()
        {
            foreach (var msg in pendingMessages)
                QueueMessage(msg);
            pendingMessages.Clear();
        }

        public static void QueueMessage(string msg)
        {
            if(string.IsNullOrEmpty(msg))
            {
                return;
            }

            if (Instance != null)
                Instance.AddMessage(msg);
            else
                pendingMessages.Add(msg);
        }

        private System.Collections.IEnumerator FixInputFieldDisplay()
        {
            // Fixes the Caret and the text highlighting because without this they don't show
            yield return new WaitForEndOfFrame();
            inputField.gameObject.SetActive(false);
            yield return new WaitForEndOfFrame();
            inputField.gameObject.SetActive(true);
        }

        public void AddMessage(string text)
        {
            if (messageContainer == null)
            {
                DebugConsole.LogWarning("[Chatbox] Tried to add a message but messageContainer was null!");
                return;
            }

            // Create a new message object
            var go = new GameObject("Message", typeof(RectTransform), typeof(TextMeshProUGUI));
            go.transform.SetParent(messageContainer, false);

            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0, 1);
            rt.anchorMax = new Vector2(1, 1);
            rt.pivot = new Vector2(0, 1);
            rt.offsetMin = new Vector2(0, 0);
            rt.offsetMax = new Vector2(0, 0);

            var tmp = go.GetComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.font = Utils.GetDefaultTMPFont();
            tmp.fontSize = 18;
            tmp.enableWordWrapping = true;
            tmp.richText = true;
            tmp.alignment = TextAlignmentOptions.TopLeft;
            tmp.color = new Color(0.9f, 0.9f, 0.9f, 1f);
            tmp.margin = new Vector4(6, 2, 6, 2);

            // Fit content height
            var fitter = go.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;

            messages.Add(tmp);

            // Manually rebuild layout and force scroll to bottom
            LayoutRebuilder.ForceRebuildLayoutImmediate(messageContainer);
            var scrollRect = messageContainer.GetComponentInParent<ScrollRect>();
            if (scrollRect != null)
                scrollRect.verticalNormalizedPosition = 0f;
        }
        public static bool IsFocused()
        {
            return Instance != null && Instance.inputField != null && Instance.inputField.isFocused;
        }

        private void Update()
        {
            header.SetActive(MultiplayerSession.InSession);
            chatbox.SetActive(MultiplayerSession.InSession && expanded);
            if (!MultiplayerSession.InSession)
            {
                return;
            }

            // ENTER while NOT focused: activate input
            else if (!inputField.isFocused && Input.GetKeyDown(KeyCode.Return))
            {
                inputField.ActivateInputField();
            }

            // ESC while focused: cancel typing
            else if (inputField.isFocused && Input.GetKeyDown(KeyCode.Escape))
            {
                inputField.DeactivateInputField();
            }
        }

        public GameObject CreatePanel(string name, Transform parent, Vector2 size)
        {
            var panel = new GameObject(name, typeof(Image));
            panel.transform.SetParent(parent, false);

            var rt = panel.GetComponent<RectTransform>();
            rt.sizeDelta = size;
            rt.anchorMin = new Vector2(0.5f, 0); // Bottom center of screen
            rt.anchorMax = new Vector2(0.5f, 0);
            rt.pivot = new Vector2(0.5f, 0);
            rt.anchoredPosition = new Vector2(0, 20); // 20 pixels above bottom edge

            return panel;
        }

        public TMP_InputField CreateInputField(string name, Transform parent, Vector2 position, Vector2 size)
        {
            // Container with background
            var go = new GameObject(name, typeof(Image), typeof(TMP_InputField));
            go.transform.SetParent(parent, false);

            var rt = go.GetComponent<RectTransform>();
            rt.sizeDelta = size;
            rt.anchorMin = new Vector2(0, 0); // bottom left
            rt.anchorMax = new Vector2(0, 0);
            rt.pivot = new Vector2(0, 0);
            rt.anchoredPosition = position;

            var bg = go.GetComponent<Image>();
            bg.color = new Color(0.2f, 0.2f, 0.2f, 1f); // Background color

            var input = go.GetComponent<TMP_InputField>();
            input.lineType = TMP_InputField.LineType.SingleLine;
            input.scrollSensitivity = 0f;

            // Viewport
            var viewportGO = new GameObject("TextViewport", typeof(RectMask2D), typeof(Image));
            viewportGO.transform.SetParent(go.transform, false);
            var viewportRT = viewportGO.GetComponent<RectTransform>();
            viewportRT.anchorMin = Vector2.zero;
            viewportRT.anchorMax = Vector2.one;
            viewportRT.offsetMin = new Vector2(5, 4);
            viewportRT.offsetMax = new Vector2(-5, -4);
            viewportGO.GetComponent<Image>().color = Color.clear;

            // Text component
            var textGO = new GameObject("Text", typeof(TextMeshProUGUI));
            textGO.transform.SetParent(viewportGO.transform, false);
            var tmp = textGO.GetComponent<TextMeshProUGUI>();
            tmp.fontSize = 18;
            tmp.enableWordWrapping = false;
            tmp.alignment = TextAlignmentOptions.MidlineLeft;
            tmp.color = Color.white;

            var textRT = tmp.GetComponent<RectTransform>();
            textRT.anchorMin = Vector2.zero;
            textRT.anchorMax = Vector2.one;
            textRT.offsetMin = Vector2.zero;
            textRT.offsetMax = Vector2.zero;

            // Highlight + Caret visuals
            input.textViewport = viewportRT;
            input.textComponent = tmp;
            input.placeholder = null;
            input.targetGraphic = bg;
            input.caretColor = Color.white;
            input.selectionColor = new Color(0.3f, 0.6f, 1f, 0.5f); // Light blue selection
            input.customCaretColor = true;

            return input;
        }

        public GameObject CreateScrollArea(string name, Transform parent, out RectTransform content)
        {
            var scrollGO = new GameObject(name, typeof(RectTransform), typeof(ScrollRect));
            scrollGO.transform.SetParent(parent, false);
            var scrollRT = scrollGO.GetComponent<RectTransform>();

            scrollRT.anchorMin = new Vector2(0, 0);
            scrollRT.anchorMax = new Vector2(1, 1);
            scrollRT.pivot = new Vector2(0.5f, 1);
            scrollRT.offsetMin = new Vector2(10, 50);
            scrollRT.offsetMax = new Vector2(-10, -10);

            // Viewport
            var viewport = new GameObject("Viewport", typeof(RectMask2D), typeof(Image));
            viewport.transform.SetParent(scrollGO.transform, false);
            var viewportRT = viewport.GetComponent<RectTransform>();
            viewportRT.anchorMin = Vector2.zero;
            viewportRT.anchorMax = Vector2.one;
            viewportRT.offsetMin = Vector2.zero;
            viewportRT.offsetMax = Vector2.zero;
            viewport.GetComponent<Image>().color = Color.clear;

            // Content container
            var contentGO = new GameObject("Content", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
            contentGO.transform.SetParent(viewport.transform, false);
            content = contentGO.GetComponent<RectTransform>();

            var layout = contentGO.GetComponent<VerticalLayoutGroup>();
            layout.childAlignment = TextAnchor.UpperLeft;
            layout.childControlHeight = true;
            layout.childForceExpandHeight = false;
            layout.childControlWidth = true;
            layout.childForceExpandWidth = true;
            layout.spacing = 5;

            var fitter = contentGO.GetComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;

            var scroll = scrollGO.GetComponent<ScrollRect>();
            scroll.content = content;
            scroll.viewport = viewportRT;
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 50f;

            // Add layout group and fitter to viewport (optional but can help)
            viewport.AddComponent<CanvasRenderer>();

            return scrollGO;
        }

        public static bool IsMouseOverChatPanel()
        {
            if (Instance == null || Instance.panelRectTransform == null)
                return false;

            Vector2 mousePos = Input.mousePosition;
            return RectTransformUtility.RectangleContainsScreenPoint(Instance.panelRectTransform, mousePos, Camera.main);
        }

        private void OnInputSubmitted(string text)
        {
            if (!Input.GetKeyDown(KeyCode.Return)) return;

            if (!string.IsNullOrWhiteSpace(text))
            {
                string senderName = SteamFriends.GetPersonaName();

                string colorHex = ColorUtility.ToHtmlStringRGB(CursorManager.Instance.color);
                QueueMessage($"<color=#{colorHex}>{senderName}:</color> {text}");
                inputField.text = "";

                var packet = new ChatMessagePacket
                {
                    SenderId = MultiplayerSession.LocalSteamID,
                    Message = text,
                    PlayerColor = CursorManager.Instance.color
                };

                if (!MultiplayerSession.IsHost)
                {
                    PacketSender.SendToHost(packet);
                } else
                {
                    PacketSender.SendToAllClients(packet);
                }
            }

            inputField.DeactivateInputField();
        }

    }
}

using ONI_MP.DebugTools;
using ONI_MP.SharedStorage;
using ONI_MP.Networking.Components;
using UnityEngine;
using UnityEngine.UI;

namespace ONI_MP.Menus
{
    /// <summary>
    /// Dialog for configuring storage server settings when hosting a game
    /// </summary>
    public static class StorageServerDialog
    {
        private static GameObject _currentDialog;
        private static System.Action<string> _onServerSelected;

        /// <summary>
        /// Shows the storage server configuration dialog
        /// </summary>
        public static void Show(Transform parent, System.Action<string> onServerSelected)
        {
            if (_currentDialog != null)
            {
                DebugConsole.Log("[StorageServerDialog] Dialog already open.");
                return;
            }

            _onServerSelected = onServerSelected;

            // Create dialog container
            GameObject dialog = new GameObject("StorageServerDialog", typeof(RectTransform), typeof(CanvasGroup), typeof(Image));
            _currentDialog = dialog;

            dialog.transform.SetParent(parent, worldPositionStays: false);

            var rt = dialog.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(400, 300);
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = Vector2.zero;

            var canvasGroup = dialog.GetComponent<CanvasGroup>();
            canvasGroup.interactable = true;
            canvasGroup.blocksRaycasts = true;

            var background = dialog.GetComponent<Image>();
            background.color = new Color(0.1f, 0.1f, 0.1f, 0.9f);

            // Title
            AddDialogLabel(dialog.transform, "Storage Server Setup", new Vector2(0, 100), 16);
            
            // Local server section
            AddDialogLabel(dialog.transform, "Embedded Server (Recommended)", new Vector2(0, 60), 12);
            
            // Start local server button
            AddDialogButton(dialog.transform, "Start Embedded Server", new Vector2(0, 30), () =>
            {
                StartLocalServer();
            });

            // Custom server section
            AddDialogLabel(dialog.transform, "Custom Server", new Vector2(0, -20), 12);
            
            // Server URL input
            var inputField = CreateServerUrlInput(dialog.transform, new Vector2(0, -50));
            
            // Connect to custom server button
            AddDialogButton(dialog.transform, "Use Custom Server", new Vector2(0, -80), () =>
            {
                string url = inputField.text.Trim();
                if (string.IsNullOrEmpty(url))
                {
                    url = "http://localhost:3000";
                }
                UseCustomServer(url);
            });

            // Cancel button
            AddDialogButton(dialog.transform, "Cancel", new Vector2(0, -130), () =>
            {
                Close();
            });
        }

        private static void StartLocalServer()
        {
            DebugConsole.Log("[StorageServerDialog] Starting embedded server...");
            
            // Disable dialog interaction while starting
            if (_currentDialog != null)
            {
                var canvasGroup = _currentDialog.GetComponent<CanvasGroup>();
                canvasGroup.interactable = false;
            }

            // Start server asynchronously
            System.Threading.Tasks.Task.Run(async () =>
            {
                bool success = await StorageServerManager.StartServerAsync();
                
                // Return to main thread
                MainThreadExecutor.dispatcher.QueueEvent(() =>
                {
                    if (success)
                    {
                        string serverUrl = StorageServerManager.ServerUrl;
                        string authToken = StorageServerManager.AuthToken;
                        string externalUrl = StorageServerManager.ExternalUrl;
                        bool upnpEnabled = StorageServerManager.UPnPEnabled;
                        
                        DebugConsole.Log($"[StorageServerDialog] Embedded server started at {serverUrl}");
                        DebugConsole.Log($"[StorageServerDialog] Authentication token: {authToken}");
                        
                        if (!string.IsNullOrEmpty(externalUrl) && upnpEnabled)
                        {
                            DebugConsole.Log($"[StorageServerDialog] External access enabled: {externalUrl}");
                            DebugConsole.Log("[StorageServerDialog] Share this external URL and auth token with remote players");
                        }
                        else if (!string.IsNullOrEmpty(StorageServerManager.ExternalIP))
                        {
                            DebugConsole.Log($"[StorageServerDialog] External IP detected: {StorageServerManager.ExternalIP}:29600");
                            DebugConsole.Log("[StorageServerDialog] UPnP failed - manual port forwarding may be needed");
                        }
                        else
                        {
                            DebugConsole.Log("[StorageServerDialog] Server only accessible locally - check network configuration");
                        }
                        
                        _onServerSelected?.Invoke(serverUrl);
                        Close();
                    }
                    else
                    {
                        DebugConsole.LogError("[StorageServerDialog] Failed to start embedded server", false);
                        
                        // Re-enable dialog
                        if (_currentDialog != null)
                        {
                            var canvasGroup = _currentDialog.GetComponent<CanvasGroup>();
                            canvasGroup.interactable = true;
                        }
                    }
                });
            });
        }

        private static void UseCustomServer(string url)
        {
            DebugConsole.Log($"[StorageServerDialog] Using custom server: {url}");
            
            // Set the server URL
            StorageServerManager.SetServerUrl(url);
            
            // Test connection asynchronously
            System.Threading.Tasks.Task.Run(async () =>
            {
                StorageServerManager.SetServerUrl(url);
                bool isReachable = await StorageServerManager.PerformHealthCheck();
                
                // Return to main thread
                MainThreadExecutor.dispatcher.QueueEvent(() =>
                {
                    if (isReachable)
                    {
                        DebugConsole.Log($"[StorageServerDialog] Successfully connected to custom server");
                        _onServerSelected?.Invoke(url);
                        Close();
                    }
                    else
                    {
                        DebugConsole.LogWarning($"[StorageServerDialog] Custom server at {url} is not reachable, but will use it anyway");
                        _onServerSelected?.Invoke(url);
                        Close();
                    }
                });
            });
        }

        private static void Close()
        {
            if (_currentDialog != null)
            {
                Object.Destroy(_currentDialog);
                _currentDialog = null;
            }
            _onServerSelected = null;
        }

        private static void AddDialogLabel(Transform parent, string text, Vector2 position, int fontSize)
        {
            GameObject labelGO = new GameObject("Label", typeof(RectTransform), typeof(Text));
            labelGO.transform.SetParent(parent, false);

            var rt = labelGO.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(350, 30);
            rt.anchoredPosition = position;

            var textComponent = labelGO.GetComponent<Text>();
            textComponent.text = text;
            textComponent.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            textComponent.fontSize = fontSize;
            textComponent.color = Color.white;
            textComponent.alignment = TextAnchor.MiddleCenter;
        }

        private static void AddDialogButton(Transform parent, string text, Vector2 position, System.Action onClick)
        {
            var template = Object.FindObjectOfType<MainMenu>()?.Button_ResumeGame;
            if (template == null)
            {
                DebugConsole.LogError("Cannot find template button to clone.");
                return;
            }

            GameObject btnGO = Object.Instantiate(template.gameObject, parent);
            btnGO.name = $"ServerDialog_{text.Replace(" ", "")}_Button";

            var rt = btnGO.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(200, 30);
            rt.anchoredPosition = position;

            var btn = btnGO.GetComponent<KButton>();

            var textComponents = btnGO.GetComponentsInChildren<LocText>(includeInactive: true);
            bool mainSet = false;
            foreach (var locText in textComponents)
            {
                if (!mainSet)
                {
                    locText.text = text;
                    mainSet = true;
                }
                else
                {
                    locText.text = "";
                }
            }

            btn.onClick += () => onClick();
        }

        private static InputField CreateServerUrlInput(Transform parent, Vector2 position)
        {
            GameObject inputGO = new GameObject("ServerUrlInput", typeof(RectTransform), typeof(Image), typeof(InputField));
            inputGO.transform.SetParent(parent, false);

            var rt = inputGO.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(300, 30);
            rt.anchoredPosition = position;

            var background = inputGO.GetComponent<Image>();
            background.color = new Color(0.2f, 0.2f, 0.2f, 1f);

            // Create text component for the input
            GameObject textGO = new GameObject("Text", typeof(RectTransform), typeof(Text));
            textGO.transform.SetParent(inputGO.transform, false);

            var textRt = textGO.GetComponent<RectTransform>();
            textRt.sizeDelta = Vector2.zero;
            textRt.anchorMin = Vector2.zero;
            textRt.anchorMax = Vector2.one;
            textRt.offsetMin = new Vector2(5, 0);
            textRt.offsetMax = new Vector2(-5, 0);

            var textComponent = textGO.GetComponent<Text>();
            textComponent.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            textComponent.fontSize = 12;
            textComponent.color = Color.white;
            textComponent.alignment = TextAnchor.MiddleLeft;

            // Create placeholder text
            GameObject placeholderGO = new GameObject("Placeholder", typeof(RectTransform), typeof(Text));
            placeholderGO.transform.SetParent(inputGO.transform, false);

            var placeholderRt = placeholderGO.GetComponent<RectTransform>();
            placeholderRt.sizeDelta = Vector2.zero;
            placeholderRt.anchorMin = Vector2.zero;
            placeholderRt.anchorMax = Vector2.one;
            placeholderRt.offsetMin = new Vector2(5, 0);
            placeholderRt.offsetMax = new Vector2(-5, 0);

            var placeholderText = placeholderGO.GetComponent<Text>();
            placeholderText.text = "http://localhost:3000";
            placeholderText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            placeholderText.fontSize = 12;
            placeholderText.color = new Color(0.7f, 0.7f, 0.7f, 1f);
            placeholderText.alignment = TextAnchor.MiddleLeft;

            var inputField = inputGO.GetComponent<InputField>();
            inputField.textComponent = textComponent;
            inputField.placeholder = placeholderText;

            return inputField;
        }
    }
}

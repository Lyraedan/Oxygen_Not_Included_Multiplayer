using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using ONI_MP.DebugTools;

namespace ONI_MP.Menus
{
    public static class ModCompatibilityDialog
    {
        private static GameObject currentDialog = null;

        public static void ShowIncompatibilityError(string reason, string[] missingMods, string[] extraMods, string[] versionMismatches)
        {
            // If only extra mods and no missing/version issues, show info dialog instead
            if ((missingMods == null || missingMods.Length == 0) &&
                (versionMismatches == null || versionMismatches.Length == 0) &&
                extraMods != null && extraMods.Length > 0)
            {
                ShowExtraModsInfo(extraMods);
                return;
            }

            // Otherwise show the full incompatibility dialog
            ShowFullIncompatibilityError(reason, missingMods, extraMods, versionMismatches);
        }

        private static void ShowExtraModsInfo(string[] extraMods)
        {
            try
            {
                DebugConsole.Log("[ModCompatibilityDialog] Showing extra mods info dialog...");

                // Close any existing dialog
                CloseDialog();

                // Create the info dialog
                CreateInfoDialog(extraMods);

                DebugConsole.Log("[ModCompatibilityDialog] Extra mods info dialog created successfully.");
            }
            catch (Exception ex)
            {
                DebugConsole.LogWarning($"[ModCompatibilityDialog] Failed to create info dialog: {ex.Message}");
                // Fallback to console logging
                LogExtraModsToConsole(extraMods);
            }
        }

        private static void ShowFullIncompatibilityError(string reason, string[] missingMods, string[] extraMods, string[] versionMismatches)
        {
            try
            {
                DebugConsole.Log("[ModCompatibilityDialog] Creating mod compatibility dialog...");

                // Close any existing dialog
                CloseDialog();

                // Create the dialog
                CreateDialog(reason, missingMods, extraMods, versionMismatches);

                DebugConsole.Log("[ModCompatibilityDialog] Mod compatibility dialog created successfully.");
            }
            catch (Exception ex)
            {
                DebugConsole.LogWarning($"[ModCompatibilityDialog] Failed to create dialog: {ex.Message}");
                // Fallback to console logging
                LogToConsole(reason, missingMods, extraMods, versionMismatches);
            }
        }

        private static void CreateDialog(string reason, string[] missingMods, string[] extraMods, string[] versionMismatches)
        {
            // Find a suitable parent canvas
            Canvas parentCanvas = GetBestCanvas();
            if (parentCanvas == null)
            {
                DebugConsole.LogWarning("[ModCompatibilityDialog] No suitable canvas found, falling back to console");
                LogToConsole(reason, missingMods, extraMods, versionMismatches);
                return;
            }

            // Create root dialog object
            currentDialog = new GameObject("ModCompatibilityDialog");
            currentDialog.transform.SetParent(parentCanvas.transform, false);

            // Add Canvas Group for fade effects and blocking
            CanvasGroup canvasGroup = currentDialog.AddComponent<CanvasGroup>();
            canvasGroup.alpha = 1f;
            canvasGroup.blocksRaycasts = true;

            // Set up as full screen overlay
            RectTransform rectTransform = currentDialog.AddComponent<RectTransform>();
            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.offsetMin = Vector2.zero;
            rectTransform.offsetMax = Vector2.zero;

            // Add background (semi-transparent black)
            Image background = currentDialog.AddComponent<Image>();
            background.color = new Color(0f, 0f, 0f, 0.7f);

            // Create main panel
            GameObject panel = CreateMainPanel(currentDialog, reason, missingMods, extraMods, versionMismatches);

            DebugConsole.Log("[ModCompatibilityDialog] Dialog created and displayed.");
        }

        private static GameObject CreateMainPanel(GameObject parent, string reason, string[] missingMods, string[] extraMods, string[] versionMismatches)
        {
            // Create main panel container
            GameObject panel = new GameObject("DialogPanel");
            panel.transform.SetParent(parent.transform, false);

            // Set up panel rect transform (centered dialog) - optimized size for content
            RectTransform panelRect = panel.AddComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0.5f, 0.5f);
            panelRect.anchorMax = new Vector2(0.5f, 0.5f);
            panelRect.pivot = new Vector2(0.5f, 0.5f);
            panelRect.sizeDelta = new Vector2(700f, 400f);

            // Add panel background
            Image panelBg = panel.AddComponent<Image>();
            panelBg.color = new Color(0.2f, 0.2f, 0.2f, 0.95f);

            // Add border
            Outline outline = panel.AddComponent<Outline>();
            outline.effectColor = new Color(1f, 0f, 0f, 0.8f);
            outline.effectDistance = new Vector2(2f, 2f);

            // Add layout group for organizing content
            VerticalLayoutGroup layoutGroup = panel.AddComponent<VerticalLayoutGroup>();
            layoutGroup.childAlignment = TextAnchor.UpperCenter;
            layoutGroup.childControlWidth = true;
            layoutGroup.childControlHeight = false;
            layoutGroup.childForceExpandWidth = true;
            layoutGroup.childForceExpandHeight = false;
            layoutGroup.spacing = 10f;
            layoutGroup.padding = new RectOffset(20, 20, 20, 20);

            // Create header
            CreateHeader(panel, "Mod Compatibility Error");

            // Create content area
            CreateContent(panel, reason, missingMods, extraMods, versionMismatches);

            // Create close button
            CreateCloseButton(panel);

            return panel;
        }

        private static void CreateHeader(GameObject parent, string title)
        {
            GameObject header = new GameObject("Header");
            header.transform.SetParent(parent.transform, false);

            // Header layout
            LayoutElement headerLayout = header.AddComponent<LayoutElement>();
            headerLayout.preferredHeight = 40f;

            // Header text
            TextMeshProUGUI headerText = header.AddComponent<TextMeshProUGUI>();
            headerText.text = title;
            headerText.fontSize = 24f;
            headerText.color = new Color(1f, 0.4f, 0.4f, 1f); // Light red
            headerText.alignment = TextAlignmentOptions.Center;
            headerText.fontStyle = FontStyles.Bold;

            // Header rect transform
            RectTransform headerRect = header.GetComponent<RectTransform>();
            headerRect.anchorMin = Vector2.zero;
            headerRect.anchorMax = Vector2.one;
            headerRect.offsetMin = Vector2.zero;
            headerRect.offsetMax = Vector2.zero;
        }

        private static void CreateContent(GameObject parent, string reason, string[] missingMods, string[] extraMods, string[] versionMismatches)
        {
            // Create scroll area for content
            GameObject scrollArea = new GameObject("ScrollArea");
            scrollArea.transform.SetParent(parent.transform, false);

            // Layout element - adjusted height for optimal content display
            LayoutElement scrollLayout = scrollArea.AddComponent<LayoutElement>();
            scrollLayout.flexibleHeight = 1f;
            scrollLayout.preferredHeight = 250f;

            // Scroll rect
            ScrollRect scrollRect = scrollArea.AddComponent<ScrollRect>();
            scrollRect.horizontal = false;
            scrollRect.vertical = true;
            scrollRect.scrollSensitivity = 20f;

            // Create viewport
            GameObject viewport = new GameObject("Viewport");
            viewport.transform.SetParent(scrollArea.transform, false);

            RectTransform viewportRect = viewport.AddComponent<RectTransform>();
            viewportRect.anchorMin = Vector2.zero;
            viewportRect.anchorMax = Vector2.one;
            viewportRect.offsetMin = Vector2.zero;
            viewportRect.offsetMax = Vector2.zero;

            // Add mask to viewport
            Image viewportImage = viewport.AddComponent<Image>();
            viewportImage.color = new Color(0f, 0f, 0f, 0.1f);
            Mask mask = viewport.AddComponent<Mask>();
            mask.showMaskGraphic = false;

            scrollRect.viewport = viewportRect;

            // Create content container
            GameObject content = new GameObject("Content");
            content.transform.SetParent(viewport.transform, false);

            RectTransform contentRect = content.AddComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0f, 1f);
            contentRect.anchorMax = new Vector2(1f, 1f);
            contentRect.pivot = new Vector2(0.5f, 1f);

            // Content layout group
            VerticalLayoutGroup contentLayout = content.AddComponent<VerticalLayoutGroup>();
            contentLayout.childAlignment = TextAnchor.UpperLeft;
            contentLayout.childControlWidth = true;
            contentLayout.childControlHeight = true;
            contentLayout.childForceExpandWidth = true;
            contentLayout.childForceExpandHeight = false;
            contentLayout.spacing = 5f;
            contentLayout.padding = new RectOffset(15, 15, 10, 10); // Increased side padding for better spacing

            // Content size fitter - configured to expand properly
            ContentSizeFitter contentFitter = content.AddComponent<ContentSizeFitter>();
            contentFitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained; // Use full width
            contentFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            scrollRect.content = contentRect;

            // Add reason text
            if (!string.IsNullOrEmpty(reason))
            {
                CreateTextElement(content, reason, new Color(1f, 1f, 1f, 1f), 16f, FontStyles.Bold);
                CreateTextElement(content, "", Color.white, 12f); // Spacer
            }

            // Add missing mods section
            if (missingMods != null && missingMods.Length > 0)
            {
                CreateTextElement(content, "MISSING MODS (install these):", new Color(1f, 0.5f, 0.5f, 1f), 14f, FontStyles.Bold);
                foreach (var mod in missingMods)
                {
                    CreateModEntryWithButton(content, mod, "Install", new Color(1f, 0.7f, 0.7f, 1f), new Color(0.2f, 0.6f, 0.2f, 1f));
                }
                CreateTextElement(content, "", Color.white, 8f); // Spacer
            }

            // Add extra mods section
            if (extraMods != null && extraMods.Length > 0)
            {
                CreateTextElement(content, "EXTRA MODS (disable these):", new Color(1f, 0.8f, 0.2f, 1f), 14f, FontStyles.Bold);
                foreach (var mod in extraMods)
                {
                    CreateModEntryWithButton(content, mod, "View", new Color(1f, 0.9f, 0.5f, 1f), new Color(0.8f, 0.4f, 0.1f, 1f));
                }
                CreateTextElement(content, "", Color.white, 8f); // Spacer
            }

            // Add version mismatches section
            if (versionMismatches != null && versionMismatches.Length > 0)
            {
                CreateTextElement(content, "VERSION MISMATCHES (update these):", new Color(0.5f, 0.8f, 1f, 1f), 14f, FontStyles.Bold);
                foreach (var mod in versionMismatches)
                {
                    CreateModEntryWithButton(content, mod, "Update", new Color(0.7f, 0.9f, 1f, 1f), new Color(0.2f, 0.4f, 0.8f, 1f));
                }
                CreateTextElement(content, "", Color.white, 8f); // Spacer
            }

            // Add instructions
            CreateTextElement(content, "Install/disable the required mods, then try connecting again.",
                new Color(0.8f, 0.8f, 0.8f, 1f), 12f, FontStyles.Italic);
        }

        private static void CreateTextElement(GameObject parent, string text, Color color, float fontSize, FontStyles fontStyle = FontStyles.Normal)
        {
            GameObject textObj = new GameObject("Text");
            textObj.transform.SetParent(parent.transform, false);

            TextMeshProUGUI textComponent = textObj.AddComponent<TextMeshProUGUI>();
            textComponent.text = text;
            textComponent.fontSize = fontSize;
            textComponent.color = color;
            textComponent.fontStyle = fontStyle;
            textComponent.alignment = TextAlignmentOptions.Left;
            textComponent.enableWordWrapping = true;

            // Layout element - configured to use full width
            LayoutElement layoutElement = textObj.AddComponent<LayoutElement>();
            layoutElement.preferredHeight = string.IsNullOrEmpty(text) ? fontSize * 0.5f : -1f;
            layoutElement.flexibleHeight = string.IsNullOrEmpty(text) ? 0f : -1f;
            layoutElement.flexibleWidth = 1f; // Use full available width
        }

        private static void CreateModEntryWithButton(GameObject parent, string modId, string buttonText, Color textColor, Color buttonColor)
        {
            // Create horizontal container for mod entry
            GameObject container = new GameObject("ModEntry");
            container.transform.SetParent(parent.transform, false);

            // Layout element for the container - configured to use full width
            LayoutElement containerLayout = container.AddComponent<LayoutElement>();
            containerLayout.preferredHeight = 35f;
            containerLayout.flexibleWidth = 1f; // Use full available width

            // Horizontal layout group - configured for full width usage
            HorizontalLayoutGroup horizontalLayout = container.AddComponent<HorizontalLayoutGroup>();
            horizontalLayout.childAlignment = TextAnchor.MiddleLeft;
            horizontalLayout.childControlWidth = true;
            horizontalLayout.childControlHeight = true;
            horizontalLayout.childForceExpandWidth = true;
            horizontalLayout.childForceExpandHeight = true;
            horizontalLayout.spacing = 10f;
            horizontalLayout.padding = new RectOffset(5, 5, 0, 0);

            // Create mod text
            GameObject textObj = new GameObject("ModText");
            textObj.transform.SetParent(container.transform, false);

            TextMeshProUGUI textComponent = textObj.AddComponent<TextMeshProUGUI>();
            textComponent.text = $"• {modId}";
            textComponent.fontSize = 12f;
            textComponent.color = textColor;
            textComponent.alignment = TextAlignmentOptions.Left;
            textComponent.enableWordWrapping = true;

            // Layout element for text (takes most space) - uses flexible width for full expansion
            LayoutElement textLayout = textObj.AddComponent<LayoutElement>();
            textLayout.flexibleWidth = 1f;
            textLayout.preferredWidth = 0f; // Let it expand to fill available space

            // Create install button
            GameObject buttonObj = new GameObject("InstallButton");
            buttonObj.transform.SetParent(container.transform, false);

            // Button rect transform
            RectTransform buttonRect = buttonObj.AddComponent<RectTransform>();
            buttonRect.sizeDelta = new Vector2(80f, 25f);

            // Button image
            Image buttonImage = buttonObj.AddComponent<Image>();
            buttonImage.color = buttonColor;

            // Button component
            Button button = buttonObj.AddComponent<Button>();
            button.targetGraphic = buttonImage;

            // Button colors
            ColorBlock colors = button.colors;
            colors.normalColor = buttonColor;
            colors.highlightedColor = new Color(buttonColor.r * 1.2f, buttonColor.g * 1.2f, buttonColor.b * 1.2f, 1f);
            colors.pressedColor = new Color(buttonColor.r * 0.8f, buttonColor.g * 0.8f, buttonColor.b * 0.8f, 1f);
            button.colors = colors;

            // Button click event
            string modIdCopy = modId; // Capture for closure
            button.onClick.AddListener(() => OpenSteamWorkshopPage(modIdCopy));

            // Layout element for button (fixed size)
            LayoutElement buttonLayout = buttonObj.AddComponent<LayoutElement>();
            buttonLayout.preferredWidth = 80f;
            buttonLayout.preferredHeight = 25f;

            // Button text
            GameObject buttonTextObj = new GameObject("ButtonText");
            buttonTextObj.transform.SetParent(buttonObj.transform, false);

            TextMeshProUGUI buttonTextComponent = buttonTextObj.AddComponent<TextMeshProUGUI>();
            buttonTextComponent.text = buttonText;
            buttonTextComponent.fontSize = 10f;
            buttonTextComponent.color = Color.white;
            buttonTextComponent.alignment = TextAlignmentOptions.Center;
            buttonTextComponent.fontStyle = FontStyles.Bold;

            RectTransform buttonTextRect = buttonTextObj.GetComponent<RectTransform>();
            buttonTextRect.anchorMin = Vector2.zero;
            buttonTextRect.anchorMax = Vector2.one;
            buttonTextRect.offsetMin = Vector2.zero;
            buttonTextRect.offsetMax = Vector2.zero;
        }

        private static void OpenSteamWorkshopPage(string modDisplayName)
        {
            try
            {
                // Extract numeric ID from display name (format: "Mod Name - 1234567890")
                string modId = ExtractModId(modDisplayName);

                string url = $"https://steamcommunity.com/sharedfiles/filedetails/?id={modId}";
                DebugConsole.Log($"[ModCompatibilityDialog] Opening Steam Workshop page for mod {modId}: {url}");

                // Use Steam overlay if possible, otherwise use default browser
                if (SteamManager.Initialized)
                {
                    // Try to open in Steam overlay
                    Steamworks.SteamFriends.ActivateGameOverlayToWebPage(url);
                    DebugConsole.Log("[ModCompatibilityDialog] Opened page using Steam overlay");
                }
                else
                {
                    // Fallback to system default browser
                    Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
                    DebugConsole.Log("[ModCompatibilityDialog] Opened page using system browser");
                }
            }
            catch (Exception ex)
            {
                DebugConsole.LogWarning($"[ModCompatibilityDialog] Failed to open Steam Workshop page for mod {modDisplayName}: {ex.Message}");
            }
        }

        private static string ExtractModId(string modDisplayName)
        {
            try
            {
                // If it contains " - " extract the ID after it
                if (modDisplayName.Contains(" - "))
                {
                    string[] parts = modDisplayName.Split(new string[] { " - " }, StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Length >= 2)
                    {
                        string lastPart = parts[parts.Length - 1];
                        // Check if the last part is numeric
                        if (System.Text.RegularExpressions.Regex.IsMatch(lastPart, @"^\d+$"))
                        {
                            return lastPart;
                        }
                    }
                }

                // Fallback: extract any numeric sequence from the string
                var match = System.Text.RegularExpressions.Regex.Match(modDisplayName, @"\d+");
                if (match.Success)
                {
                    return match.Value;
                }

                // If no numbers found, return the original string (shouldn't happen)
                DebugConsole.LogWarning($"[ModCompatibilityDialog] Could not extract numeric ID from: {modDisplayName}");
                return modDisplayName;
            }
            catch (Exception ex)
            {
                DebugConsole.LogWarning($"[ModCompatibilityDialog] Error extracting mod ID from {modDisplayName}: {ex.Message}");
                return modDisplayName;
            }
        }

        private static void CreateCloseButton(GameObject parent)
        {
            GameObject buttonObj = new GameObject("CloseButton");
            buttonObj.transform.SetParent(parent.transform, false);

            // Layout element
            LayoutElement buttonLayout = buttonObj.AddComponent<LayoutElement>();
            buttonLayout.preferredHeight = 40f;

            // Button rect transform
            RectTransform buttonRect = buttonObj.AddComponent<RectTransform>();
            buttonRect.sizeDelta = new Vector2(120f, 40f);

            // Button image
            Image buttonImage = buttonObj.AddComponent<Image>();
            buttonImage.color = new Color(0.6f, 0.2f, 0.2f, 1f);

            // Button component
            Button button = buttonObj.AddComponent<Button>();
            button.targetGraphic = buttonImage;

            // Button colors
            ColorBlock colors = button.colors;
            colors.normalColor = new Color(0.6f, 0.2f, 0.2f, 1f);
            colors.highlightedColor = new Color(0.8f, 0.3f, 0.3f, 1f);
            colors.pressedColor = new Color(0.4f, 0.1f, 0.1f, 1f);
            button.colors = colors;

            button.onClick.AddListener(() => CloseDialog());

            // Button text
            GameObject buttonTextObj = new GameObject("Text");
            buttonTextObj.transform.SetParent(buttonObj.transform, false);

            TextMeshProUGUI buttonText = buttonTextObj.AddComponent<TextMeshProUGUI>();
            buttonText.text = "OK";
            buttonText.fontSize = 16f;
            buttonText.color = Color.white;
            buttonText.alignment = TextAlignmentOptions.Center;
            buttonText.fontStyle = FontStyles.Bold;

            RectTransform buttonTextRect = buttonTextObj.GetComponent<RectTransform>();
            buttonTextRect.anchorMin = Vector2.zero;
            buttonTextRect.anchorMax = Vector2.one;
            buttonTextRect.offsetMin = Vector2.zero;
            buttonTextRect.offsetMax = Vector2.zero;
        }

        private static Canvas GetBestCanvas()
        {
            // Try to find the best canvas to attach to
            Canvas[] allCanvases = UnityEngine.Object.FindObjectsOfType<Canvas>();

            // Prefer overlay canvases with higher sorting order
            Canvas bestCanvas = null;
            int highestOrder = -1;

            foreach (Canvas canvas in allCanvases)
            {
                if (canvas != null && canvas.gameObject != null && canvas.gameObject.activeInHierarchy)
                {
                    if (canvas.renderMode == RenderMode.ScreenSpaceOverlay && canvas.sortingOrder > highestOrder)
                    {
                        bestCanvas = canvas;
                        highestOrder = canvas.sortingOrder;
                    }
                }
            }

            // If no overlay canvas found, use any active canvas
            if (bestCanvas == null)
            {
                foreach (Canvas canvas in allCanvases)
                {
                    if (canvas != null && canvas.gameObject != null && canvas.gameObject.activeInHierarchy)
                    {
                        bestCanvas = canvas;
                        break;
                    }
                }
            }

            return bestCanvas;
        }

        public static void CloseDialog()
        {
            if (currentDialog != null)
            {
                DebugConsole.Log("[ModCompatibilityDialog] Closing dialog.");
                UnityEngine.Object.Destroy(currentDialog);
                currentDialog = null;
            }
        }

        private static void CreateInfoDialog(string[] extraMods)
        {
            // Find a suitable parent canvas
            Canvas parentCanvas = GetBestCanvas();
            if (parentCanvas == null)
            {
                DebugConsole.LogWarning("[ModCompatibilityDialog] No suitable canvas found, falling back to console");
                LogExtraModsToConsole(extraMods);
                return;
            }

            // Create root dialog object
            currentDialog = new GameObject("ModInfoDialog");
            currentDialog.transform.SetParent(parentCanvas.transform, false);

            // Add Canvas Group for fade effects and blocking
            CanvasGroup canvasGroup = currentDialog.AddComponent<CanvasGroup>();
            canvasGroup.alpha = 1f;
            canvasGroup.blocksRaycasts = true;

            // Set up as full screen overlay
            RectTransform rectTransform = currentDialog.AddComponent<RectTransform>();
            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.offsetMin = Vector2.zero;
            rectTransform.offsetMax = Vector2.zero;

            // Add background (semi-transparent black)
            Image background = currentDialog.AddComponent<Image>();
            background.color = new Color(0f, 0f, 0f, 0.5f);

            // Create main panel
            GameObject panel = CreateInfoPanel(currentDialog, extraMods);

            DebugConsole.Log("[ModCompatibilityDialog] Info dialog created and displayed.");
        }

        private static GameObject CreateInfoPanel(GameObject parent, string[] extraMods)
        {
            // Create main panel container
            GameObject panel = new GameObject("InfoPanel");
            panel.transform.SetParent(parent.transform, false);

            // Set up panel rect transform (centered dialog)
            RectTransform panelRect = panel.AddComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0.5f, 0.5f);
            panelRect.anchorMax = new Vector2(0.5f, 0.5f);
            panelRect.pivot = new Vector2(0.5f, 0.5f);
            panelRect.sizeDelta = new Vector2(500f, 300f);

            // Add panel background (green tint for info)
            Image panelBg = panel.AddComponent<Image>();
            panelBg.color = new Color(0.1f, 0.3f, 0.1f, 0.95f);

            // Add border (green)
            Outline outline = panel.AddComponent<Outline>();
            outline.effectColor = new Color(0.2f, 0.8f, 0.2f, 0.8f);
            outline.effectDistance = new Vector2(2f, 2f);

            // Add layout group for organizing content
            VerticalLayoutGroup layoutGroup = panel.AddComponent<VerticalLayoutGroup>();
            layoutGroup.childAlignment = TextAnchor.UpperCenter;
            layoutGroup.childControlWidth = true;
            layoutGroup.childControlHeight = false;
            layoutGroup.childForceExpandWidth = true;
            layoutGroup.childForceExpandHeight = false;
            layoutGroup.spacing = 10f;
            layoutGroup.padding = new RectOffset(20, 20, 20, 20);

            // Create header
            CreateInfoHeader(panel, "Connection Allowed");

            // Create content area
            CreateInfoContent(panel, extraMods);

            // Create OK button
            CreateInfoButton(panel);

            return panel;
        }

        private static void CreateInfoHeader(GameObject parent, string title)
        {
            GameObject header = new GameObject("Header");
            header.transform.SetParent(parent.transform, false);

            // Header layout
            LayoutElement headerLayout = header.AddComponent<LayoutElement>();
            headerLayout.preferredHeight = 40f;

            // Header text
            TextMeshProUGUI headerText = header.AddComponent<TextMeshProUGUI>();
            headerText.text = title;
            headerText.fontSize = 20f;
            headerText.color = new Color(0.4f, 1f, 0.4f, 1f); // Light green
            headerText.alignment = TextAlignmentOptions.Center;
            headerText.fontStyle = FontStyles.Bold;

            // Header rect transform
            RectTransform headerRect = header.GetComponent<RectTransform>();
            headerRect.anchorMin = Vector2.zero;
            headerRect.anchorMax = Vector2.one;
            headerRect.offsetMin = Vector2.zero;
            headerRect.offsetMax = Vector2.zero;
        }

        private static void CreateInfoContent(GameObject parent, string[] extraMods)
        {
            // Create content message
            CreateTextElement(parent, $"You have {extraMods.Length} extra mod(s) that the host doesn't have.",
                new Color(0.9f, 0.9f, 0.9f, 1f), 14f);

            CreateTextElement(parent, "This is allowed and shouldn't cause issues.",
                new Color(0.7f, 0.9f, 0.7f, 1f), 12f);

            CreateTextElement(parent, "", Color.white, 8f); // Spacer

            // Show the extra mods
            CreateTextElement(parent, "Your extra mods:", new Color(0.8f, 0.8f, 0.8f, 1f), 12f, FontStyles.Bold);
            foreach (var mod in extraMods)
            {
                CreateTextElement(parent, $"• {mod}", new Color(0.6f, 0.8f, 0.6f, 1f), 11f);
            }
        }

        private static void CreateInfoButton(GameObject parent)
        {
            GameObject buttonObj = new GameObject("OKButton");
            buttonObj.transform.SetParent(parent.transform, false);

            // Layout element
            LayoutElement buttonLayout = buttonObj.AddComponent<LayoutElement>();
            buttonLayout.preferredHeight = 35f;

            // Button rect transform
            RectTransform buttonRect = buttonObj.AddComponent<RectTransform>();
            buttonRect.sizeDelta = new Vector2(100f, 35f);

            // Button image
            Image buttonImage = buttonObj.AddComponent<Image>();
            buttonImage.color = new Color(0.2f, 0.6f, 0.2f, 1f);

            // Button component
            Button button = buttonObj.AddComponent<Button>();
            button.targetGraphic = buttonImage;

            // Button colors
            ColorBlock colors = button.colors;
            colors.normalColor = new Color(0.2f, 0.6f, 0.2f, 1f);
            colors.highlightedColor = new Color(0.3f, 0.8f, 0.3f, 1f);
            colors.pressedColor = new Color(0.1f, 0.4f, 0.1f, 1f);
            button.colors = colors;

            button.onClick.AddListener(() => CloseDialog());

            // Button text
            GameObject buttonTextObj = new GameObject("Text");
            buttonTextObj.transform.SetParent(buttonObj.transform, false);

            TextMeshProUGUI buttonText = buttonTextObj.AddComponent<TextMeshProUGUI>();
            buttonText.text = "OK";
            buttonText.fontSize = 14f;
            buttonText.color = Color.white;
            buttonText.alignment = TextAlignmentOptions.Center;
            buttonText.fontStyle = FontStyles.Bold;

            RectTransform buttonTextRect = buttonTextObj.GetComponent<RectTransform>();
            buttonTextRect.anchorMin = Vector2.zero;
            buttonTextRect.anchorMax = Vector2.one;
            buttonTextRect.offsetMin = Vector2.zero;
            buttonTextRect.offsetMax = Vector2.zero;
        }

        private static void LogExtraModsToConsole(string[] extraMods)
        {
            DebugConsole.Log("[ModCompatibilityDialog] ========================================");
            DebugConsole.Log("[ModCompatibilityDialog] CONNECTION ALLOWED WITH EXTRA MODS");
            DebugConsole.Log("[ModCompatibilityDialog] ========================================");
            DebugConsole.Log($"[ModCompatibilityDialog] You have {extraMods.Length} extra mod(s):");
            foreach (var mod in extraMods)
            {
                DebugConsole.Log($"[ModCompatibilityDialog]   • {mod}");
            }
            DebugConsole.Log("[ModCompatibilityDialog] ========================================");
        }

        private static void LogToConsole(string reason, string[] missingMods, string[] extraMods, string[] versionMismatches)
        {
            DebugConsole.Log("[ModCompatibilityDialog] ========================================");
            DebugConsole.Log("[ModCompatibilityDialog] MOD COMPATIBILITY CHECK FAILED");
            DebugConsole.Log("[ModCompatibilityDialog] ========================================");

            if (!string.IsNullOrEmpty(reason))
            {
                DebugConsole.Log($"[ModCompatibilityDialog] Reason: {reason}");
            }

            if (missingMods != null && missingMods.Length > 0)
            {
                DebugConsole.Log("[ModCompatibilityDialog] MISSING MODS (install these):");
                foreach (var mod in missingMods)
                {
                    DebugConsole.Log($"[ModCompatibilityDialog]   • {mod}");
                }
            }

            if (extraMods != null && extraMods.Length > 0)
            {
                DebugConsole.Log("[ModCompatibilityDialog] EXTRA MODS (you have these, host doesn't):");
                foreach (var mod in extraMods)
                {
                    DebugConsole.Log($"[ModCompatibilityDialog]   • {mod}");
                }
            }

            if (versionMismatches != null && versionMismatches.Length > 0)
            {
                DebugConsole.Log("[ModCompatibilityDialog] VERSION MISMATCHES (update these):");
                foreach (var mod in versionMismatches)
                {
                    DebugConsole.Log($"[ModCompatibilityDialog]   • {mod}");
                }
            }

            DebugConsole.Log("[ModCompatibilityDialog] ========================================");
            DebugConsole.Log("[ModCompatibilityDialog] Please check console for mod details.");
            DebugConsole.Log("[ModCompatibilityDialog] Press Shift+F1 to open debug console.");
        }
    }
}
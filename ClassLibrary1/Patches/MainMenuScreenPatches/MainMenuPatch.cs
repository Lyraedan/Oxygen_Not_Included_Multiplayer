using HarmonyLib;
using ONI_MP;
using ONI_MP.Misc;
using ONI_MP.Networking;
using Steamworks;
using System;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;

[HarmonyPatch(typeof(MainMenu), "OnPrefabInit")]
internal static class MainMenuPatch
{
	private static GameObject staticBgGO;

	private static void Postfix(MainMenu __instance)
	{
		int normalFontSize = 20;
		var normalStyle = __instance.normalButtonStyle; 

		var buttonInfoType = __instance.GetType().GetNestedType("ButtonInfo", BindingFlags.NonPublic);

		var makeButton = __instance.GetType().GetMethod("MakeButton", BindingFlags.NonPublic | BindingFlags.Instance);

		// Multiplayer - Opens the multiplayer screen with all options
		var multiplayerInfo = CreateButtonInfo(
				MP_STRINGS.UI.MAINMENU.MULTIPLAYER.LABEL,
				new System.Action(() =>
				{
					// Open the multiplayer screen
					var canvas = UnityEngine.Object.FindObjectOfType<Canvas>();
					if (canvas != null)
					{
						ONI_MP.Menus.MultiplayerScreen.Show(canvas.transform);
					}
				}),
				normalFontSize,
				normalStyle,
				buttonInfoType
		);
		makeButton.Invoke(__instance, new object[] { multiplayerInfo });

		UpdatePromos();
		UpdateDLC();
		UpdateBuildNumber();
		AddSocials(__instance);

		UpdateLogo();
		UpdatePlacements(__instance);
	}

	// Reflection utility to build ButtonInfo struct
	private static object CreateButtonInfo(string text, System.Action action, int fontSize, ColorStyleSetting style, Type buttonInfoType)
	{
		var buttonInfo = Activator.CreateInstance(buttonInfoType);
		buttonInfoType.GetField("text").SetValue(buttonInfo, new LocString(text));
		buttonInfoType.GetField("action").SetValue(buttonInfo, action);
		buttonInfoType.GetField("fontSize").SetValue(buttonInfo, fontSize);
		buttonInfoType.GetField("style").SetValue(buttonInfo, style);
		return buttonInfo;
	}

	private static void UpdatePlacements(MainMenu __instance)
	{
		var buttonParent = Traverse.Create(__instance).Field("buttonParent").GetValue<GameObject>();
		if (buttonParent != null)
		{
			var children = buttonParent.GetComponentsInChildren<KButton>(true);

            var newGameBtn = children.FirstOrDefault(b =>
                    b.GetComponentInChildren<LocText>()?.text.ToUpper().Contains("NEW GAME") == true);

            // Find "Load Game" button
            var loadGameBtn = children.FirstOrDefault(b =>
					b.GetComponentInChildren<LocText>()?.text.ToUpper().Contains("LOAD GAME") == true);

			// Find Multiplayer button
			var multiplayerBtn = children.FirstOrDefault(b =>
					b.GetComponentInChildren<LocText>()?.text.ToUpper().Contains("MULTIPLAYER") == true);

			if (loadGameBtn != null && multiplayerBtn != null)
			{
				int loadGameIdx = loadGameBtn.transform.GetSiblingIndex();
				// Move Multiplayer immediately after "Load Game"
				multiplayerBtn.transform.SetSiblingIndex(loadGameIdx + 1);
			} else if(loadGameBtn == null && newGameBtn != null && multiplayerBtn != null)
			{
				int newGameIdx = newGameBtn.transform.GetSiblingIndex();
                // Move Multiplayer immediately after "New Game"
                multiplayerBtn.transform.SetSiblingIndex(newGameIdx + 1);
            }
        }
	}

	private static void UpdateLogo()
	{
		// Attempt to find and replace the logo
		GameObject logoObj = GameObject.Find("Logo");
		if (logoObj != null)
		{
			var image = logoObj.GetComponent<UnityEngine.UI.Image>();
			if (image != null)
			{
				Texture2D tex = ResourceLoader.LoadEmbeddedTexture("ONI_MP.Assets.oni_together_logo.png");
				if (tex != null)
				{
					Sprite newSprite = Sprite.Create(
							tex,
							new Rect(0, 0, tex.width, tex.height),
							new Vector2(0.5f, 0.5f)
					);
					image.sprite = newSprite;
				}
			}
		}

	}
	private static void UpdatePromos()
	{
		GameObject uiGroup = GameObject.Find("UI Group");
		if (uiGroup == null)
			return;

		GameObject topLeftColumns = GameObject.Find("TopLeftColumns");
		if (topLeftColumns == null)
			return;

		GameObject promoContainer = new GameObject("ONI_MP_PromoContainer", typeof(RectTransform));
		promoContainer.transform.SetParent(uiGroup.transform, false);

		RectTransform promoRect = promoContainer.GetComponent<RectTransform>();
		promoRect.anchorMin = new Vector2(0f, 0f);
		promoRect.anchorMax = new Vector2(0f, 0f);
		promoRect.pivot = new Vector2(0f, 0f);
		promoRect.anchoredPosition = new Vector2(30f, 30f);
		promoRect.sizeDelta = new Vector2(1000f, 215f);

		string[] motdNames = { "MOTDBox_A", "MOTDBox_B", "MOTDBox_C" };
		float bannerWidth = 300f;
		float bannerHeight = 215f;
		float spacing = 10f;

		for (int i = 0; i < motdNames.Length; i++)
		{
			Transform banner = topLeftColumns.transform.Find("MOTD/" + motdNames[i]);
			if (banner != null)
			{
				banner.SetParent(promoContainer.transform, false);

				RectTransform bannerRect = banner.GetComponent<RectTransform>();
				bannerRect.anchorMin = new Vector2(0f, 0f);
				bannerRect.anchorMax = new Vector2(0f, 0f);
				bannerRect.pivot = new Vector2(0f, 0f);
				bannerRect.sizeDelta = new Vector2(bannerWidth, bannerHeight);
				bannerRect.anchoredPosition = new Vector2((bannerWidth + spacing) * i, 0f);
			}
		}
	}

	private static void UpdateDLC()
	{
		Transform dlcLogos = GameObject.Find("DLCLogos (1)")?.transform;
		Transform topLeft = GameObject.Find("TopLeftColumns")?.transform;

		if (dlcLogos == null || topLeft == null)
			return;

		dlcLogos.SetParent(topLeft, true);

		var rect = dlcLogos.GetComponent<RectTransform>();
		rect.anchorMin = new Vector2(0f, 1f);
		rect.anchorMax = new Vector2(0f, 1f);
		rect.pivot = new Vector2(0f, 1f);
		rect.anchoredPosition = new Vector2(20f, 0f);
		rect.localScale = Vector3.one;

		dlcLogos.SetAsFirstSibling();
	}

	private static void UpdateBuildNumber()
	{
		GameObject promoContainer = GameObject.Find("ONI_MP_PromoContainer");
		GameObject watermark = GameObject.Find("BuildWatermark");

		if (promoContainer == null)
			return;

		if (watermark == null)
			return;

		RectTransform promoRect = promoContainer.GetComponent<RectTransform>();
		RectTransform watermarkRect = watermark.GetComponent<RectTransform>();

		// Re-parent the watermark to the same parent as the promo container
		watermark.transform.SetParent(promoContainer.transform.parent, worldPositionStays: false);

		// Anchor it to bottom-left
		watermarkRect.anchorMin = new Vector2(0f, 0f);
		watermarkRect.anchorMax = new Vector2(0f, 0f);
		watermarkRect.pivot = new Vector2(0f, 0f);

		// Place it just above the DLC panels (which are 215 high)
		watermarkRect.anchoredPosition = new Vector2(30f, 260f);
	}

	private static void AddSocials(MainMenu menu)
	{
		var promoContainer = GameObject.Find("ONI_MP_PromoContainer");
		if (promoContainer == null)
		{
			return;
		}

		GameObject socialsContainer = new GameObject("ONI_MP_SocialsContainer", typeof(RectTransform));
		socialsContainer.transform.SetParent(promoContainer.transform.parent, false);

		RectTransform socialsRect = socialsContainer.GetComponent<RectTransform>();
		socialsRect.anchorMin = new Vector2(0f, 0f);
		socialsRect.anchorMax = new Vector2(0f, 0f);
		socialsRect.pivot = new Vector2(0f, 0f);

		// place right next to the promos
		socialsRect.anchoredPosition = new Vector2(
				promoContainer.GetComponent<RectTransform>().anchoredPosition.x + 925f,
				30f
		);


		var layout = socialsContainer.AddComponent<HorizontalLayoutGroup>();
		layout.childAlignment = TextAnchor.MiddleLeft;
		layout.spacing = 10f;
		layout.childForceExpandHeight = false;
		layout.childForceExpandWidth = false;
		layout.childControlHeight = false;
		layout.childControlWidth = false;

		// Example Discord button
		var discordSprite = ResourceLoader.LoadEmbeddedTexture("ONI_MP.Assets.discord.png");
		AddSocialButton(socialsContainer.transform, MP_STRINGS.UI.MAINMENU.DISCORD_INFO, "https://discord.gg/jpxveK6mmY", discordSprite);

		// Automatically resize the container to properly fit the buttons
		int buttonCount = socialsContainer.transform.childCount;
		float buttonWidth = 96f;
		float totalWidth = buttonCount * buttonWidth + (buttonCount - 1) * layout.spacing;

		socialsRect.sizeDelta = new Vector2(totalWidth, 100f); // keep the same height
	}

	private static void AddSocialButton(Transform parent, string tooltip, string url, Texture2D spriteSheet)
	{
		if (spriteSheet == null)
			return;

		GameObject buttonGO = new GameObject($"SocialButton_{tooltip}", typeof(RectTransform));
		buttonGO.transform.SetParent(parent, false);

		var buttonImage = buttonGO.AddComponent<Image>();

		var button = buttonGO.AddComponent<Button>();

		var rectTransform = button.GetComponent<RectTransform>();
		rectTransform.sizeDelta = new Vector2(96f, 96f);

		// slice the spritesheet (3 frames horizontally)
		Sprite normalSprite = Sprite.Create(spriteSheet, new Rect(0, 0, 512, 512), new Vector2(0.5f, 0.5f));
		Sprite highlightedSprite = Sprite.Create(spriteSheet, new Rect(512, 0, 512, 512), new Vector2(0.5f, 0.5f));
		Sprite pressedSprite = Sprite.Create(spriteSheet, new Rect(1024, 0, 512, 512), new Vector2(0.5f, 0.5f));

		buttonImage.sprite = normalSprite;

		var spriteState = new SpriteState
		{
			highlightedSprite = highlightedSprite,
			pressedSprite = pressedSprite
		};
		button.spriteState = spriteState;
		button.transition = Selectable.Transition.SpriteSwap;

		var tooltipComp = buttonGO.AddComponent<ToolTip>();
		tooltipComp.toolTip = tooltip;

		button.onClick.AddListener(() =>
		{
			Application.OpenURL(url);
		});
	}
}

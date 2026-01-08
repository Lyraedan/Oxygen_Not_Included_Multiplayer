using ONI_MP.UI.lib;
using UI.lib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using ONI_MP.DebugTools;

namespace ONI_MP
{
	public static class ModAssets
	{
		public static GameObject ParentScreen => App.GetCurrentSceneName() == "frontend" ? FrontEndManager.Instance.gameObject : GameScreenManager.Instance.GetParent(GameScreenManager.UIRenderTarget.ScreenSpaceOverlay);
		public static GameObject MP_ScreenPrefab, MP_PW_Dialogue;

		public static void LoadAssetBundles()
		{
			var bundle = AssetUtils.LoadAssetBundle("oni_mp_ui_assets", platformSpecific: true);
			MP_ScreenPrefab = bundle.LoadAsset<GameObject>("Assets/UIs/mp_screen.prefab");
			MP_PW_Dialogue = bundle.LoadAsset<GameObject>("Assets/UIs/mp_password_dialogue.prefab");

			var TMPConverter = new TMPConverter();
			DebugConsole.Log("Loading main screen prefab...");
			TMPConverter.ReplaceAllText(MP_ScreenPrefab);
			DebugConsole.Log("Loading password dialogue prefab...");
			TMPConverter.ReplaceAllText(MP_PW_Dialogue);
		}
	}
}

using HarmonyLib;
using ONI_MP.UI;
using Shared.Profiling;

[HarmonyPatch(typeof(CameraController), nameof(CameraController.OnKeyDown))]
public static class CameraControllerPatch
{
	static bool Prefix(KButtonEvent e)
	{
		Profiler.Scope();

		// Block camera zoom if mouse is over chat panel
		if (ChatScreen.IsMouseOverChatPanel())
		{
			if (e.IsAction(Action.ZoomIn) || e.IsAction(Action.ZoomOut))
			{
				// Skip the original method
				return false;
			}
		}

		// Allow original method to run
		return true;
	}
}

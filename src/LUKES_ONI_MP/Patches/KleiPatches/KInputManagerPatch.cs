using HarmonyLib;
using ONI_MP.UI;

[HarmonyPatch(typeof(KInputManager), nameof(KInputManager.Update))]
public static class KInputManagerPatch
{
    static bool Prefix()
    {
        // Suppress input processing while typing in chat
        if (ChatScreen.IsFocused())
        {
            return false; // Skip Update() entirely
        }

        return true; // Allow input through
    }
}

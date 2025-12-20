using HarmonyLib;
using KMod;
using PeterHan.PLib.Core;

namespace ONI_MP.Api
{
    public class MultiplayerApi : UserMod2
    {
        public override void OnLoad(Harmony harmony)
        {
            base.OnLoad(harmony);

            PUtil.InitLibrary(true);
        }
    }
}

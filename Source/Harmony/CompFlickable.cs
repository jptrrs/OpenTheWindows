using HarmonyLib;
using RimWorld;
using Verse;

namespace OpenTheWindows
{
    [HarmonyPatch(typeof(CompFlickable), nameof(CompFlickable.DoFlick))]
    public static class CompFlickable_DoFlick
    {
        public static void Postfix(CompFlickable __instance)
        {
            if (__instance is CompWindow compWindow)
                compWindow.SwitchIsOn = !compWindow.SwitchIsOn;
        }
    }

    [HarmonyPatch(typeof(CompFlickable), nameof(CompFlickable.PostExposeData))]
    public static class CompFlickable_PostExposeData
    {
        public static bool Prefix(CompFlickable __instance, bool ___switchOnInt, bool ___wantSwitchOn)
        {
            if (!(__instance is CompWindow compWindow)) return true;

            if (Scribe.mode == LoadSaveMode.PostLoadInit)
                compWindow.SetupState();

            return false;
        }
    }
}
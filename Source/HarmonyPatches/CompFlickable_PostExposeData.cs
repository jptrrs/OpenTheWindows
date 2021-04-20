using HarmonyLib;
using RimWorld;
using Verse;

namespace OpenTheWindows
{
    //Initialize Window components on spawn.
    [HarmonyPatch(typeof(CompFlickable), nameof(CompFlickable.PostExposeData))]
    public static class CompFlickable_PostExposeData
    {
        public static bool Prefix(CompFlickable __instance, bool ___switchOnInt, bool ___wantSwitchOn)
        {
            if (!(__instance is CompWindow compWindow)) return true;
            if (Scribe.mode == LoadSaveMode.PostLoadInit) compWindow.SetupState();
            return false;
        }
    }
}
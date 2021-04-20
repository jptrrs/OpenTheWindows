using HarmonyLib;
using RimWorld;
using Verse;

namespace OpenTheWindows
{
    [HarmonyPatch(typeof(CompFlickable), nameof(CompFlickable.DoFlick))]
    public static class CompFlickable_DoFlick
    {
        public static void Prefix(CompFlickable __instance, CompProperties_Flickable ___props)
        {
            var compWindow = __instance as CompWindow;
            string signal = compWindow != null ? compWindow.Props.signal : "normal";
            Log.Message($"{___props.compClass} was flicked! ({signal})");
        }
    }

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

    [HarmonyPatch(typeof(CompFlickable), nameof(CompFlickable.WantsFlick))]
    public static class CompFlickable_WantsFlick
    {
        public static void Postfix(CompFlickable __instance, bool __result, CompProperties_Flickable ___props)
        {
            var compWindow = __instance as CompWindow;
            string signal = compWindow != null ? compWindow.Props.signal : "normal";
            if (__result) Log.Message($"{___props.compClass} wants to flick! ({signal})");
            else Log.Message($"{___props.compClass} dosen't wanna flick! ({signal})");
        }
    }
}
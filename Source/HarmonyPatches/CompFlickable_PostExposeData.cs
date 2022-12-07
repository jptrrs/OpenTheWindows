using HarmonyLib;
using RimWorld;
using Verse;

namespace OpenTheWindows
{
    //Initialize Window components on spawn.
    [HarmonyPatch(typeof(CompFlickable), nameof(CompFlickable.PostExposeData))]
    public static class CompFlickable_PostExposeData
    {
        public static bool Prefix(CompFlickable __instance)
        {
            if (Scribe.mode == LoadSaveMode.PostLoadInit && __instance is CompWindow compWindow)
            {
                compWindow.SetupState();
                return false;
            } 
            return true;
        }
    }
}
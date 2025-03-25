using HarmonyLib;
using RimWorld;
using Verse;

namespace OpenTheWindows
{
    //Replaces StuckOpen property for Building_Window (because overriding won't cut it)
    [HarmonyPatch(typeof(Building_Door), "StuckOpen", MethodType.Getter)]
    public static class Building_Door_StuckOpen
    {
        public static bool Prefix(object __instance)
        {
            return !(__instance is Building_Window);
        }
    }
}
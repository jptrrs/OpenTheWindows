using HarmonyLib;
using System.Reflection;
using Verse;

namespace OpenTheWindows
{
    //Triggers the illuminated area recalculation when roof is constructed.
    [HarmonyPatch(typeof(RoofGrid), nameof(RoofGrid.SetRoof))]
    public static class RoofGrid_SetRoof
    {
        public static void Postfix(RoofGrid __instance, IntVec3 c, RoofDef def)
        {
            var info = new MapUpdateWatcher.MapUpdateInfo()
            {
                center = c,
                removing = def == null
            };
            MapUpdateWatcher.OnMapUpdate(__instance, info);
        }
    }
}
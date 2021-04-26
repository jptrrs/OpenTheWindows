using HarmonyLib;
using System.Reflection;
using Verse;

namespace OpenTheWindows
{
    //Triggers the illuminated area recalculation when roof is constructed.
    [HarmonyPatch(typeof(RoofGrid), nameof(RoofGrid.SetRoof))]
    public static class RoofGrid_SetRoof
    {
        public static void Prefix(RoofGrid __instance, IntVec3 c, out RoofDef __state)
        {
            __state = HarmonyPatcher.ExpandedRoofing ? __instance.RoofAt(c) : null;
        }

        public static void Postfix(RoofGrid __instance, IntVec3 c, RoofDef def, RoofDef __state)
        {
            bool removing = def == null;
            RoofDef defToPass = HarmonyPatcher.ExpandedRoofing ? (removing ? __state : def) : def;
            var info = new MapUpdateWatcher.MapUpdateInfo()
            {
                center = c,
                removed = removing,
                roofDef = defToPass
            };
            MapUpdateWatcher.OnMapUpdate(__instance, info);
        }
    }
}
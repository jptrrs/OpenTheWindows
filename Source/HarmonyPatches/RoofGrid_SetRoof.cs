using HarmonyLib;
using System.Reflection;
using Verse;

namespace OpenTheWindows
{
    //Triggers the illuminated area recalculation when roof is constructed.
    [HarmonyPatch(typeof(RoofGrid), nameof(RoofGrid.SetRoof))]
    public static class RoofGrid_SetRoof
    {
        public static void Prefix(RoofGrid __instance, IntVec3 c, RoofDef def)
        {
            FieldInfo mapInfo = AccessTools.Field(typeof(RoofGrid), "map");
            Map map = (Map)mapInfo.GetValue(__instance);
            MapComp_Windows mapComp = map.GetComponent<MapComp_Windows>();

            if (mapComp != null && mapComp.WindowScanGrid[map.cellIndices.CellToIndex(c)] > 0)
                mapComp.roofUpdateRequest = true;
        }
    }
}
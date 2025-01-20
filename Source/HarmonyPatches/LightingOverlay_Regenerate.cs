using HarmonyLib;
using System.Collections.Generic;
using System.Reflection;
using Verse;

namespace OpenTheWindows
{
    //Maps the roofs over the windows's influence area.
    [HarmonyPatch(typeof(SectionLayer_LightingOverlay), nameof(SectionLayer_LightingOverlay.Regenerate))]
    public static class LightingOverlay_Regenerate
    {
        //public static void Prefix(SectionLayer_LightingOverlay __instance, ref int[] __state)
        //{
        //    Map map = __instance.Map;
        //    MapComp_Windows mapComp;
        //    MapComp_Windows mapComp2;
        //    if (MapComp_Windows.LightComps.TryGetValue(map.uniqueID, out mapComp))
        //    {
        //        mapComp2 = mapComp;
        //    }
        //    else
        //    {
        //        mapComp2 = map.GetComponent<MapComp_Skylights>();
        //    }
        //    __state = mapComp2.SectionCells(__instance.section);
        //    foreach (int num in __state)
        //    {
        //        if (mapComp2.SkylightGrid[num])
        //        {
        //            mapComp2.roofGridCopy[num] = map.roofGrid.roofGrid[num];
        //            map.roofGrid.roofGrid[num] = null;
        //        }
        //    }
        //}

        //public static void Postfix(SectionLayer_LightingOverlay __instance, int[] __state)
        //{
        //    Map map = __instance.Map;
        //    MapComp_Skylights mapComp_Skylights;
        //    MapComp_Skylights mapComp_Skylights2;
        //    if (MapComp_Skylights.LightComps.TryGetValue(map.uniqueID, out mapComp_Skylights))
        //    {
        //        mapComp_Skylights2 = mapComp_Skylights;
        //    }
        //    else
        //    {
        //        mapComp_Skylights2 = map.GetComponent<MapComp_Skylights>();
        //    }
        //    foreach (int num in __state)
        //    {
        //        if (mapComp_Skylights2.SkylightGrid[num])
        //        {
        //            map.roofGrid.roofGrid[num] = mapComp_Skylights2.roofGridCopy[num];
        //        }
        //    }
        //}



        public static Dictionary<IntVec3, RoofDef> changedRoofs;
        public static Map map = null;
        public static RoofDef[] roofRef = null;
        public static bool stateDirty = true;
        public static MapComp_Windows windowComponent = null;
        static FieldInfo roofGridInfo = AccessTools.Field(typeof(RoofGrid), "roofGrid");

        [HarmonyBefore(new string[] {"raisetheroof.harmony"})]
        public static void Prefix()
        {
            changedRoofs = new Dictionary<IntVec3, RoofDef>();
            if (stateDirty || map != Find.CurrentMap)
            {
                map = Find.CurrentMap; // cache map
                roofRef = (RoofDef[])roofGridInfo.GetValue(map.roofGrid); // cache roofgrid
                windowComponent = map.GetComponent<MapComp_Windows>(); // cache windowcomponent
                // we are clean
                stateDirty = false;
            }
            foreach (IntVec3 cell in windowComponent.WindowCells)
            {
                var index = map.cellIndices.CellToIndex(cell);
                changedRoofs.Add(cell, roofRef[index]);
                roofRef[index] = null;
            }
        }

        public static void Postfix()
        {
            foreach (KeyValuePair<IntVec3, RoofDef> entry in changedRoofs)
            {
                roofRef[map.cellIndices.CellToIndex(entry.Key)] = entry.Value;
            }
        }
    }
}
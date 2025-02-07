using HarmonyLib;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Verse;

namespace OpenTheWindows
{
    //Maps the roofs over the windows's influence area.
    [HarmonyPatch(typeof(SectionLayer_LightingOverlay), nameof(SectionLayer_LightingOverlay.Regenerate))]
    public static class LightingOverlay_Regenerate
    {
        public static MapComp_Windows windowsMapComponent = null;
        //static int lastMapID;
        //static RoofDef[] roofRef = null;
        public static bool stateDirty = true;
        //static FieldInfo roofGridInfo = AccessTools.Field(typeof(RoofGrid), "roofGrid");

        [HarmonyBefore(new string[] { "raisetheroof.harmony" })]
        public static void Prefix(SectionLayer_LightingOverlay __instance, ref Dictionary<int, RoofDef> __state)
        {
            Map map = __instance.Map;
            MapComp_Windows cachedComp;
            MapComp_Windows actualComp;
            if (MapComp_Windows.MapCompsCache.TryGetValue(map.uniqueID, out cachedComp))
            {
                actualComp = cachedComp;
            }
            else
            {
                actualComp = map.GetComponent<MapComp_Windows>();
            }
            __state = new Dictionary<int, RoofDef>();
            //windowsMapComponent = map.GetComponent<MapComp_Windows>();
            //if (stateDirty || lastMapID != map.uniqueID) //Build cache if necessary
            //{
            //    lastMapID = map.uniqueID;
            //    roofRef = (RoofDef[])roofGridInfo.GetValue(map.roofGrid);
            //    //windowsMapComponent = map.GetComponent<MapComp_Windows>();
            //    stateDirty = false;
            //}
            int[] sectionCells = actualComp.GetCachedSectionCells(__instance.section);
            RoofDef[] roofRef = map.roofGrid.roofGrid;
            foreach (int num in actualComp.WindowCells.Intersect(sectionCells))
            {
                __state.Add(num, roofRef[num]);
                roofRef[num] = null;
            }
        }

        public static void Postfix(SectionLayer_LightingOverlay __instance, ref Dictionary<int, RoofDef> __state)
        {
            Map map = __instance.Map;
            foreach (var entry in __state)
            {
                map.roofGrid.roofGrid[entry.Key] = entry.Value;
                //roofRef[entry.Key] = entry.Value;
            }
        }
    }
}
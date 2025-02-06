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
        static int lastMapID;
        static RoofDef[] roofRef = null;
        static bool stateDirty = true;
        static FieldInfo roofGridInfo = AccessTools.Field(typeof(RoofGrid), "roofGrid");

        [HarmonyBefore(new string[] { "raisetheroof.harmony" })]
        public static void Prefix(SectionLayer_LightingOverlay __instance, ref Dictionary<int, RoofDef> __state)
        {
            Map map = __instance.Map;
            __state = new Dictionary<int, RoofDef>();
            if (stateDirty || lastMapID != map.uniqueID) //Build cache if necessary
            {
                lastMapID = map.uniqueID;
                roofRef = (RoofDef[])roofGridInfo.GetValue(map.roofGrid);
                windowsMapComponent = map.GetComponent<MapComp_Windows>();
                stateDirty = false;
            }
            int[] sectionCells = windowsMapComponent.GetCachedSectionCells(__instance.section);
            foreach (int num in windowsMapComponent.WindowCells.Intersect(sectionCells))
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
                roofRef[entry.Key] = entry.Value;
            }
        }
    }
}
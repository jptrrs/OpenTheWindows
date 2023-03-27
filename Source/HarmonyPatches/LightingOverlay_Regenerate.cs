using HarmonyLib;
using System.Collections.Generic;
using Verse;

namespace OpenTheWindows
{
    //Maps the roofs over the windows's influence area.
    [HarmonyPatch(typeof(SectionLayer_LightingOverlay), nameof(SectionLayer_LightingOverlay.Regenerate))]
    public static class LightingOverlay_Regenerate
    {
        public static Dictionary<IntVec3, RoofDef> changedRoofs;
        public static Map map = null;
        public static RoofDef[] roofRef = null;
        public static bool stateDirty = true;
        public static MapComp_Windows windowComponent = null;

        [HarmonyBefore(new string[] {"raisetheroof.harmony"})]
        public static void Prefix(SectionLayer_LightingOverlay __instance)
        {
            changedRoofs = new Dictionary<IntVec3, RoofDef>();
            if (stateDirty || map != __instance.Map)
            {
                map = __instance.Map; // cache map
                roofRef = map?.roofGrid?.roofGrid; // cache roofgrid
                if (roofRef == null) return;
                windowComponent = map.GetComponent<MapComp_Windows>(); // cache windowcomponent
                // we are clean
                stateDirty = false;
            }
            if (map != null && roofRef != null)
            {
                foreach (IntVec3 cell in windowComponent.WindowCells)
                {
                    var index = map.cellIndices.CellToIndex(cell);
                    changedRoofs.Add(cell, roofRef[index]);
                    roofRef[index] = null;
                }
            }
        }

        public static void Postfix()
        {
            if (map != null && roofRef != null)
            {
                foreach (KeyValuePair<IntVec3, RoofDef> entry in changedRoofs)
                {
                    roofRef[map.cellIndices.CellToIndex(entry.Key)] = entry.Value;
                }
            }
        }
    }
}
using HarmonyLib;
using Verse;

namespace OpenTheWindows
{
    //Triggers the illuminated area recalculation when thing is constructed/deconstructed.
    [HarmonyPatch(typeof(ThingGrid), nameof(ThingGrid.RegisterInCell))]
    public static class ThingGrid_Register
    {
        public static void Prefix(Thing t, IntVec3 c)
        {
            if (t is Building && t.def.passability == Traversability.Impassable)
            {
                MapComp_Windows mapComp = t.Map.GetComponent<MapComp_Windows>();
                if (mapComp != null && mapComp.WindowScanGrid[t.Map.cellIndices.CellToIndex(c)] > 0)
                {
                    mapComp.updateRequest = true;
                }
            }
        }
    }

    [HarmonyPatch(typeof(ThingGrid), nameof(ThingGrid.DeregisterInCell))]
    public static class ThingGrid_Deregister
    {
        public static void Prefix(Thing t, IntVec3 c)
        {
            if (t is Building && t.def.passability == Traversability.Impassable)
            {
                MapComp_Windows mapComp = t.Map.GetComponent<MapComp_Windows>();
                if (mapComp != null && mapComp.WindowScanGrid[t.Map.cellIndices.CellToIndex(c)] > 0)
                {
                    mapComp.updateRequest = true;
                }
            }
        }
    }
}
using HarmonyLib;
using Verse;

namespace OpenTheWindows
{
    //Triggers the illuminated area recalculation when thing is constructed/deconstructed.
    [HarmonyPatch(typeof(ThingGrid), nameof(ThingGrid.RegisterInCell))]
    public static class ThingGrid_Register
    {
        public static void Postfix(Thing t, IntVec3 c)
        {
            if (t is Building && t.def.passability == Traversability.Impassable)
            {
                var info = new MapUpdateWatcher.MapUpdateInfo()
                {
                    center = c,
                    removing = false
                };
                MapUpdateWatcher.OnMapUpdate(t, info);
            }
        }
    }

    [HarmonyPatch(typeof(ThingGrid), nameof(ThingGrid.DeregisterInCell))]
    public static class ThingGrid_Deregister
    {
        public static void Postfix(Thing t, IntVec3 c)
        {
            if (t is Building && t.def.passability == Traversability.Impassable)
            {
                var info = new MapUpdateWatcher.MapUpdateInfo()
                {
                    center = c,
                    removing = true
                };
                MapUpdateWatcher.OnMapUpdate(t, info);
            }
        }
    }
}
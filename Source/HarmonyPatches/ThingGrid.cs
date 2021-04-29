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
            if (Scribe.mode == LoadSaveMode.LoadingVars) return;
            if ((t is Building && t.def.passability == Traversability.Impassable) || (HarmonyPatcher.DubsSkylights && t.GetType() == HarmonyPatcher.Building_Skylight))
            {
                var info = new MapUpdateWatcher.MapUpdateInfo()
                {
                    center = c,
                    removed = false,
                    map = t.Map
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
            if ((t is Building && t.def.passability == Traversability.Impassable) || (HarmonyPatcher.DubsSkylights && t.GetType() == HarmonyPatcher.Building_Skylight))
            {
                var info = new MapUpdateWatcher.MapUpdateInfo()
                {
                    center = c,
                    removed = true,
                    map = t.Map
                };
                MapUpdateWatcher.OnMapUpdate(t, info);
            }
        }
    }
}
using HarmonyLib;
using Verse;

namespace OpenTheWindows
{
    //Triggers the illuminated area recalculation when thing is constructed/deconstructed.
    [HarmonyPatch(typeof(ThingGrid), "RegisterInCell")]
    public static class ThingGrid_Register
    {
        public static void Postfix(Thing t, IntVec3 c)
        {
            //Log.Message($"Scribe.mode: {Scribe.mode}, ProgramState: {Current.ProgramState}");
            //if (Scribe.mode == LoadSaveMode.LoadingVars) return;
            if (Current.ProgramState != ProgramState.Playing) return;
            if ((t is Building && t.def.passability == Traversability.Impassable) || (HarmonyPatcher.DubsSkylights && t.GetType() == HarmonyPatcher.Building_Skylight))
            {
                var info = new MapUpdateWatcher.MapUpdateInfo()
                {
                    origin = c,
                    removed = false,
                    map = t.Map
                };
                MapUpdateWatcher.OnMapUpdate(t, info);
            }
        }
    }

    [HarmonyPatch(typeof(ThingGrid), "DeregisterInCell")]
    public static class ThingGrid_Deregister
    {
        public static void Postfix(Thing t, IntVec3 c)
        {
            if ((t is Building && t.def.passability == Traversability.Impassable) || (HarmonyPatcher.DubsSkylights && t.GetType() == HarmonyPatcher.Building_Skylight))
            {
                var info = new MapUpdateWatcher.MapUpdateInfo()
                {
                    origin = c,
                    removed = true,
                    map = t.Map
                };
                MapUpdateWatcher.OnMapUpdate(t, info);
            }
        }
    }
}
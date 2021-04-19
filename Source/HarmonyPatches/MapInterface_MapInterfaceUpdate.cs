using HarmonyLib;
using RimWorld;
using Verse;

namespace OpenTheWindows
{
    [HarmonyPatch(typeof(MapInterface), nameof(MapInterface.MapInterfaceUpdate))]
    public static class MapInterface_MapInterfaceUpdate
    {
        public static void Postfix()
        {
            if (Find.CurrentMap == null || RimWorld.Planet.WorldRendererUtility.WorldRenderedNow) return;

            NaturalLightOverlay naturalLightMap = new NaturalLightOverlay();
            naturalLightMap.Update();
        }
    }
}
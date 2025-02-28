using HarmonyLib;
using OpenTheWindows;
using System;
using Verse;

namespace HumanResources
{
    public static class ReBuildDoorsAndCorners_Patches
    {
        private static Map curMap;
        private static MapComp_Windows curComp;

        public static void Execute(Harmony instance)
        {
            //Deferring to their roof skipping function so we can use it for our own needs.
            Type LightingOverlayRegeneratePatch = AccessTools.TypeByName("ReBuildDoorsAndCorners.SectionLayer_LightingOverlay_Regenerate_Patch");
            instance.Patch(AccessTools.Method(LightingOverlayRegeneratePatch, "TryInterceptRoof"),
                null, new HarmonyMethod(typeof(ReBuildDoorsAndCorners_Patches), nameof(TryInterceptRoofPlugin)), null);
            instance.Patch(AccessTools.Method(LightingOverlayRegeneratePatch, "TryInterceptRoofed"),
                null, new HarmonyMethod(typeof(ReBuildDoorsAndCorners_Patches), nameof(TryInterceptRoofedPlugin)), null);

            //Replacing their GroundGlowAt functino with our own, so our light transmission factor also applies to their glass walls.
            Type GroundGlowAtPatch = AccessTools.TypeByName("ReBuildDoorsAndCorners.GlowGrid_GroundGlowAt_Patch");
            var undo = GroundGlowAtPatch.GetMethod("Transpiler");
            var original = typeof(GlowGrid).GetMethod("GroundGlowAt");
            instance.Unpatch(original, undo);
            instance.CreateReversePatcher(GroundGlowAtPatch.GetMethod("HasNoNaturalLight", new[] { typeof(bool), typeof(IntVec3), typeof(GlowGrid) }), new HarmonyMethod(AccessTools.Method(typeof(ReBuildDoorsAndCorners_Patches), nameof(HasNoNaturalLightProxy)))).Patch();
        }

        public static RoofDef TryInterceptRoofPlugin(RoofDef roof, int index, Map ___curMap)
        {
            if (roof == null) return roof;
            return IsUnderWindow(___curMap, index) ? null : roof;
        }

        public static bool TryInterceptRoofedPlugin(bool roofed, int index, Map ___curMap)
        {
            if (!roofed) return roofed;
            return !IsUnderWindow(___curMap, index);
        }

        private static bool IsUnderWindow(Map map, int index)
        {
            MapComp_Windows actualComp;
            if (map != curMap)
            {
                curMap = map;
                curComp = actualComp = map.GetComponent<MapComp_Windows>();
            }
            else
            {
                if (curComp == null)
                {
                    MapComp_Windows cachedComp;
                    if (MapComp_Windows.MapCompsCache.TryGetValue(map.uniqueID, out cachedComp))
                    {
                        actualComp = cachedComp;
                    }
                    else
                    {
                        actualComp = map.GetComponent<MapComp_Windows>();
                    }
                }
                else actualComp = curComp;
            }
            return actualComp.WindowCells.Contains(index);
        }

        public static bool HasNoNaturalLightProxy(bool roofed, IntVec3 c, GlowGrid glowGrid)
        {
            throw new NotImplementedException("Expected ReBuild: Doors and Corners!");
        }

    }
}

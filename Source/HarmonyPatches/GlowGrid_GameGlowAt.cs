using HarmonyLib;
using System;
using Verse;
using static OpenTheWindows.LightingOverlay_Regenerate;

namespace OpenTheWindows
{
    //Illuminates the tiles that are under a window's influence area.
    [HarmonyPatch(typeof(GlowGrid), nameof(GlowGrid.GameGlowAt))]
    public static class GlowGrid_GameGlowAt
    {
        public static float Postfix(float __result, IntVec3 c)
        {
            if (windowComponent == null || !windowComponent.WindowCells.Contains(c)) return __result;
            
            float fromSky = map.skyManager.curSkyGlowInt * OpenTheWindowsSettings.LightTransmission;
            if (HarmonyPatcher.RaiseTheRoof && c.Roofed(map) && c.IsTransparentRoof(map))
            {
                __result *= OpenTheWindowsSettings.LightTransmission;
            }
            return __result > fromSky ? __result : fromSky;
        }
    }
}
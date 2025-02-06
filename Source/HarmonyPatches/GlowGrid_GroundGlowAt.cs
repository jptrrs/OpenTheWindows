using HarmonyLib;
using System;
using UnityEngine;
using Verse;

namespace OpenTheWindows
{
    //Illuminates the tiles that are under a window's influence area.
    [HarmonyPatch(typeof(GlowGrid), nameof(GlowGrid.GroundGlowAt))]
    public static class GlowGrid_GroundGlowAt
    {
        public static void Postfix(IntVec3 c, Map ___map, ref float __result)
        {
            //Map map = LightingOverlay_Regenerate.map;
            MapComp_Windows comp = LightingOverlay_Regenerate.windowsMapComponent;
            if (comp == null || !comp.IsUnderWindow(c)) return;
            try
            {
                float fromSky = ___map.skyManager.CurSkyGlow * OpenTheWindowsSettings.LightTransmission;
                if (HarmonyPatcher.RaiseTheRoof && c.Roofed(___map) && c.IsTransparentRoof(___map))
                {
                    __result *= OpenTheWindowsSettings.LightTransmission;
                }
                __result = Mathf.Max(__result, fromSky);
            }
            catch (Exception e) // if you are catching err's you might as well explain them.
            {
                Log.Warning("Error at GlowGrid_GroundGlowAt: " + e.Message);
            }
        }
    }
}
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
            if (__result < 1f && MapComp_Windows.MapCompsCache[___map.uniqueID].IsUnderWindow(c))
            {
                float fromSky = ___map.skyManager.CurSkyGlow * OpenTheWindowsSettings.LightTransmission;
                if (HarmonyPatcher.RaiseTheRoof && c.Roofed(___map) && c.IsTransparentRoof(___map))
                {
                    __result *= OpenTheWindowsSettings.LightTransmission;
                }
                __result = Mathf.Max(__result, fromSky);
            }
        }
    }
}
using HarmonyLib;
using System;
using UnityEngine;
using Verse;

namespace OpenTheWindows
{
    //Changed in RW 1.4
    //Illuminates the tiles that are under a window's influence area.
    [HarmonyPatch(typeof(GlowGrid), nameof(GlowGrid.GameGlowAt))]
    public static class GlowGrid_GameGlowAt
    {
        public static void Postfix(IntVec3 c, ref float __result)
        {
            Map map = LightingOverlay_Regenerate.map;
            MapComp_Windows comp = LightingOverlay_Regenerate.windowComponent;
            if (comp == null || !comp.WindowCells.Contains(c)) return;
            try
            {
                float fromSky = map.skyManager.CurSkyGlow * OpenTheWindowsSettings.LightTransmission;
                if (HarmonyPatcher.RaiseTheRoof && c.Roofed(map) && c.IsTransparentRoof(map))
                {
                    __result *= OpenTheWindowsSettings.LightTransmission;
                }
                __result = Mathf.Max(__result, fromSky);
            }
            catch (Exception e) // if you are catching err's you might as well explain them.
            {
                Log.Warning("Error at GlowGrid_GameGlowAt: " + e.Message);
            }
        }
    }
}
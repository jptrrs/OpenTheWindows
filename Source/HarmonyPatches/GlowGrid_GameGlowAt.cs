﻿using HarmonyLib;
using System;
using UnityEngine;
using Verse;

namespace OpenTheWindows
{
    //Illuminates the tiles that are under a window's influence area.
    [HarmonyPatch(typeof(GlowGrid), nameof(GlowGrid.GameGlowAt))]
    public static class GlowGrid_GameGlowAt
    {
        public static void Postfix(IntVec3 c, ref float __result)
        {
            // more caching, this method is called A LOT !
            Map map = LightingOverlay_Regenerate.map;
            MapComp_Windows comp = LightingOverlay_Regenerate.windowComponent;

            try
            {
                if (__result < 1f && comp.WindowCells.Contains(c))
                {
                    float x = Mathf.Max(0f, map.skyManager.CurSkyGlow * OpenTheWindowsSettings.LightTransmission);
                    __result = Mathf.Max(__result, x);
                }
            }
            catch (Exception e) // if you are catching err's you might as well explain them.
            {
                Log.Warning("Error at GlowGrid_GameGlowAt: " + e.Message);
            }
        }
    }
}
using HarmonyLib;
using System;
using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace OpenTheWindows
{
    //[HarmonyPatch(typeof(RegionTypeUtility), nameof(RegionTypeUtility.GetExpectedRegionType))]
    public static class RegionTypeUtility_GetExpectedRegionType
    {
        public static void Postfix(IntVec3 c, Map map, ref RegionType __result)
        {
            //Log.Message("DEBUG postfixing RegionTypeUtility_GetExpectedRegionType");
            if (__result == RegionType.ImpassableFreeAirExchange)
            {
                if ((c.GetEdifice(map) as Building_Window) != null)
                {
                    __result = RegionType.Portal;
                }
            }
        }
    }
}
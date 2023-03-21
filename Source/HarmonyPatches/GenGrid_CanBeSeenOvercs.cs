using HarmonyLib;
using RimWorld;
using System;
using Verse;

namespace OpenTheWindows
{
    //Changes the visibility through the windows when they are open.
    [HarmonyPatch(typeof(GenGrid), nameof(GenGrid.CanBeSeenOver), new Type[] { typeof(Building) })]
    public static class GenGrid_CanBeSeenOver
    {
        public static bool Postfix(bool __result, Building b)
        {
            return (b is Building_Window && FlickUtility.WantsToBeOn(b)) ? true : __result;
        }
    }
}
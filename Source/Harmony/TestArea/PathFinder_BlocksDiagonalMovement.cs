using HarmonyLib;
using RimWorld;
using System;
using Verse;
using Verse.AI;

namespace OpenTheWindows
{
    //[HarmonyPatch(typeof(PathFinder), nameof(PathFinder.BlocksDiagonalMovement), new Type[] { typeof(int), typeof(Map) })]
    public static class PathFinder_BlocksDiagonalMovement
    {
        public static bool Prefix(int index, Map map, ref bool __result)
        {
            __result = !map.pathGrid.WalkableFast(index) || map.edificeGrid[index] is Building_Door || map.edificeGrid[index] is Building_Window;
            return false;
        }
    }
}
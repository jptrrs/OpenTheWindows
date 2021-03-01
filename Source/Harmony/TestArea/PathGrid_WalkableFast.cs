using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using Verse;
using Verse.AI;

namespace OpenTheWindows
{
    //[HarmonyPatch(typeof(PathGrid), nameof(PathGrid.WalkableFast), new Type[] { typeof(int) })]
    public static class PathGrid_WalkableFast
    {
        static FieldInfo edificeGridInfo = AccessTools.Field(typeof(PathFinder), "edificeGrid");

        public static void Postfix(object __instance, int index, ref bool __result)
        {
            if (__result && PathFinder_FindPath.CurPathFinder != null)
            {
                Building[] edificeGrid = (Building[])edificeGridInfo.GetValue(PathFinder_FindPath.CurPathFinder);
                if (index <= edificeGrid.Length && edificeGrid[index] != null)
                {
                    Building_Window window = edificeGrid[index] as Building_Window;
                    string test = window != null ? "ok" : "bad";
                    Log.Message("DEBUG WalkableFast triggered with window = " + test + " building is " + edificeGrid[index].def);
                    //if (window != null)
                    //{
                    //    Log.Message("DEBUG window triggered WalkableFast limit at " + window.Position + ", CurTraverseParms: mode = " + PathFinder_FindPath.CurTraverseParms.mode + ", canBash = "+PathFinder_FindPath.CurTraverseParms.canBash);
                    //}
                }
            }
        }
    }
}

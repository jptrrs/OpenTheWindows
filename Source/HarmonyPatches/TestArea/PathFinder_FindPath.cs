using HarmonyLib;
using System;
using Verse;
using Verse.AI;

namespace OpenTheWindows
{
    //[HarmonyPatch(typeof(PathFinder), nameof(PathFinder.FindPath), new Type[] { typeof(IntVec3), typeof(LocalTargetInfo), typeof(TraverseParms), typeof(PathEndMode) })]
    public static class PathFinder_FindPath
    {
        public static PathFinder CurPathFinder = null;
        public static TraverseParms CurTraverseParms = new TraverseParms();

        public static void Prefix(PathFinder __instance, TraverseParms traverseParms)
        { 
            CurPathFinder = __instance;
            CurTraverseParms = traverseParms;
        }

        public static void Postfix()
        {
            CurPathFinder = null;
        }
    }
}
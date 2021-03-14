using HarmonyLib;
using System;
using Verse;
using Verse.AI;

namespace OpenTheWindows
{
    //[HarmonyPatch(typeof(PathFinder), nameof(PathFinder.PfProfilerBeginSample))]
    public static class PathFinder_PfProfilerBeginSample
    {
        public static string sample = "";

        public static void Prefix(string s, FastPriorityQueue<PathFinder.CostNode> ___openList)
        {
            //Log.Message("Profiler Sample: " + s + ", openList count = " + ___openList.Count);
            sample = s;
        }
    }

    //[HarmonyPatch(typeof(PathFinder), nameof(PathFinder.PfProfilerEndSample))]
    public static class PathFinder_PfProfilerEndSample
    {
        public static void Prefix(FastPriorityQueue<PathFinder.CostNode> ___openList)
        {
            if (___openList.Count <= 0)
            Log.Message("Profiler ended at sample " + PathFinder_PfProfilerBeginSample.sample);
            PathFinder_PfProfilerBeginSample.sample = "";
        }
    }

}
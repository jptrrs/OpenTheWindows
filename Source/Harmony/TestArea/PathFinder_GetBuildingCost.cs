using HarmonyLib;
using Verse;
using Verse.AI;

namespace OpenTheWindows
{
    [HarmonyPatch(typeof(PathFinder), nameof(PathFinder.GetBuildingCost))]
    public static class PathFinder_GetBuildingCost
    {
        public static bool Prefix(Building b, TraverseParms traverseParms, ref int __result)
        {
            Building_Window window = b as Building_Window;
            if (window != null)
            {
                switch (traverseParms.mode)
                {
                    case TraverseMode.ByPawn:
                        __result = traverseParms.canBash ? 300 : int.MaxValue;
                        break;
                    case TraverseMode.PassDoors:
                    case TraverseMode.NoPassClosedDoors:
                    case TraverseMode.NoPassClosedDoorsOrWater:
                        __result = int.MaxValue;
                        break;
                    case TraverseMode.PassAllDestroyableThings:
                    case TraverseMode.PassAllDestroyableThingsNotWater:
                        __result = 50 + (int)(window.HitPoints * 0.2f);
                        break;
                }
                Log.Message("DEBUG PathFinder_GetBuildingCost changed result to " + __result.ToString());
                return false;
            }
            return true;
        }
    }
}
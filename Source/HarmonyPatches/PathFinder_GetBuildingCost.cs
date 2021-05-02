using HarmonyLib;
using RimWorld;
using Verse;
using Verse.AI;

namespace OpenTheWindows
{
    //Tweaks the windows building cost for pathfinding reasons. 1/2
    [HarmonyPatch(typeof(PathFinder), nameof(PathFinder.GetBuildingCost))]
    public static class PathFinder_GetBuildingCost
    {
        public static void Postfix(Building b, TraverseParms traverseParms, Pawn pawn, ref int __result)
        {
            Building_Window window = b as Building_Window;
            if (window != null)
            {
                switch (traverseParms.mode)
                {
                    case TraverseMode.ByPawn:
                    case TraverseMode.PassDoors:
                        if (traverseParms.canBash) __result = 300;
                        else if (pawn.CurJob.attackDoorIfTargetLost)
                        {
                            __result = 100 + (int)(window.HitPoints * 0.2f);
                        }
                        else
                        {
                            __result = int.MaxValue;
                        }
                        break;
                    case TraverseMode.NoPassClosedDoors:
                    case TraverseMode.NoPassClosedDoorsOrWater:
                        __result = int.MaxValue;
                        break;
                    case TraverseMode.PassAllDestroyableThings:
                    case TraverseMode.PassAllDestroyableThingsNotWater:
                        __result = 50 + (int)(window.HitPoints * 0.2f);
                        break;
                }
            }
        }
    }
}
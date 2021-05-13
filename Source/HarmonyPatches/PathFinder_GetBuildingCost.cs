using HarmonyLib;
using Verse;
using Verse.AI;

namespace OpenTheWindows
{
    //Tweaks the windows building cost for pathfinding reasons. 1/2
    [HarmonyPatch(typeof(PathFinder), nameof(PathFinder.GetBuildingCost))]
    public static class PathFinder_GetBuildingCost
    {
        public static int Postfix(int result, Building b, TraverseParms traverseParms, Pawn pawn)
        {
            Building_Window window = b as Building_Window;
            if (window == null) return result;
            switch (traverseParms.mode)
            {
                case TraverseMode.ByPawn:
                case TraverseMode.PassDoors:
                    if (traverseParms.canBash) return 300;
                    if (pawn.CurJob?.attackDoorIfTargetLost == true || pawn.MentalState is MentalState_Manhunter)
                    {
                        return 100 + (int)(window.HitPoints * 0.2f);
                    }
                    return int.MaxValue;
                case TraverseMode.NoPassClosedDoors:
                case TraverseMode.NoPassClosedDoorsOrWater:
                    return int.MaxValue;
                case TraverseMode.PassAllDestroyableThings:
                case TraverseMode.PassAllDestroyableThingsNotWater:
                    return 50 + (int)(window.HitPoints * 0.2f);
                default:
                     return result;
            }
        }
    }
}
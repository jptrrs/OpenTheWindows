using HarmonyLib;
using Verse;
using Verse.AI;

namespace OpenTheWindows
{
    //Tweaks the windows building cost for pathfinding reasons. 1/2
    //fetched from Owlchemist's
    [HarmonyPatch(typeof(PathFinder), nameof(PathFinder.GetBuildingCost))]
    public static class PathFinder_GetBuildingCost
    {
        public static int Postfix(int result, Building b, TraverseParms traverseParms, Pawn pawn)
        {
            if (!(b is Building_Window)) return result;
            switch (traverseParms.mode)
            {
                case TraverseMode.ByPawn:
                case TraverseMode.PassDoors:
                    if (traverseParms.canBashDoors) return 300;
                    if (pawn != null && (pawn.CurJob?.attackDoorIfTargetLost == true || (pawn.def.race.intelligence < Intelligence.Humanlike && pawn.MentalState is MentalState_Manhunter)))
                    {
                        return 100 + (int)(b.HitPoints * 0.2f);
                    }
                    return int.MaxValue;
                case TraverseMode.NoPassClosedDoors:
                case TraverseMode.NoPassClosedDoorsOrWater:
                    return int.MaxValue;
                case TraverseMode.PassAllDestroyableThings:
                case TraverseMode.PassAllDestroyableThingsNotWater:
                    return 50 + (int)(b.HitPoints * 0.2f);
                default:
                     return result;
            }
        }
    }
}
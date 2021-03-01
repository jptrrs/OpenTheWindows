using HarmonyLib;
using RimWorld;
using Verse;
using Verse.AI;

namespace OpenTheWindows
{
    //[HarmonyPatch(typeof(PathFinder), nameof(PathFinder.GetBuildingCost))]
    public static class PathFinder_GetBuildingCost
    {
        public static void Postfix(Building b, TraverseParms traverseParms, Pawn pawn, ref int __result)
        {
            //Log.Message("DEBUG GetBuildingCost for " + pawn+" performing " +pawn.CurJob+ " at building "+b+", traverseParms: mode = " + traverseParms.mode + ", caBash = "+traverseParms.canBash);
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
                //Log.Message("DEBUG GetBuildingCost postfix for " + pawn + ", on mode " + traverseParms.mode + ", changed result to " + __result.ToString());
            }

            //testing
            if (b != null && (b as Building_Door != null || b as Building_Window != null))
            {
                Log.Message("DEBUG PathFinder.GetBuildingCost: building cost for " + b + ": " + __result);
            }

        }

        //public static bool Prefix(Building b, TraverseParms traverseParms, ref int __result)
        //{
        //    Log.Message("DEBUG prefixing Pathfinder, traverseParms.mode = " + traverseParms.mode);
        //    if (traverseParms.canBash)
        //    {
        //        Building_Window window = b as Building_Window;
        //        if (window != null)
        //        {
        //            switch (traverseParms.mode)
        //            {
        //                case TraverseMode.ByPawn:
        //                __result = 300;
        //                break;
        //                case TraverseMode.PassDoors:
        //                case TraverseMode.NoPassClosedDoors:
        //                case TraverseMode.NoPassClosedDoorsOrWater:
        //                    __result = int.MaxValue;
        //                    break;
        //                case TraverseMode.PassAllDestroyableThings:
        //                case TraverseMode.PassAllDestroyableThingsNotWater:
        //                    __result = 50 + (int)(window.HitPoints * 0.2f);
        //                    break;
        //            }
        //            Log.Message("DEBUG PathFinder_GetBuildingCost changed result to " + __result.ToString());
        //            return false;
        //        }
        //    }
        //    return true;
        //}
    }
}
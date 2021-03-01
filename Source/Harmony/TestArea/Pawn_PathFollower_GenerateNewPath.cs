using HarmonyLib;
using Verse;
using Verse.AI;

namespace OpenTheWindows
{
    //[HarmonyPatch(typeof(Pawn_PathFollower), nameof(Pawn_PathFollower.GenerateNewPath))]
    public static class Pawn_PathFollower_GenerateNewPath
    {
        public static bool Prefix(object b, TraverseParms traverseParms, ref int __result)
        {
            if (traverseParms.canBash)
            {
                Building_Window window = b as Building_Window;
                if (window != null)
                {
                    switch (traverseParms.mode)
                    {
                        case TraverseMode.ByPawn:
                            __result = 300;
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
            }
            return true;
        }
    }
}
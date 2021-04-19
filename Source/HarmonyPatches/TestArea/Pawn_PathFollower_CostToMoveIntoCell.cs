using HarmonyLib;
using System;
using Verse;
using Verse.AI;

namespace OpenTheWindows
{
    //[HarmonyPatch(typeof(Pawn_PathFollower), nameof(Pawn_PathFollower.CostToMoveIntoCell), new Type[] { typeof(Pawn), typeof(IntVec3) })]
    public static class Pawn_PathFollower_CostToMoveIntoCell
    {
        public static void Postfix(Pawn pawn, IntVec3 c, int __result)  
        {
            if (c.GetEdifice(pawn.Map) is Building_Window window)
            {
                Log.Message($"DEBUG CostToMoveIntoCell at {window} for {pawn} was {__result}");
            }
        }
    }
}
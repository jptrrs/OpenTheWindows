using HarmonyLib;
using System;
using Verse;
using Verse.AI;

namespace OpenTheWindows
{
    //[HarmonyPatch(typeof(TouchPathEndModeUtility), nameof(TouchPathEndModeUtility.MakesOccupiedCellsAlwaysReachableDiagonally))]
    public static class TouchPathEndModeUtility_MakesOccupiedCellsAlwaysReachableDiagonally
    {
        public static void Postfix(ThingDef def, ref bool __result)
        {
            if (def.thingClass == typeof(Building_Window)) Log.Message("DEBUG MakesOccupiedCellsAlwaysReachableDiagonally for window = "+__result);
        }
    }

}
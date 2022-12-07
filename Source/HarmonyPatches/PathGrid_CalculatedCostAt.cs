using HarmonyLib;
using RimWorld;
using System.Collections.Generic;
using Verse;
using Verse.AI;

namespace OpenTheWindows
{
    //Tweaks the windows building cost for pathfinding reasons. 2/2
    [HarmonyPatch(typeof(PathGrid), nameof(PathGrid.CalculatedCostAt))]
    public static class PathGrid_CalculatedCostAt
    {
        public static int Postfix(int __result, PathGrid __instance, IntVec3 c, bool perceivedStatic, IntVec3 prevCell)
        {
            if (__result == 10000 && c.GetEdifice(__instance.map)?.def.thingClass == typeof(Building_Window))
            {
                int cost = 10;
                if (perceivedStatic)
                {
                    for (int j = 0; j < 9; j++)
                    {
                        IntVec3 intVec = GenAdj.AdjacentCellsAndInside[j];
                        IntVec3 c2 = c + intVec;
                        if (c2.InBounds(__instance.map))
                        {
                            List<Thing> list = __instance.map.thingGrid.ThingsListAtFast(c2);
                            var length = list.Count;
                            for (int k = 0; k < length; k++)
                            {
                                var thing = list[k];
                                if (thing.def.thingClass == typeof(Fire))
                                {
                                    if (((Fire)thing).parent == null)
                                    {
                                        return (intVec.x == 0 && intVec.z == 0) ? cost += 1000 : cost += 150;
                                    }
                                } 
                            }
                        }
                    }
                }
                return cost;
            }
            return __result;
        }
    }
}

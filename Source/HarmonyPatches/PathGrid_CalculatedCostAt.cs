using HarmonyLib;
using RimWorld;
using System.Collections.Generic;
using Verse;
using Verse.AI;

namespace OpenTheWindows
{
    //Tweaks the windows building cost for pathfinding reasons. 2/2
    //fetched from Owlchemist's
    [HarmonyPatch(typeof(PathGrid), nameof(PathGrid.CalculatedCostAt))]
    public static class PathGrid_CalculatedCostAt
    {
        public static int Postfix(int __result, PathGrid __instance, IntVec3 c, bool perceivedStatic, IntVec3 prevCell)
        {
            Map map = __instance.map;
            if (__result == 10000 && c.GetEdifice(map) is Building_Window)
            {
                int cost = 10;
                if (perceivedStatic)
                {
                    var thingGrid = map.thingGrid;
                    for (int j = 0; j < 9; j++)
                    {
                        IntVec3 intVec = GenAdj.AdjacentCellsAndInside[j];
                        IntVec3 c2 = c + intVec;
                        if (c2.InBounds(map))
                        {
                            List<Thing> list = thingGrid.ThingsListAtFast(c2);
                            for (int k = list.Count; k-- > 0;)
                            {
                                if (list[k] is Fire fire && fire.parent == null)
                                {
                                    return (intVec.x == 0 && intVec.z == 0) ? cost += 1000 : cost += 150;
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
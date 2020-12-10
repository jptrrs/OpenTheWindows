using HarmonyLib;
using RimWorld;
using System.Collections.Generic;
using System.Reflection;
using Verse;
using Verse.AI;

namespace OpenTheWindows
{
    [HarmonyPatch(typeof(PathGrid), nameof(PathGrid.CalculatedCostAt))]
    public static class PathGrid_CalculatedCostAt
    {
        static MethodInfo IsPathCostIgnoreRepeaterInfo = AccessTools.Method(typeof(PathGrid), "IsPathCostIgnoreRepeater");
        static MethodInfo ContainsPathCostIgnoreRepeaterInfo = AccessTools.Method(typeof(PathGrid), "ContainsPathCostIgnoreRepeater");

        public static void Postfix(PathGrid __instance, IntVec3 c, bool perceivedStatic, IntVec3 prevCell, Map ___map, ref int __result)
        {
            if (__result == 10000 && c.GetEdifice(___map) as Building_Window != null)
            {
                TerrainDef terrainDef = ___map.terrainGrid.TerrainAt(c);
                int cost = terrainDef.pathCost;
                List<Thing> list = ___map.thingGrid.ThingsListAt(c);
                for (int i = 0; i < list.Count; i++)
                {
                    Thing thing = list[i];
                    if (!(bool)IsPathCostIgnoreRepeaterInfo.Invoke(__instance, new object[] { thing.def }) || 
                        !prevCell.IsValid || 
                        !(bool)ContainsPathCostIgnoreRepeaterInfo.Invoke(__instance, new object[] { prevCell }))
                    {
                        int pathCost = thing.def.pathCost;
                        if (pathCost > cost)
                        {
                            cost = pathCost;
                        }
                    }
                }
                int snowCost = SnowUtility.MovementTicksAddOn(___map.snowGrid.GetCategory(c));
                if (snowCost > cost)
                {
                    cost = snowCost;
                }
                if (prevCell.IsValid)
                {
                    cost += 45;
                }
                if (perceivedStatic)
                {
                    for (int j = 0; j < 9; j++)
                    {
                        IntVec3 intVec = GenAdj.AdjacentCellsAndInside[j];
                        IntVec3 c2 = c + intVec;
                        if (c2.InBounds(___map))
                        {
                            Fire fire = null;
                            list = ___map.thingGrid.ThingsListAtFast(c2);
                            for (int k = 0; k < list.Count; k++)
                            {
                                fire = (list[k] as Fire);
                                if (fire != null)
                                {
                                    break;
                                }
                            }
                            if (fire != null && fire.parent == null)
                            {
                                if (intVec.x == 0 && intVec.z == 0)
                                {
                                    cost += 1000;
                                }
                                else
                                {
                                    cost += 150;
                                }
                            }
                        }
                    }
                }
                __result = cost;
                Log.Message("DEBUG Calculated cost for window at " + c + ": " + __result);
            }
        }
    }
}

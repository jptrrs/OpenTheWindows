using HarmonyLib;
using RimWorld;
using System.Collections.Generic;
using System.Reflection;
using Verse;
using Verse.AI;
using System.Diagnostics;

namespace OpenTheWindows
{
    //[HarmonyPatch(typeof(PathGrid), nameof(PathGrid.CalculatedCostAt))]
    public static class PathGrid_CalculatedCostAt
    {
        //public static bool Prefix(IntVec3 c, bool perceivedStatic, Map ___map, ref int __result)
        //{
        //    if (perceivedStatic)
        //    {
        //        Building_Window window = c.GetEdifice(___map) as Building_Window;
        //        if (window != null)
        //        {
        //            __result = 10000;
        //            return false;
        //        }
        //    }
        //    return true;
        //}

        static MethodInfo IsPathCostIgnoreRepeaterInfo = AccessTools.Method(typeof(PathGrid), "IsPathCostIgnoreRepeater");
        static MethodInfo ContainsPathCostIgnoreRepeaterInfo = AccessTools.Method(typeof(PathGrid), "ContainsPathCostIgnoreRepeater");
        //static MethodInfo RecalculatePerceivedPathCostAtInfo = AccessTools.Method(typeof(PathGrid), "RecalculatePerceivedPathCostAt");

        public static void Postfix(PathGrid __instance, IntVec3 c, bool perceivedStatic, IntVec3 prevCell, Map ___map, ref int __result)
        {

            if (perceivedStatic && __result == 10000)
            {
                Building_Window window = c.GetEdifice(___map) as Building_Window;
                if (window != null)
                {
                    int cost = 50;
                    for (int j = 0; j < 9; j++)
                    {
                        IntVec3 intVec = GenAdj.AdjacentCellsAndInside[j];
                        IntVec3 c2 = c + intVec;
                        if (c2.InBounds(___map))
                        {
                            Fire fire = null;
                            List<Thing> list = ___map.thingGrid.ThingsListAtFast(c2);
                            for (int k = 0; k < list.Count; k++)
                            {
                                fire = (list[k] as Fire);
                                if (fire != null) break;
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
                    __result = cost;
                    //Log.Message("DEBUG Calculated cost for WINDOW at " + c + ": " + __result);
                }
            }

            //testing
            Building b = c.GetEdifice(___map);
            if (b != null && (b as Building_Door != null || b as Building_Window != null))
            {
                Log.Message("DEBUG PathGrid.CalculatedCostAt: calculated cost for " + b+": " + __result);
            }
        }
    }
}

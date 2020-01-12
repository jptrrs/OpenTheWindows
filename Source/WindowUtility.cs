using RimWorld;
using System;
using System.Collections.Generic;
using Verse;

namespace OpenTheWindows
{
    public class WindowUtility
    {
        public static IEnumerable<IntVec3> CalculateWindowLightCells(ThingDef def, IntVec3 center, Rot4 rot, Map map)
        {
            foreach (IntVec3 c in GetWindowObfuscation(center, rot, def.size, map))
            {
                if (DoesLightReach(c, center, map))
                {
                    yield return c;
                }
            }
            yield break;
        }

        //public static IEnumerable<IntVec3> GetWindowLightAreas(IntVec3 center, Rot4 rot, IntVec2 size, Map map)
        //{
        //    CellRect rectA = default(CellRect);
        //    CellRect rectB = default(CellRect);
        //    int reach = Math.Max(size.x, size.z) / 2 + 1;
        //    int deep = 2;
        //    int maxReachA = 0;
        //    int maxReachB = 0;
        //    int maxReach = 0;

        //    //find light limits
        //    if (rot.IsHorizontal)
        //    {
        //        //test if window normals are clear
        //        for (int i = 1; i < 1 + reach + deep; i++)
        //        {
        //            IntVec3 c = new IntVec3(center.x + i, 0, center.z);
        //            if (c.Walkable(map) & !map.roofGrid.Roofed(c)) maxReachA++;
        //            else break;
        //        }
        //        for (int i = 1; i < 1 + reach + deep; i++)
        //        {
        //            IntVec3 c = new IntVec3(center.x - i, 0, center.z);
        //            if (c.Walkable(map) & !map.roofGrid.Roofed(c)) maxReachB++;
        //            else break;
        //        }
        //        maxReach = Math.Max(maxReachA, maxReachB);

        //        //sets limits acordingly
        //        rectA.minX = center.x + 1;
        //        rectA.maxX = center.x + maxReach;
        //        rectB.minX = center.x - maxReach;
        //        rectB.maxX = center.x - 1;
        //        rectB.minZ = (rectA.minZ = center.z - reach);
        //        rectB.maxZ = (rectA.maxZ = center.z + reach);
        //    }
        //    else
        //    {
        //        //test if window normals are clear
        //        for (int i = 1; i < 1 + reach + deep; i++)
        //        {
        //            IntVec3 c = new IntVec3(center.x, 0, center.z + i);
        //            if (c.Walkable(map) & !map.roofGrid.Roofed(c)) maxReachA++;
        //            else break;
        //        }
        //        for (int i = 1; i < 1 + reach + deep; i++)
        //        {
        //            IntVec3 c = new IntVec3(center.x, 0, center.z - i);
        //            if (c.Walkable(map) & !map.roofGrid.Roofed(c)) maxReachB++;
        //            else break;
        //        }
        //        maxReach = Math.Max(maxReachA, maxReachB);

        //        //sets limits acordingly
        //        rectA.minZ = center.z + 1;
        //        rectA.maxZ = center.z + maxReach;
        //        rectB.minZ = center.z - maxReach;
        //        rectB.maxZ = center.z - 1;
        //        rectB.minX = (rectA.minX = center.x - reach);
        //        rectB.maxX = (rectA.maxX = center.x + reach);
        //    }

        //    //draws window light areas
        //    for (int z = rectA.minZ; z <= rectA.maxZ; z++)
        //    {
        //        for (int x = rectA.minX; x <= rectA.maxX; x++)
        //        {
        //            yield return new IntVec3(x, 0, z);
        //        }
        //    }
        //    for (int z2 = rectB.minZ; z2 <= rectB.maxZ; z2++)
        //    {
        //        for (int x2 = rectB.minX; x2 <= rectB.maxX; x2++)
        //        {
        //            yield return new IntVec3(x2, 0, z2);
        //        }
        //    }
        //    yield break;
        //}

        private static bool DoesLightReach(IntVec3 watchCell, IntVec3 buildingCenter, Map map)
        {
            return (watchCell.Walkable(map) && GenSight.LineOfSightToEdges(buildingCenter, watchCell, map, true, null));
        }

        private static IEnumerable<IntVec3> GetWindowObfuscation(IntVec3 center, Rot4 rot, IntVec2 size, Map map)
        {
            //base vars
            List<IntVec3> area = new List<IntVec3>();
            int reach = Math.Max(size.x, size.z) / 2 + 1;
            int deep = 2;
            IntVec3 nextOnTop = new IntVec3(0, 0, +1);
            IntVec3 nextOnBottom = new IntVec3(0, 0, -1);
            IntVec3 nextOnRight = new IntVec3(+1, 0, 0);
            IntVec3 nextOnLeft = new IntVec3(-1, 0, 0);

            //identify extremities
            IntVec3 start = new IntVec3(0, 0, 0);
            IntVec3 end = new IntVec3(0, 0, 0);
            bool large = Math.Max(size.x, size.z) > 1;
            if (large)
            {
                LinkDirections dirA;
                LinkDirections dirB;
                IntVec3 centerAdjustB;
                IntVec3 centerAdjustA;
                if (rot.IsHorizontal)
                {
                    dirA = LinkDirections.Up;
                    dirB = LinkDirections.Down;
                    centerAdjustA = new IntVec3(1, 0, -1);
                    centerAdjustB = new IntVec3(1, 0, +1);
                }
                else
                {
                    dirA = LinkDirections.Right;
                    dirB = LinkDirections.Left;
                    centerAdjustA = new IntVec3(-1, 0, 1);
                    centerAdjustB = new IntVec3(+1, 0, 1);
                }
                start = GenAdj.CellsAdjacentAlongEdge(center + centerAdjustA, rot, size, dirA).FirstOrFallback();
                end = GenAdj.CellsAdjacentAlongEdge(center + centerAdjustB, rot, size, dirB).FirstOrFallback();
            }

            //front and back
            if (rot.IsHorizontal)
            {
                foreach (IntVec3 c in GenAdj.OccupiedRect(center, rot, size))
                {
                    int maxReachA = 0;
                    int maxReachB = 0;
                    int cellx = c.x;
                    int cellz = c.z;

                    //find reach
                    for (int i = 1; i < 1 + reach + deep; i++)
                    {
                        IntVec3 target = new IntVec3(cellx + i, 0, cellz);
                        if (target.Walkable(map) & !map.roofGrid.Roofed(target)) maxReachA++;
                        else break;
                    }
                    for (int i = 1; i < 1 + reach + deep; i++)
                    {
                        IntVec3 target = new IntVec3(Math.Max(0, cellx - i), 0, cellz);
                        if (target.Walkable(map) & !map.roofGrid.Roofed(target)) maxReachB++;
                        else break;
                    }
                    int maxReach = Math.Max(maxReachA, maxReachB);

                    //register affected cells
                    for (int i = 1; i <= maxReach; i++)
                    {
                        area.Add(new IntVec3(cellx + i, 0, cellz));
                        area.Add(new IntVec3(cellx - i, 0, cellz));
                    }

                    //add borders if on extremity
                    if (!large || c + nextOnTop == start)
                    {
                        //for (int f = 1; f <= reach; f++)
                        //{
                            for (int i = 1; i <= maxReach; i++)
                            {
                                area.Add(new IntVec3(cellx + i, 0, cellz + 1));
                                area.Add(new IntVec3(cellx - i, 0, cellz + 1));
                            }
                        //}
                    }
                    if (!large || c + nextOnBottom == end)
                    {
                        //for (int f = 1; f <= reach; f++)
                        //{
                            for (int i = 1; i <= maxReach; i++)
                            {
                                area.Add(new IntVec3(cellx + i, 0, cellz - 1));
                                area.Add(new IntVec3(cellx - i, 0, cellz - 1));
                            }
                        //}
                    }

                    //clean cell itself
                    area.Remove(c);
                }
            }
            else
            {
                foreach (IntVec3 c in GenAdj.OccupiedRect(center, rot, size))
                {
                    int maxReachA = 0;
                    int maxReachB = 0;
                    int cellx = c.x;
                    int cellz = c.z;

                    //find reach
                    for (int i = 1; i <= reach + deep; i++)
                    {
                        IntVec3 target = new IntVec3(cellx, 0, cellz + i);
                        if (target.Walkable(map) & !map.roofGrid.Roofed(target)) maxReachA++;
                        else break;
                    }
                    for (int i = 1; i <= reach + deep; i++)
                    {
                        IntVec3 target = new IntVec3(cellx, 0, Math.Max(0, cellz - i));
                        if (target.Walkable(map) & !map.roofGrid.Roofed(target)) maxReachB++;
                        else break;
                    }
                    int maxReach = Math.Max(maxReachA, maxReachB);

                    //register affected cells
                    for (int i = 1; i <= maxReach; i++)
                    {
                        area.Add(new IntVec3(cellx, 0, cellz + i));
                        area.Add(new IntVec3(cellx, 0, cellz - i));
                    }

                    //add borders if on extremity
                    if (!large || c + nextOnRight == start)
                    {
                        //for (int f = 1; f <= reach; f++)
                        //{
                            for (int i = 1; i <= maxReach; i++)
                            {
                                area.Add(new IntVec3(cellx + 1, 0, cellz + i));
                                area.Add(new IntVec3(cellx + 1, 0, cellz - i));
                            //}
                        }
                    }
                    if (!large || c + nextOnLeft == end)
                    {
                        //for (int f = 1; f <= reach; f++)
                        //{
                            for (int i = 1; i <= maxReach; i++)
                            {
                                area.Add(new IntVec3(cellx - 1, 0, cellz + i));
                                area.Add(new IntVec3(cellx - 1, 0, cellz - i));
                            }
                        //}
                    }

                    //clean cell itself
                    area.Remove(c);
                }
            }
            return area;
        }

        public static void FindWindowExternalFacing(Building_Window window)
        {
            List<IntVec3> openSideA = new List<IntVec3>();
            List<IntVec3> openSideB = new List<IntVec3>();
            LinkDirections dirA;
            LinkDirections dirB;
            IntVec3 centerAdjustB;
            IntVec3 centerAdjustA;
            if (window.Rotation.IsHorizontal)
            {
                dirA = LinkDirections.Right;
                dirB = LinkDirections.Left;
                centerAdjustA = new IntVec3(-1, 0, 0);
                centerAdjustB = new IntVec3(+1, 0, 0);
            }
            else
            {
                dirA = LinkDirections.Up;
                dirB = LinkDirections.Down;
                centerAdjustA = new IntVec3(0, 0, -1);
                centerAdjustB = new IntVec3(0, 0, +1);
            }
            foreach (IntVec3 c in GenAdj.CellsAdjacentAlongEdge(window.Position + centerAdjustA, window.Rotation, window.def.size, dirA))
            {
                if (window.illuminated.Contains(c) && !window.Map.roofGrid.Roofed(c))
                {
                    openSideA.Add(c);
                }
            }
            foreach (IntVec3 c in GenAdj.CellsAdjacentAlongEdge(window.Position + centerAdjustB, window.Rotation, window.def.size, dirB))
            {
                if (window.illuminated.Contains(c) && !window.Map.roofGrid.Roofed(c))
                {
                    openSideB.Add(c);
                }
            }
            if (openSideA.Count != openSideB.Count)
            {
                window.Facing = (openSideA.Count > openSideB.Count) ? dirA : dirB;
                window.isFacingSet = true;
            }
            else
            {
                window.isFacingSet = false;
            }
        }

        //public static Rot4 WindowRotationAt(IntVec3 loc, Map map)
        //{
        //    int num = 0;
        //    int num2 = 0;
        //    num += AlignQualityAgainst(loc + IntVec3.East, map);
        //    num += AlignQualityAgainst(loc + IntVec3.West, map);
        //    num2 += AlignQualityAgainst(loc + IntVec3.North, map);
        //    num2 += AlignQualityAgainst(loc + IntVec3.South, map);
        //    if (num >= num2)
        //    {
        //        return Rot4.North;
        //    }
        //    return Rot4.East;
        //}

        //private static int AlignQualityAgainst(IntVec3 c, Map map)
        //{
        //    if (!c.InBounds(map))
        //    {
        //        return 0;
        //    }
        //    if (!c.Walkable(map))
        //    {
        //        return 9;
        //    }
        //    List<Thing> thingList = c.GetThingList(map);
        //    for (int i = 0; i < thingList.Count; i++)
        //    {
        //        Thing thing = thingList[i];
        //        if (typeof(Building_Window).IsAssignableFrom(thing.def.thingClass))
        //        {
        //            return 1;
        //        }
        //        Thing thing2 = thing as Blueprint;
        //        if (thing2 != null)
        //        {
        //            if (thing2.def.entityDefToBuild.passability == Traversability.Impassable)
        //            {
        //                return 9;
        //            }
        //            if (typeof(Building_Window).IsAssignableFrom(thing.def.thingClass))
        //            {
        //                return 1;
        //            }
        //        }
        //    }
        //    return 0;
        //}
    }
}
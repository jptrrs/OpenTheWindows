using System;
using System.Collections.Generic;
using Verse;

namespace OpenTheWindows
{
    public class WindowUtility
    {
        public const int deep = 2;
        
        public static IEnumerable<IntVec3> CalculateWindowLightCells(Building_Window window)
        {
            return CalculateWindowLightCells(window.def.size, window.Position, window.Rotation, window.Map, window.start, window.end);
        }

        public static IEnumerable<IntVec3> CalculateWindowLightCells(IntVec2 size, IntVec3 center, Rot4 rot, Map map, IntVec3 start, IntVec3 end)
        {
            foreach (IntVec3 c in GetWindowObfuscation(center, rot, size, map, start, end))
            {
                if (DoesLightReach(c, center, map))
                {
                    yield return c;
                }
            }
            yield break;
        }

        private static bool DoesLightReach(IntVec3 watchCell, IntVec3 buildingCenter, Map map)
        {
            return (watchCell.Walkable(map) && GenSight.LineOfSightToEdges(buildingCenter, watchCell, map, true, null));
        }

        private static IEnumerable<IntVec3> GetWindowObfuscation(IntVec3 center, Rot4 rot, IntVec2 size, Map map, IntVec3 start, IntVec3 end)
        {
            //base vars
            List<IntVec3> area = new List<IntVec3>();
            int reach = Math.Max(size.x, size.z) / 2 + 1;
            bool large = Math.Max(size.x, size.z) > 1;

            //front and back
            foreach (IntVec3 c in GenAdj.OccupiedRect(center, rot, size))
            {
                if (c.InBounds(map))
                {
                    int maxReachA = 0;
                    int maxReachB = 0;
                    int cellx = c.x;
                    int cellz = c.z;

                    if (rot.IsHorizontal)
                    {
                        //find reach
                        for (int i = 1; i <= reach + deep; i++)
                        {
                            IntVec3 target = new IntVec3(cellx + i, 0, cellz);
                            if (target.InBounds(map) && target.Walkable(map) && !map.roofGrid.Roofed(target)) maxReachA++;
                            else break;
                        }
                        for (int i = 1; i <= reach + deep; i++)
                        {
                            IntVec3 target = new IntVec3(Math.Max(0, cellx - i), 0, cellz);
                            if (target.InBounds(map) && target.Walkable(map) && !map.roofGrid.Roofed(target)) maxReachB++;
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
                        if (!large || c + IntVec3.North == start)
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
                        if (!large || c + IntVec3.South == end)
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
                    }
                    else
                    {
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
                        if (!large || c + IntVec3.East == start)
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
                        if (!large || c + IntVec3.West == end)
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
                    }
                    //clean cell itself
                    //area.Remove(c);
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
                centerAdjustA = IntVec3.West;
                centerAdjustB = IntVec3.East;
            }
            else
            {
                dirA = LinkDirections.Up;
                dirB = LinkDirections.Down;
                centerAdjustA = IntVec3.South;
                centerAdjustB = IntVec3.North;
            }
            foreach (IntVec3 c in GenAdj.CellsAdjacentAlongEdge(window.Position + centerAdjustA, window.Rotation, window.def.size, dirA))
            {
                if (!window.Map.roofGrid.Roofed(c))
                {
                    openSideA.Add(c);
                }
            }
            foreach (IntVec3 c in GenAdj.CellsAdjacentAlongEdge(window.Position + centerAdjustB, window.Rotation, window.def.size, dirB))
            {
                if (!window.Map.roofGrid.Roofed(c))
                {
                    openSideB.Add(c);
                }
            }
            if (openSideA.Count != openSideB.Count)
            {
                window.Facing = (openSideA.Count > openSideB.Count) ? dirA : dirB;
                window.isFacingSet = true;
                //Log.Message("Sides were " + openSideA.Count + "/" + openSideB.Count + ". New Facing is " + window.Facing);
            }
            else window.isFacingSet = false;
        }

        public static void FindEnds(Building_Window window)
        {
            window.start = FindEnd(window.Position, window.Rotation, window.def.size, false);
            window.end = FindEnd(window.Position, window.Rotation, window.def.size, true);
        }

        public static IntVec3 FindEnd(IntVec3 center, Rot4 rot, IntVec2 size, bool again)
        {
            LinkDirections dirA;
            LinkDirections dirB;
            IntVec3 adjust;
            int delta = again ? +1 : -1;
            if (rot.IsHorizontal)
            {
                dirA = LinkDirections.Up;
                dirB = LinkDirections.Down;
                adjust = new IntVec3(1, 0, delta);
            }
            else
            {
                dirA = LinkDirections.Right;
                dirB = LinkDirections.Left;
                adjust = new IntVec3(delta, 0, 1);
            }
            LinkDirections dir = again ? dirB : dirA;
            return GenAdj.CellsAdjacentAlongEdge(center + adjust, rot, size, dir).FirstOrFallback();
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
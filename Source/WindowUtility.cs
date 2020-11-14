using System;
using System.Collections.Generic;
using System.Linq;
using Verse;

namespace OpenTheWindows
{
    public class WindowUtility
    {
        public const int deep = 2;

        public static int CalculateWindowReach(IntVec2 size)
        {
            int witdh = Math.Max(size.x, size.z);
            return (witdh / 2 + 1) + deep;
        }

        public static Dictionary<IntVec3, int> CalculateWindowLightCells(Building_Window window)
        {
            return CalculateWindowLightCells(window.def.size, window.Reach, window.Position, window.Rotation, window.Map, window.start, window.end);
        }

        public static Dictionary<IntVec3, int> CalculateWindowLightCells(IntVec2 size, int reach, IntVec3 center, Rot4 rot, Map map, IntVec3 start, IntVec3 end)
        {
            return GetWindowObfuscation(center, rot, size, reach, map, start, end).Where(x => DoesLightReach(x.Key, center, map)).ToDictionary(x => x.Key, x => x.Value);
        }

        private static bool DoesLightReach(IntVec3 watchCell, IntVec3 buildingCenter, Map map)
        {
            return (watchCell.Walkable(map) && GenSight.LineOfSightToEdges(buildingCenter, watchCell, map, true, null));
        }
         
        private static IEnumerable<KeyValuePair<IntVec3,int>> GetWindowObfuscation(IntVec3 center, Rot4 rot, IntVec2 size, int reach, Map map, IntVec3 start, IntVec3 end)
        {
            //base vars
            //IEnumerable<KeyValuePair<IntVec3, int>> area = new IEnumerable<KeyValuePair<IntVec3, int>>();
            //int reach = Math.Max(size.x, size.z) / 2 + 1;
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
                        for (int i = 1; i <= reach; i++)
                        {
                            IntVec3 target = new IntVec3(cellx + i, 0, cellz);
                            if (target.InBounds(map) && target.Walkable(map) && !map.roofGrid.Roofed(target)) maxReachA++;
                            else break;
                        }
                        for (int i = 1; i <= reach; i++)
                        {
                            IntVec3 target = new IntVec3(Math.Max(0, cellx - i), 0, cellz);
                            if (target.InBounds(map) && target.Walkable(map) && !map.roofGrid.Roofed(target)) maxReachB++;
                            else break;
                        }
                        int maxReach = Math.Max(maxReachA, maxReachB);

                        //register affected cells
                        for (int i = 1; i <= maxReach; i++)
                        {
                            yield return new KeyValuePair<IntVec3, int>(new IntVec3(cellx + i, 0, cellz), i);
                            yield return new KeyValuePair<IntVec3, int>(new IntVec3(cellx - i, 0, cellz), i);
                            //area.Add(new IntVec3(cellx + i, 0, cellz), i);
                            //area.Add(new IntVec3(cellx - i, 0, cellz), i);
                        }

                        //add borders if on extremity
                        if (!large || c + IntVec3.North == start)
                        {
                            for (int i = 1; i <= maxReach; i++)
                            {
                                yield return new KeyValuePair<IntVec3, int>(new IntVec3(cellx + i, 0, cellz + 1), i);
                                yield return new KeyValuePair<IntVec3, int>(new IntVec3(cellx - i, 0, cellz + 1), i);
                                //area.Add(new IntVec3(cellx + i, 0, cellz + 1), i);
                                //area.Add(new IntVec3(cellx - i, 0, cellz + 1), i);
                            }
                        }
                        if (!large || c + IntVec3.South == end)
                        {
                            for (int i = 1; i <= maxReach; i++)
                            {
                                yield return new KeyValuePair<IntVec3, int>(new IntVec3(cellx + i, 0, cellz - 1), i);
                                yield return new KeyValuePair<IntVec3, int>(new IntVec3(cellx - i, 0, cellz - 1), i);
                                //area.Add(new IntVec3(cellx + i, 0, cellz - 1), i);
                                //area.Add(new IntVec3(cellx - i, 0, cellz - 1), i);
                            }
                        }
                    }
                    else
                    {
                        //find reach
                        for (int i = 1; i <= reach; i++)
                        {
                            IntVec3 target = new IntVec3(cellx, 0, cellz + i);
                            if (target.Walkable(map) & !map.roofGrid.Roofed(target)) maxReachA++;
                            else break;
                        }
                        for (int i = 1; i <= reach; i++)
                        {
                            IntVec3 target = new IntVec3(cellx, 0, Math.Max(0, cellz - i));
                            if (target.Walkable(map) & !map.roofGrid.Roofed(target)) maxReachB++;
                            else break;
                        }
                        int maxReach = Math.Max(maxReachA, maxReachB);

                        //register affected cells
                        for (int i = 1; i <= maxReach; i++)
                        {
                            yield return new KeyValuePair<IntVec3, int>(new IntVec3(cellx, 0, cellz + i), i);
                            yield return new KeyValuePair<IntVec3, int>(new IntVec3(cellx, 0, cellz - i), i);
                            //area.Add(new IntVec3(cellx, 0, cellz + i), i);
                            //area.Add(new IntVec3(cellx, 0, cellz - i), i);
                        }

                        //add borders if on extremity
                        if (!large || c + IntVec3.East == start)
                        {
                            for (int i = 1; i <= maxReach; i++)
                            {
                                yield return new KeyValuePair<IntVec3, int>(new IntVec3(cellx + 1, 0, cellz + i), i);
                                yield return new KeyValuePair<IntVec3, int>(new IntVec3(cellx + 1, 0, cellz - i), i);
                                //area.Add(new IntVec3(cellx + 1, 0, cellz + i), i);
                                //area.Add(new IntVec3(cellx + 1, 0, cellz - i), i);
                            }
                        }
                        if (!large || c + IntVec3.West == end)
                        {
                            for (int i = 1; i <= maxReach; i++)
                            {
                                yield return new KeyValuePair<IntVec3, int>(new IntVec3(cellx - 1, 0, cellz + i), i);
                                yield return new KeyValuePair<IntVec3, int>(new IntVec3(cellx - 1, 0, cellz - i), i);
                                //area.Add(new IntVec3(cellx - 1, 0, cellz + i), i);
                                //area.Add(new IntVec3(cellx - 1, 0, cellz - i), i);
                            }
                        }
                    }
                }
            }
            yield break;
            //return area;
        }

        public static void FindWindowExternalFacing(Building_Window window)
        {
            List<IntVec3> openSideA = new List<IntVec3>();
            List<IntVec3> openSideB = new List<IntVec3>();
            LinkDirections dirA, dirB;
            IntVec3 centerAdjustB, centerAdjustA;

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
    }
}
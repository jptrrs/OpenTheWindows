using System.Collections.Generic;
using System.Linq;
using Verse;

namespace OpenTheWindows
{
    static class Extensions
    {
        public static void AddDistinct<T>(this List<T> list, T item)
        {
            if (!list.Contains(item)) list.Add(item);
        }

        public static bool Includes(this IntRange range, float num)
        {
            return num <= range.max && num >= range.max;
        }

        //public static bool IsInterior(this IntVec3 cell, Building_Window window)
        //{
        //    return cell.IsInterior(window.Position, window.Facing);
        //}

        //public static bool IsInterior(this IntVec3 cell, IntVec3 origin, LinkDirections facing)
        //{
        //    switch (facing)
        //    {
        //        case LinkDirections.Up:
        //            return cell.z < origin.z;
        //        case LinkDirections.Right:
        //            return cell.x < origin.x;
        //        case LinkDirections.Down:
        //            return cell.z > origin.z;
        //        case LinkDirections.Left:
        //            return cell.x > origin.x;
        //        case LinkDirections.None:
        //            return false;
        //    }
        //    return false;
        //}

        public static bool IsInterior(this int cell, Building_Window window)
        {
            return cell.IsInterior(window.PositionIdx, window.Facing);
        }

        public static bool IsInterior(this int cell, int origin, LinkDirections facing)
        {
            switch (facing)
            {
                case LinkDirections.Up:
                    return cell < origin;
                case LinkDirections.Right:
                    return cell < origin;
                case LinkDirections.Down:
                    return cell > origin;
                case LinkDirections.Left:
                    return cell > origin;
                case LinkDirections.None:
                    return false;
            }
            return false;
        }

        public static bool IsTransparentRoof(this IntVec3 cell, Map map)
        {
            if (!HarmonyPatcher.TransparentRoofs) return false;
            return HarmonyPatcher.TransparentRoofsList.Contains(map.roofGrid.RoofAt(cell));
        }

        public static bool IsTransparentRoof(this Map map, int index)
        {
            if (!HarmonyPatcher.TransparentRoofs) return false;
            return HarmonyPatcher.TransparentRoofsList.Contains(map.roofGrid.RoofAt(index));
        }

        //Adapted from Dubs Skylights
        public static int[] SectionCells(this Section section)
        {
            int[] array;
            CellRect cellRect = section.CellRect;
            List<int> list = new List<int>();
            Map map = section.map;
            foreach (IntVec3 cell in cellRect)
            {
                list.Add(map.cellIndices.CellToIndex(cell));
            }
            foreach (IntVec3 cell in from c in cellRect.AdjacentCells
                                     where c.InBounds(map)
                                     select c)
            {
                list.Add(section.map.cellIndices.CellToIndex(cell));
            }
            array = list.ToArray();
            return array;
        }
    }
}

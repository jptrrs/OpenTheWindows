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

        public static bool IsInterior(this IntVec3 cell, Building_Window window)
        {
            return cell.IsInterior(window.Position, window.Facing);
        }

        public static bool IsInterior(this IntVec3 cell, IntVec3 origin, LinkDirections facing)
        {
            switch (facing)
            {
                case LinkDirections.Up:
                    if (cell.z < origin.z) return true;
                    break;
                case LinkDirections.Right:
                    if (cell.x < origin.x) return true;
                    break;
                case LinkDirections.Down:
                    if (cell.z > origin.z) return true;
                    break;
                case LinkDirections.Left:
                    if (cell.x > origin.x) return true;
                    break;
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

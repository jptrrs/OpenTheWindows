using System.Collections.Generic;
using Verse;

namespace OpenTheWindows
{
    public class MapComp_Windows : MapComponent
    {
        public List<Building_Window> cachedWindows = new List<Building_Window>();

        public bool[] WindowGrid;

        public MapComp_Windows(Map map) : base(map)
        {
            WindowGrid = new bool[map.cellIndices.NumGridCells];
        }

        public void DeRegisterWindow(Building_Window window)
        {
            if (cachedWindows.Contains(window))
            {
                cachedWindows.Remove(window);
            }
        }

        public void RegenGrid()
        {
            WindowGrid = new bool[map.cellIndices.NumGridCells];
            foreach (Building_Window window in cachedWindows)
            {
                if (window.open)
                {
                    window.CastLight();
                    foreach (IntVec3 c in window.illuminated)
                    {
                        WindowGrid[map.cellIndices.CellToIndex(c)] = true;
                    }
                }
            }
        }

        public void RegisterWindow(Building_Window window)
        {
            if (!cachedWindows.Contains(window))
            {
                cachedWindows.Add(window);
            }
        }
    }
}
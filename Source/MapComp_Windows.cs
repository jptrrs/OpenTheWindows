using System.Collections.Generic;
using Verse;

namespace OpenTheWindows
{
    public class MapComp_Windows : MapComponent
    {
        public MapComp_Windows(Map map) : base(map)
        {
            WindowGrid = new bool[map.cellIndices.NumGridCells];
            //LightGrid = new float[map.cellIndices.NumGridCells];
        }

        public void RegisterWindow(Building_Window window)
        {
            if (!cachedWindows.Contains(window))
            {
                cachedWindows.Add(window);
            }
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
                window.CastLight();
                foreach (IntVec3 c in window.illuminated)
                {
                    WindowGrid[map.cellIndices.CellToIndex(c)] = true;
                    //LightGrid[map.cellIndices.CellToIndex(c)] = 0f;
                }
                if (!window.isFacingSet) WindowUtility.FindWindowExternalFacing(window);
            }
        }

        public List<Building_Window> cachedWindows = new List<Building_Window>();

        public bool[] WindowGrid;

        //public float[] LightGrid;
    }
}
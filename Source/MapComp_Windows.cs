using Harmony;
using System;
using System.Collections.Generic;
using System.Reflection;
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
                if (window.open && window.isFacingSet)
                {
                    foreach (IntVec3 c in window.illuminated)
                    {
                        bool interior = false;
                        switch (window.Facing)
                        {
                            case LinkDirections.Up:
                                if (c.z < window.Position.z) interior = true;
                                break;

                            case LinkDirections.Right:
                                if (c.x < window.Position.x) interior = true;
                                break;

                            case LinkDirections.Down:
                                if (c.z > window.Position.z) interior = true;
                                break;

                            case LinkDirections.Left:
                                if (c.x > window.Position.x) interior = true;
                                break;

                            case LinkDirections.None:
                                break;
                        }
                        if (interior && map.roofGrid.Roofed(c))
                        {
                            WindowGrid[map.cellIndices.CellToIndex(c)] = true;
                        }
                    }
                }
            }
            if (HarmonyPatches.DubsSkylights)
            {
                Type type = AccessTools.TypeByName("Dubs_Skylight.MapComp_Skylights");
                FieldInfo skylightGridinfo = AccessTools.Field(type, "SkylightGrid");
                MethodInfo compInfo = AccessTools.Method(typeof(Map), "GetComponent", new[] { typeof(Type) });
                bool[] skyLightGrid = (bool[])skylightGridinfo.GetValue(compInfo.Invoke(map, new[] { type }));                
                for (int i = 0; i < skyLightGrid.Length; i++)
                {
                    if(skyLightGrid[i] == true)
                    {
                        WindowGrid[i] = true;
                    }
                }
            }
            if (HarmonyPatches.ExpandedRoofing)
            {
                Type type = AccessTools.TypeByName("ExpandedRoofing.RoofDefOf");
                FieldInfo roofTransparentInfo = AccessTools.Field(type, "RoofTransparent");
                RoofDef roofTransparent = (RoofDef)roofTransparentInfo.GetValue(Find.CurrentMap.roofGrid);
                for (int i = 0; i < map.cellIndices.NumGridCells; i++)
                {
                    if (map.roofGrid.RoofAt(i) == roofTransparent)
                    {
                        WindowGrid[i] = true;
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
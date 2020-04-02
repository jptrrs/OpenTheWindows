using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Reflection;
using Verse;

namespace OpenTheWindows
{
    public class MapComp_Windows : MapComponent
    {
        public List<Building_Window> cachedWindows = new List<Building_Window>();
        public bool updateRequest = false;
        public bool roofUpdateRequest = false;
        public bool[] WindowGrid;
        public int[] WindowScanGrid;
        //private Map lastSeenMap;
        //private int nextUpdateTick;
        //private int updateDelay = (int)OpenTheWindowsSettings.UpdateInterval;

        public MapComp_Windows(Map map) : base(map)
        {
            WindowGrid = new bool[map.cellIndices.NumGridCells];
            WindowScanGrid = new int[map.cellIndices.NumGridCells];
        }

        public void CastNaturalLightOnDemand()
        {
            bool doRegen = false;
            List<IntVec3> affected = new List<IntVec3>();
            foreach (Building_Window window in cachedWindows)
            {
                if (roofUpdateRequest && window.NeedExternalFacingUpdate())
                {
                    WindowUtility.FindWindowExternalFacing(window);
                    window.CastLight();
                    doRegen = true;
                    affected.Add(window.Position);
                }
                if (!doRegen && window.NeedLightUpdate())
                {
                    doRegen = true;
                    affected.Add(window.Position);
                }
            }
            if (doRegen)
            {
                RegenGrid();
                foreach (IntVec3 c in affected)
                {
                    map.glowGrid.MarkGlowGridDirty(c);
                }
            }
            updateRequest = (roofUpdateRequest = false);
        }

        public void DeRegisterWindow(Building_Window window)
        {
            if (cachedWindows.Contains(window))
            {
                cachedWindows.Remove(window);
                SetWindowScanArea(window, false);
            }
        }

        public override void MapComponentTick()
        {
            base.MapComponentTick();
            if (updateRequest || roofUpdateRequest)
            {
                CastNaturalLightOnDemand();
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
                    if (skyLightGrid[i] == true)
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
                SetWindowScanArea(window, true);
            }
        }

        private void SetWindowScanArea(Building_Window window, bool register)
        {
            Map map = window.Map;
            int deep = WindowUtility.deep;
            int reach = Math.Max(window.def.size.x, window.def.size.z) / 2 + 1;
            int delta = register ? +1 : -1;

            //front and back
            foreach (IntVec3 c in GenAdj.OccupiedRect(window.Position, window.Rotation, window.def.size))
            {
                if (c.InBounds(map))
                {
                    int cellx = c.x;
                    int cellz = c.z;
                    for (int i = 1; i <= +reach + deep; i++)
                    {
                        if (window.Rotation.IsHorizontal)
                        {
                            IntVec3 targetA = new IntVec3(cellx + i, 0, cellz);
                            if (targetA.InBounds(map)) WindowScanGrid[map.cellIndices.CellToIndex(targetA)] += delta;
                            IntVec3 targetB = new IntVec3(Math.Max(0, cellx - i), 0, cellz);
                            if (targetB.InBounds(map)) WindowScanGrid[map.cellIndices.CellToIndex(targetB)] += delta;
                        }
                        else
                        {
                            IntVec3 targetA = new IntVec3(cellx, 0, cellz + i);
                            if (targetA.InBounds(map)) WindowScanGrid[map.cellIndices.CellToIndex(targetA)] += delta;
                            IntVec3 targetB = new IntVec3(cellx, 0, Math.Max(0, cellz - i));
                            if (targetB.InBounds(map)) WindowScanGrid[map.cellIndices.CellToIndex(targetB)] += delta;
                        }
                    }
                }
            }
        }
    }
}
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Verse;

namespace OpenTheWindows
{
    public class MapComp_Windows : MapComponent
    {
        public List<Building_Window> cachedWindows = new List<Building_Window>();
        public HashSet<IntVec3> WindowCells;
        public int[] WindowScanGrid;
        private FieldInfo
            DubsSkylights_skylightGridinfo,
            ExpandedRoofing_roofTransparentInfo;
        private Type
            DubsSkylights_type,
            ExpandedRoofing_type;
        private MethodInfo MapCompInfo;

        public MapComp_Windows(Map map) : base(map)
        {
            WindowScanGrid = new int[map.cellIndices.NumGridCells];
            WindowCells = new HashSet<IntVec3>();
            if (HarmonyPatcher.DubsSkylights)
            {
                DubsSkylights_type = AccessTools.TypeByName("Dubs_Skylight.MapComp_Skylights");
                DubsSkylights_skylightGridinfo = AccessTools.Field(DubsSkylights_type, "SkylightGrid");
                MapCompInfo = AccessTools.Method(typeof(Map), "GetComponent", new[] { typeof(Type) });
            }
            if (HarmonyPatcher.ExpandedRoofing)
            {
                ExpandedRoofing_type = AccessTools.TypeByName("ExpandedRoofing.RoofDefOf");
                ExpandedRoofing_roofTransparentInfo = AccessTools.Field(ExpandedRoofing_type, "RoofTransparent");
            }
            MapUpdateWatcher.MapUpdate += MapUpdated;  // register with an event, handler must match template signature
        }

        public void CastNaturalLightOnDemand(List<Building_Window> windows, bool reFace)
        {
            foreach (Building_Window window in windows)
            {
                if (reFace && window.NeedExternalFacingUpdate())
                {
                    WindowUtility.FindWindowExternalFacing(window);
                }
                window.CastLight();
            }
        }

        public void DeRegisterWindow(Building_Window window)
        {
            if (cachedWindows.Contains(window))
            {
                cachedWindows.Remove(window);
                SetWindowScanArea(window, false);
            }
        }

        public void ExcludeTile(IntVec3 tile)
        {
            if (WindowCells.Contains(tile))
            {
                WindowCells.Remove(tile);
                map.glowGrid.MarkGlowGridDirty(tile);
            }
        }

        public void IncludeTile(IntVec3 tile)
        {
            if (!WindowCells.Contains(tile))
            {
                WindowCells.Add(tile);
                map.glowGrid.MarkGlowGridDirty(tile);
            }
        }
        public void MapUpdated(object sender, IntVec3 center) // event handler
        {
            map.cellIndices.CellToIndex(center);
            List<Building_Window> windows = new List<Building_Window>();
            var region = map.regionGrid.GetValidRegionAt(center);
            if (region == null) return;
            FindAffectedWindows(windows, region);
            if (!windows.NullOrEmpty()) CastNaturalLightOnDemand(windows, sender is RoofGrid);
        }

        //Windows register their cells on their on, this is just for compatibles.
        public void RegenGrid()
        {
            if (HarmonyPatcher.DubsSkylights)
            {
                bool[] DubsSkylights_skyLightGrid = (bool[])DubsSkylights_skylightGridinfo.GetValue(MapCompInfo.Invoke(map, new[] { DubsSkylights_type }));
                for (int i = 0; i < DubsSkylights_skyLightGrid.Length; i++)
                {
                    if (DubsSkylights_skyLightGrid[i] == true)
                    {
                        WindowCells.Add(map.cellIndices.IndexToCell(i));
                    }
                }
            }

            if (HarmonyPatcher.ExpandedRoofing)
            {
                RoofDef roofTransparent = (RoofDef)ExpandedRoofing_roofTransparentInfo.GetValue(Find.CurrentMap.roofGrid);
                for (int i = 0; i < map.cellIndices.NumGridCells; i++)
                {
                    if (map.roofGrid.RoofAt(i) == roofTransparent)
                    {
                        WindowCells.Add(map.cellIndices.IndexToCell(i));
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

        private static void FindAffectedWindows(List<Building_Window> windows, Region region, bool recursive = true)
        {
            foreach (Region connected in region.links.Select(x => x.GetOtherRegion(region)))
            {
                if (connected.IsDoorway)
                {
                    var window = connected.ListerThings.AllThings.FirstOrDefault(x => x is Building_Window);
                    if (window != null)
                    {
                        windows.Add(window as Building_Window);
                    }
                }
                else if (recursive) FindAffectedWindows(windows, connected, false);
            }
        }

        private void SetWindowScanArea(Building_Window window, bool register)
        {
            Map map = window.Map;
            int deep = WindowUtility.deep;
            int reach = Math.Max(window.def.size.x, window.def.size.z) / 2 + 1;
            int delta = register ? 1 : -1;

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
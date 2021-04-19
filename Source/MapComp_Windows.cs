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
        public HashSet<IntVec3> WindowCells;
        public int[] WindowScanGrid;

        private Type DubsSkylights_type;
        private FieldInfo DubsSkylights_skylightGridinfo;
        private MethodInfo MapCompInfo;
        private Type ExpandedRoofing_type;
        private FieldInfo ExpandedRoofing_roofTransparentInfo;

        public void IncludeTile(IntVec3 tile)
        {
            if (!WindowCells.Contains(tile))
            {
                WindowCells.Add(tile);
                map.glowGrid.MarkGlowGridDirty(tile);
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
        }

        public void CastNaturalLightOnDemand()
        {
            foreach (Building_Window window in cachedWindows)
            {
                if (roofUpdateRequest && window.NeedExternalFacingUpdate())
                {
                    WindowUtility.FindWindowExternalFacing(window);
                }
                window.CastLight();
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

        // Issue. This function sucks.
        public void RegenGrid()
        {
            /*
             * Why on earth do we need to reset here? We can very easily tell which window has been changed, WE IMPLEMENT THE WINDOW CLASS !!!
             * You can quite easily implement this and make this function mostly neglible performance wise.
             */
            //WindowCells = new HashSet<IntVec3>();

            //foreach (Building_Window window in cachedWindows)
            //{
            //    if (window.open && window.isFacingSet)
            //    {
            //        foreach (IntVec3 c in window.effectArea)
            //        {
            //            bool interior = false;
            //            switch (window.Facing)
            //            {
            //                case LinkDirections.Up:
            //                    if (c.z < window.Position.z) interior = true;
            //                    break;

            //                case LinkDirections.Right:
            //                    if (c.x < window.Position.x) interior = true;
            //                    break;

            //                case LinkDirections.Down:
            //                    if (c.z > window.Position.z) interior = true;
            //                    break;

            //                case LinkDirections.Left:
            //                    if (c.x > window.Position.x) interior = true;
            //                    break;

            //                case LinkDirections.None:
            //                    break;
            //            }
            //            if (interior)
            //            {
            //                WindowCells.Add(c);
            //            }
            //        }
            //    }
            //}

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
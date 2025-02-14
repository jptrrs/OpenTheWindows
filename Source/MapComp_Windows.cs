﻿using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Verse;

namespace OpenTheWindows
{
    using static HarmonyPatcher;

    public class MapComp_Windows : MapComponent
    {
        public static Dictionary<int, MapComp_Windows> MapCompsCache = new Dictionary<int, MapComp_Windows>();
        public List<Building_Window> cachedWindows = new List<Building_Window>();
        public Dictionary<Section, int[]> SectionCellsCache;
        public HashSet<int> WindowCells;
        private bool audit = false;
        private FieldInfo DubsSkylights_skylightGridinfo;
        private Type DubsSkylights_type;
        private NaturalLightOverlay lightOverlay;
        private MethodInfo MapCompInfo;
        private HashSet<int> wrongTiles;

        public MapComp_Windows(Map map) : base(map)
        {
            if (MapCompsCache.ContainsKey(map.uniqueID))
            {
                MapCompsCache[map.uniqueID] = this;
            }
            else
            {
                MapCompsCache.Add(map.uniqueID, this);
            }
            WindowCells = new HashSet<int>();
            SectionCellsCache = new Dictionary<Section, int[]>();
            lightOverlay = new NaturalLightOverlay(this);
            if (DubsSkylights)
            {
                DubsSkylights_type = AccessTools.TypeByName("Dubs_Skylight.MapComp_Skylights");
                DubsSkylights_skylightGridinfo = AccessTools.Field(DubsSkylights_type, "SkylightGrid");
                MapCompInfo = AccessTools.Method(typeof(Map), "GetComponent", new[] { typeof(Type) });
            }
            if (DubsSkylights || TransparentRoofs)
            {
                MapUpdateWatcher.MapUpdate += MapUpdated;
            }
        }

        private bool[] skyLightGrid
        {
            get
            {
                if (DubsSkylights) return (bool[])DubsSkylights_skylightGridinfo.GetValue(MapCompInfo.Invoke(map, new[] { DubsSkylights_type }));
                return null;
            }
        }

        public void DeRegisterWindow(Building_Window window)
        {
            if (cachedWindows.Contains(window))
            {
                cachedWindows.Remove(window);
            }
        }

        public void ExcludeTile(IntVec3 tile, bool bypass = false)
        {
            int index = map.cellIndices.CellToIndex(tile);
            if (!WindowCells.Contains(index)) return;
            if (DubsSkylights && skyLightGrid[map.cellIndices.CellToIndex(tile)]) return;
            if (!bypass && tile.IsTransparentRoof(map)) return;
            WindowCells.Remove(index);
            map.glowGrid.DirtyCache(tile);
            map.mapDrawer.MapMeshDirty(tile, MapMeshFlagDefOf.Roofs);
            lightOverlay.needsUpdate = true;
        }

        public void ExcludeTileRange(IEnumerable<IntVec3> tiles)
        {
            foreach (var c in tiles)
            {
                ExcludeTile(c);
            }
        }

        public override void FinalizeInit()
        {
            if (DubsSkylights)
            {
                wrongTiles = map.AllCells.Select(x => map.cellIndices.CellToIndex(x)).Where(i => skyLightGrid[i]).ToHashSet();
                audit = !wrongTiles.EnumerableNullOrEmpty();
            }
            base.FinalizeInit();
        }

        public int[] GetCachedSectionCells(Section section)
        {
            int[] array;
            if (SectionCellsCache.TryGetValue(section, out array)) return array;
            array = section.SectionCells();
            SectionCellsCache.Add(section, array);
            return array;
        }

        public void IncludeTile(IntVec3 tile)
        {
            int index = map.cellIndices.CellToIndex(tile);
            if (WindowCells.Contains(index)) return;
            WindowCells.Add(index);
            map.glowGrid.DirtyCache(tile);
            map.mapDrawer.MapMeshDirty(tile, MapMeshFlagDefOf.Roofs);
            lightOverlay.needsUpdate = true;
        }

        public void IncludeTileRange(IEnumerable<IntVec3> tiles)
        {
            foreach (var c in tiles)
            {
                IncludeTile(c);
            }
        }

        public bool IsUnderWindow(IntVec3 cell)
        {
            if (WindowCells.NullOrEmpty()) return false;
            return WindowCells.Contains(map.cellIndices.CellToIndex(cell));
        }

        public override void MapComponentTick()
        {
            if (audit)
            {
                foreach (int idx in wrongTiles)
                {
                    map.glowGrid.DirtyCache(map.cellIndices.IndexToCell(idx));
                }
                wrongTiles.Clear();
                audit = false;
            }
            base.MapComponentTick();
        }

        public override void MapComponentUpdate()
        {
            base.MapComponentUpdate();
            lightOverlay.Update();
        }

        public override void MapRemoved()
        {
            MapCompsCache.Remove(map.uniqueID);
            base.MapRemoved();
        }

        public void MapUpdated(object sender, MapUpdateWatcher.MapUpdateInfo info)
        {
            if (info.map != map) return;
            if (DubsSkylights && sender.GetType() == Building_Skylight)
            {
                Thing thing = sender as Thing;
                var tiles = thing.OccupiedRect().ExpandedBy(1).Cells;
                ReactSkylights(info, tiles);
            }
            if (TransparentRoofs && sender is RoofGrid && info.roofDef != null && TransparentRoofsList.Contains(info.roofDef))
            {
                ReactTransparentRoof(info);
            }
        }

        public void RegisterWindow(Building_Window window)
        {
            if (!cachedWindows.Contains(window))
            {
                cachedWindows.Add(window);
            }
        }

        private void ReactSkylights(MapUpdateWatcher.MapUpdateInfo info, IEnumerable<IntVec3> tiles)
        {
            if (info.removed)
            {
                ExcludeTileRange(tiles);
                WindowUtility.ResetWindowsAround(map, info.center);
            }
            else
            {
                IncludeTileRange(tiles);
            }
        }

        private void ReactTransparentRoof(MapUpdateWatcher.MapUpdateInfo info)
        {
            if (TransparentRoofsList.Contains(info.roofDef))
            {
                if (info.removed)
                {
                    ExcludeTile(info.center, true);
                    WindowUtility.ResetWindowsAround(map, info.center);
                }
                else
                {
                    IncludeTile(info.center);
                }
            }
        }
    }
}
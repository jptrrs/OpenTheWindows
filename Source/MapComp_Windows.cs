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
        private FieldInfo DubsSkylights_skylightGridinfo;
        private Type DubsSkylights_type;
        private NaturalLightOverlay lightOverlay;
        private MethodInfo MapCompInfo;

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

        public void ExcludeTile(int index, bool bypass = false)
        {
            if (!WindowCells.Contains(index)) return;
            if (DubsSkylights && skyLightGrid[index]) return;
            if (!bypass && map.IsTransparentRoof(index)) return;
            WindowCells.Remove(index);
            UpdateMapAt(index);
        }

        public void ExcludeTileRange(List<int> tiles)
        {
            foreach (var i in tiles)
            {
                ExcludeTile(i);
            }
        }


        public int[] GetCachedSectionCells(Section section)
        {
            int[] array;
            if (SectionCellsCache.TryGetValue(section, out array)) return array;
            array = section.SectionCells();
            SectionCellsCache.Add(section, array);
            return array;
        }

        public void IncludeTile(int index)
        {
            if (WindowCells.Contains(index)) return;
            WindowCells.Add(index);
            UpdateMapAt(index);
        }

        public void IncludeTileRange(List<int> tiles)
        {
            foreach (var i in tiles)
            {
                IncludeTile(i);
            }
        }

        public bool IsUnderWindow(IntVec3 cell)
        {
            if (WindowCells.NullOrEmpty()) return false;
            return WindowCells.Contains(map.cellIndices.CellToIndex(cell));
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

        private void ReactSkylights(MapUpdateWatcher.MapUpdateInfo info, IEnumerable<IntVec3> tiles)
        {
            List<int> cells = tiles.Select(x => map.cellIndices.CellToIndex(x)).ToList();
            if (info.removed)
            {
                ExcludeTileRange(cells);
                WindowUtility.ResetWindowsAround(map, info.origin);
            }
            else
            {
                IncludeTileRange(cells);
            }
        }

        private void ReactTransparentRoof(MapUpdateWatcher.MapUpdateInfo info)
        {
            if (TransparentRoofsList.Contains(info.roofDef))
            {
                if (info.removed)
                {
                    ExcludeTile(info.Origin, true);
                    WindowUtility.ResetWindowsAround(map, info.origin);
                }
                else
                {
                    IncludeTile(info.Origin);
                }
            }
        }

        private void UpdateMapAt(int index)
        {
            IntVec3 tile = map.cellIndices.IndexToCell(index);
            map.mapDrawer.MapMeshDirty(tile, MapMeshFlagDefOf.Roofs);
            map.glowGrid.DirtyCell(tile);
            map.events.Notify_RoofChanged(tile);
            lightOverlay.needsUpdate = true;
        }
    }
}
using HarmonyLib;
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
        public List<Building_Window> cachedWindows = new List<Building_Window>();
        public HashSet<IntVec3> WindowCells;
        private bool audit = false;
        private FieldInfo DubsSkylights_skylightGridinfo;
        private Type DubsSkylights_type;
        private MethodInfo MapCompInfo;
        private HashSet<int> wrongTiles;

        private bool[] skyLightGrid
        {
            get
            {
                if (DubsSkylights) return (bool[])DubsSkylights_skylightGridinfo.GetValue(MapCompInfo.Invoke(map, new[] { DubsSkylights_type }));
                return null;
            }
        }

        public MapComp_Windows(Map map) : base(map)
        {
            WindowCells = new HashSet<IntVec3>();
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

        public void DeRegisterWindow(Building_Window window)
        {
            if (cachedWindows.Contains(window))
            {
                cachedWindows.Remove(window);
            }
        }

        public void ExcludeTile(IntVec3 tile, bool bypass = false)
        {
            if (!WindowCells.Contains(tile)) return;
            if (DubsSkylights && skyLightGrid[map.cellIndices.CellToIndex(tile)]) return;
            if (TransparentRoofs && !bypass && TransparentRoofsList.Contains(map.roofGrid.RoofAt(tile))) return;
            WindowCells.Remove(tile);
            map.glowGrid.MarkGlowGridDirty(tile);
        }

        public void ExcludeTileRange(IEnumerable<IntVec3> tiles)
        {
            //Log.Message($"DEBUG excluding {tiles.Count()} tiles");
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

        public void IncludeTile(IntVec3 tile)
        {
            if (WindowCells.Contains(tile)) return;
            WindowCells.Add(tile);
            map.glowGrid.MarkGlowGridDirty(tile);
        }

        public void IncludeTileRange(IEnumerable<IntVec3> tiles)
        {
            //Log.Message($"DEBUG including {tiles.Count()} tiles");
            foreach (var c in tiles)
            {
                IncludeTile(c);
            }
        }

        public override void MapComponentTick()
        {
            if (audit)
            {
                foreach (int idx in wrongTiles)
                {
                    map.glowGrid.MarkGlowGridDirty(map.cellIndices.IndexToCell(idx));
                }
                wrongTiles.Clear();
                audit = false;
            }
            base.MapComponentTick();
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
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
        private FieldInfo
            DubsSkylights_skylightGridinfo,
            ExpandedRoofing_roofTransparentInfo;
        private Type
            DubsSkylights_type,
            ExpandedRoofing_type;
        private MethodInfo MapCompInfo;
        private bool[] skyLightGrid => (bool[])DubsSkylights_skylightGridinfo.GetValue(MapCompInfo.Invoke(map, new[] { DubsSkylights_type }));
        RoofDef roofTransparent;

        public MapComp_Windows(Map map) : base(map)
        {
            WindowCells = new HashSet<IntVec3>();
            if (DubsSkylights)
            {
                DubsSkylights_type = AccessTools.TypeByName("Dubs_Skylight.MapComp_Skylights");
                DubsSkylights_skylightGridinfo = AccessTools.Field(DubsSkylights_type, "SkylightGrid");
                MapCompInfo = AccessTools.Method(typeof(Map), "GetComponent", new[] { typeof(Type) });
                MapUpdateWatcher.MapUpdate += MapUpdated;
            }
            if (ExpandedRoofing)
            {
                //ExpandedRoofing_type = AccessTools.TypeByName("ExpandedRoofing.RoofDefOf");
                //ExpandedRoofing_roofTransparentInfo = AccessTools.Field(ExpandedRoofing_type, "RoofTransparent");
                //roofTransparent = (RoofDef)ExpandedRoofing_roofTransparentInfo.GetValue(Find.CurrentMap.roofGrid);
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
            if (ExpandedRoofing && !bypass && transparentRoofs.Contains(map.roofGrid.RoofAt(tile))) return;
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

        public void MapUpdated(object sender, MapUpdateWatcher.MapUpdateInfo info)
        {
            if (DubsSkylights && sender.GetType() == Building_Skylight)
            {
                Thing thing = sender as Thing;
                var tiles = thing.OccupiedRect().ExpandedBy(1).Cells;
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
            if (ExpandedRoofing && sender is RoofGrid && info.roofDef != null && info.roofDef.modContentPack == ExpandedRoofingMod)
            {
                bool transparent = transparentRoofs.Contains(info.roofDef);
                if ((transparent && info.removed) || (!info.removed && !transparent))
                {
                    ExcludeTile(info.center, true);
                    WindowUtility.ResetWindowsAround(map, info.center);
                }
                if (!info.removed && transparent)
                {
                    IncludeTile(info.center);
                }
            }
        }

        //Windows register their cells on their on, this is just for compatibles.
        //public void RegenGrid()
        //{
        //    if (DubsSkylights)
        //    {
        //        for (int i = 0; i < skyLightGrid.Length; i++)
        //        {
        //            if (skyLightGrid[i] == true)
        //            {
        //                WindowCells.Add(map.cellIndices.IndexToCell(i));
        //            }
        //        }
        //    }

        //    //if (ExpandedRoofing)
        //    //{
        //    //    for (int i = 0; i < map.cellIndices.NumGridCells; i++)
        //    //    {
        //    //        if (map.roofGrid.RoofAt(i) == roofTransparent)
        //    //        {
        //    //            WindowCells.Add(map.cellIndices.IndexToCell(i));
        //    //        }
        //    //    }
        //    //}
        //}

        public void RegisterWindow(Building_Window window)
        {
            if (!cachedWindows.Contains(window))
            {
                cachedWindows.Add(window);
            }
        }
    }
}
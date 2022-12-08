using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using Verse;
using static OpenTheWindows.HarmonyPatcher;

namespace OpenTheWindows
{
    public class MapComp_Windows : MapComponent
    {
        public HashSet<IntVec3> WindowCells = new HashSet<IntVec3>();
        System.Reflection.FieldInfo DubsSkylights_skylightGridinfo;
        Type DubsSkylights_type;
        System.Reflection.MethodInfo MapCompInfo;
        NaturalLightOverlay lightOverlay;

        public MapComp_Windows(Map map) : base(map)
        {
            lightOverlay = new NaturalLightOverlay(this);
            if (DubsSkylights)
            {
                DubsSkylights_type = AccessTools.TypeByName("Dubs_Skylight.MapComp_Skylights");
                DubsSkylights_skylightGridinfo = AccessTools.Field(DubsSkylights_type, "SkylightGrid");
                MapCompInfo = AccessTools.Method(typeof(Map), "GetComponent", new[] { typeof(Type) });
            }
            if (DubsSkylights || TransparentRoofs) MapUpdateWatcher.MapUpdate += MapUpdated;
        }

        bool[] GetSkyLightGrid()
        {
            return (bool[])DubsSkylights_skylightGridinfo.GetValue(MapCompInfo.Invoke(map, new[] { DubsSkylights_type }));
        }

        public void ExcludeTile(IntVec3 tile, bool bypass = false)
        {
            if (!WindowCells.Contains(tile)) return;
            if (DubsSkylights && GetSkyLightGrid()[map.cellIndices.CellToIndex(tile)]) return;
            if (!bypass && tile.IsTransparentRoof(map)) return;
            WindowCells.Remove(tile);
            map.glowGrid.MarkGlowGridDirty(tile);
            lightOverlay.needsUpdate = true;
        }

        public void ExcludeTileRange(IEnumerable<IntVec3> tiles)
        {
            foreach (var c in tiles) ExcludeTile(c);
        }

        public override void FinalizeInit()
        {
            if (DubsSkylights)
            {
                var wrongTiles = map.AllCells.Select(x => map.cellIndices.CellToIndex(x)).Where(i => GetSkyLightGrid()[i]);
                foreach (var index in wrongTiles)
                {
                    map.glowGrid.MarkGlowGridDirty(map.cellIndices.IndexToCell(index));
                }
            }
            base.FinalizeInit();
        }

        public void IncludeTile(IntVec3 tile)
        {
            if (WindowCells.Contains(tile)) return;
            WindowCells.Add(tile);
            map.glowGrid.MarkGlowGridDirty(tile);
            lightOverlay.needsUpdate = true;
        }

        public void IncludeTileRange(IEnumerable<IntVec3> tiles)
        {
            foreach (var c in tiles) IncludeTile(c);
        }

        public override void MapComponentUpdate()
        {
            base.MapComponentUpdate();
            if (NaturalLightOverlay.toggleShow) lightOverlay.Update();
        }

        public void MapUpdated(object sender, MapUpdateWatcher.MapUpdateInfo info)
        {
            if (info.map != map) return;
            if (DubsSkylights && sender.GetType() == Building_Skylight)
            {
                //ReactSkylights
                var tiles = ((Thing)sender).OccupiedRect().ExpandedBy(1).Cells;
                if (info.removed)
                {
                    ExcludeTileRange(tiles);
                    WindowUtility.ResetWindowsAround(map, info.center);
                }
                else IncludeTileRange(tiles);
            }
            if (TransparentRoofs && sender is RoofGrid && info.roofDef != null && TransparentRoofsList.Contains(info.roofDef))
            {
                //ReactTransparentRoof
                if (info.removed)
                {
                    ExcludeTile(info.center, true);
                    WindowUtility.ResetWindowsAround(map, info.center);
                }
                else IncludeTile(info.center);
            }
        }
    }
}
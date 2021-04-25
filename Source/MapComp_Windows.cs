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
        public HashSet<IntVec3> WindowCells;
        private FieldInfo
            DubsSkylights_skylightGridinfo,
            ExpandedRoofing_roofTransparentInfo;
        private Type
            DubsSkylights_type,
            ExpandedRoofing_type;
        private MethodInfo MapCompInfo;

        public MapComp_Windows(Map map) : base(map)
        {
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

        public void DeRegisterWindow(Building_Window window)
        {
            if (cachedWindows.Contains(window))
            {
                cachedWindows.Remove(window);
            }
        }

        public void ExcludeTile(IntVec3 tile)
        {
            if (WindowCells.Contains(tile))
            {
                WindowCells.Remove(tile);
            }
            map.glowGrid.MarkGlowGridDirty(tile);
        }

        public void ExcludeTileRange(List<IntVec3> tiles)
        {
            //Log.Message($"DEBUG excluding {tiles.Count()} tiles");
            foreach (var c in tiles)
            {
                ExcludeTile(c);
            }
        }

        public void IncludeTile(IntVec3 tile)
        {
            if (!WindowCells.Contains(tile))
            {
                WindowCells.Add(tile);
            }
            map.glowGrid.MarkGlowGridDirty(tile);
        }

        public void IncludeTileRange(List<IntVec3> tiles)
        {
            //Log.Message($"DEBUG including {tiles.Count()} tiles");
            foreach (var c in tiles)
            {
                IncludeTile(c);
            }
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
            }
        }
    }
}
using UnityEngine;
using Verse;

namespace OpenTheWindows
{
    public class NaturalLightOverlay : ICellBoolGiver
    {
        public static bool toggleShow;
        public bool needsUpdate;
        static CellBoolDrawer drawer;
        public static string tooltip;
        Map lastSeenMap;
        MapComp_Windows Parent;
        Color white = Color.white, green = Color.green;

        public NaturalLightOverlay(MapComp_Windows parent)
        {
            Parent = parent;
            lastSeenMap = parent.map;
            drawer = new CellBoolDrawer(this, parent.map.Size.x, parent.map.Size.z);
            tooltip = "NaturalLightMap".Translate();
        }

        public Color Color
        {
            get { return white; }
        }

        public bool GetCellBool(int index)
        {
            return !lastSeenMap.fogGrid.fogGrid[index] && (lastSeenMap.roofGrid.roofGrid[index] == null || Parent.WindowCells.Contains(lastSeenMap.cellIndices.IndexToCell(index)));
        }

        public Color GetCellExtraColor(int index)
        {
            return green;
        }

        int ticker = 0;
        public void Update()
        {
            if (!toggleShow) return;
            if (Find.CurrentMap.uniqueID != lastSeenMap.uniqueID)
            {
                lastSeenMap = Find.CurrentMap;
                drawer = new CellBoolDrawer(this, lastSeenMap.Size.x, lastSeenMap.Size.z);
                needsUpdate = true;
            }
            if (ticker++ == 250)
            {
                ticker = 0;
                needsUpdate = true;
            }
            if (needsUpdate)
            {
                drawer.dirty = true;
                needsUpdate = false;
            }
            drawer.wantDraw = true;
            drawer.CellBoolDrawerUpdate();
        }
    }
}
using UnityEngine;
using Verse;

namespace OpenTheWindows
{
    public class NaturalLightOverlay : ICellBoolGiver
    {
        public bool needsUpdate = false;
        public static bool toggleShow = false;
        public static string tooltip;
        protected static CellBoolDrawer drawer;
        protected float defaultOpacity;
        private Map lastSeenMap;
        private MapComp_Windows parent;

        public NaturalLightOverlay(MapComp_Windows parent)
        {
            this.parent = parent;
            lastSeenMap = parent.map;
            drawer = new CellBoolDrawer(this, parent.map.Size.x, parent.map.Size.z);
            tooltip = "NaturalLightMap".Translate();
        }

        public Color Color => Color.white;

        public bool GetCellBool(int index)
        {
            return !lastSeenMap.fogGrid.fogGrid[index] && (lastSeenMap.roofGrid.roofGrid[index] == null || parent.WindowCells.Contains(index));
        }

        public Color GetCellExtraColor(int index)
        {
            return Color.green;
        }

        public void MakeDrawer()
        {
            drawer = new CellBoolDrawer(this, Find.CurrentMap.Size.x, Find.CurrentMap.Size.z);
            needsUpdate = true;
        }

        public void Update()
        {
            if (!toggleShow) return;
            if (Find.CurrentMap != lastSeenMap)
            {
                lastSeenMap = Find.CurrentMap;
                MakeDrawer();
            }
            if (needsUpdate)
            {
                drawer.SetDirty();
                needsUpdate = false;
            }
            drawer.MarkForDraw();
            drawer.CellBoolDrawerUpdate();
        }
    }
}
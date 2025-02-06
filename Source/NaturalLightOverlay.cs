using UnityEngine;
using Verse;

namespace OpenTheWindows
{
    public class NaturalLightOverlay : ICellBoolGiver
    {
        public static bool toggleShow = false;

        public bool needsUpdate = false;

        protected static CellBoolDrawer drawer;

        protected float defaultOpacity;

        private Map lastSeenMap;

        private MapComp_Windows Parent;

        private int updateDelay = 120;

        public NaturalLightOverlay(MapComp_Windows parent)
        {
            Parent = parent;
            lastSeenMap = parent.map;
            drawer = new CellBoolDrawer(this, parent.map.Size.x, parent.map.Size.z);
        }

        public Color Color
        {
            get
            {
                return Color.white;
            }
        }

        private Map map => Find.CurrentMap;

        public static Texture2D Icon()
        {
            return ContentFinder<Texture2D>.Get("NaturalLightMap", true);
        }

        public static string IconTip()
        {
            return "NaturalLightMap".Translate();
        }

        public bool GetCellBool(int index)
        {
            return !Find.CurrentMap.fogGrid.IsFogged(index) && ShowCell(index);
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

        public bool ShowCell(int index)
        {
            return map.GetComponent<MapComp_Windows>().WindowCells.Contains(index) || !map.roofGrid.Roofed(index);
        }

        public void Update()
        {
            if (!toggleShow) return;
            if (Find.TickManager.TicksGame % updateDelay == 0 || Find.CurrentMap != lastSeenMap)
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
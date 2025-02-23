using UnityEngine;
using Verse;

namespace OpenTheWindows
{
    public class NaturalLightOverlay : ICellBoolGiver
    {
        public bool needsUpdate = false;
        public static bool toggleShow = false;
        public static string tooltip;
        public static Texture2D icon;
        protected static CellBoolDrawer drawer;
        protected float defaultOpacity;
        private Map lastSeenMap;
        private MapComp_Windows parent;
        //private int updateDelay = 120;

        public NaturalLightOverlay(MapComp_Windows parent)
        {
            this.parent = parent;
            lastSeenMap = parent.map;
            drawer = new CellBoolDrawer(this, parent.map.Size.x, parent.map.Size.z);
            tooltip = "NaturalLightMap".Translate();
            //icon = ContentFinder<Texture2D>.Get("NaturalLightMap", true);
        }

        public Color Color => Color.white;

        private Map map => Find.CurrentMap;

        public static Texture2D         Icon
        {
            get
            {
                if (icon == null)
                {
                    icon = ContentFinder<Texture2D>.Get("NaturalLightMap", true);
                }
                return icon;
            }
        } 

        //public static Texture2D Icon()
        //{
        //    return ContentFinder<Texture2D>.Get("NaturalLightMap", true);
        //}

        //public static string IconTip()
        //{
        //    return "NaturalLightMap".Translate();
        //}

        public bool GetCellBool(int index)
        {
            //return !Find.CurrentMap.fogGrid.IsFogged(index) && ShowCell(index);
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

        //public bool ShowCell(Map map, int index)
        //{
        //    return map.GetComponent<MapComp_Windows>().WindowCells.Contains(index) || !map.roofGrid.Roofed(index);
        //}

        public void Update()
        {
            if (!toggleShow) return;
            if (/*Find.TickManager.TicksGame % updateDelay == 0 || */Find.CurrentMap != lastSeenMap)
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
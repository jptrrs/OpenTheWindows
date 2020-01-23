using UnityEngine;
using Verse;

namespace OpenTheWindows
{
    [StaticConstructorOnStartup]
    public class NaturalLightOverlay : ICellBoolGiver
    {
        public Color Color
        {
            get
            {
                return Color.white;
            }
        }

        public bool GetCellBool(int index)
        {
            return !Find.CurrentMap.fogGrid.IsFogged(index) && this.ShowCell(index);
        }

        public bool ShowCell(int index)
        {
            return Find.CurrentMap.GetComponent<MapComp_Windows>().WindowGrid[index] || !Find.CurrentMap.roofGrid.Roofed(index);
        }

        public Color GetCellExtraColor(int index)
        {
            return Color.green;
        }

        public void Update()
        {
            if (toggleShow)
            {
                if (drawer == null)
                {
                    MakeDrawer();
                }
                drawer.MarkForDraw();
                //from heatmap
                int ticksGame = Find.TickManager.TicksGame;
                if (nextUpdateTick == 0 || ticksGame >= nextUpdateTick || Find.CurrentMap != lastSeenMap)
                {
                    drawer.SetDirty();
                    nextUpdateTick = ticksGame + updateDelay;
                    lastSeenMap = Find.CurrentMap;
                }//
                drawer.CellBoolDrawerUpdate();
                PostDraw();
                dirty = false;
                return;
            }
            drawer = null;
            Clear();
        }

        public void MakeDrawer()
        {
            drawer = new CellBoolDrawer(this, Find.CurrentMap.Size.x, Find.CurrentMap.Size.z);
        }

        public virtual void Clear()
        {
        }

        public static void PostDraw()
        {
        }

        public void SetDirty()
        {
            CellBoolDrawer cellBoolDrawer = drawer;
            if (cellBoolDrawer != null)
            {
                cellBoolDrawer.SetDirty();
            }
            dirty = true;
        }

        public static Texture2D Icon()
        {
            return ContentFinder<Texture2D>.Get("NaturalLightMap", true);
        }

        public static string IconTip()
        {
            return "NaturalLightMap".Translate();
        }

        protected float defaultOpacity;

        protected static CellBoolDrawer drawer;

        public static bool toggleShow = false;

        public static bool dirty = true;

        private int nextUpdateTick;

        private Map lastSeenMap;

        private int updateDelay = 1;
    }
}
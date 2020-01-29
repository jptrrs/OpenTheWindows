using UnityEngine;
using Verse;

namespace OpenTheWindows
{
    [StaticConstructorOnStartup]
    public class NaturalLightOverlay : ICellBoolGiver
    {
        public static bool toggleShow = false;

        protected static CellBoolDrawer drawer;

        protected float defaultOpacity;

        private Map lastSeenMap;

        private int nextUpdateTick;

        private int updateDelay = 60;

        public Color Color
        {
            get
            {
                return Color.white;
            }
        }

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
            return !Find.CurrentMap.fogGrid.IsFogged(index) && this.ShowCell(index);
        }

        public Color GetCellExtraColor(int index)
        {
            return Color.green;
        }

        public void MakeDrawer()
        {
            drawer = new CellBoolDrawer(this, Find.CurrentMap.Size.x, Find.CurrentMap.Size.z);
        }

        public bool ShowCell(int index)
        {
            return Find.CurrentMap.GetComponent<MapComp_Windows>().WindowGrid[index] || !Find.CurrentMap.roofGrid.Roofed(index);
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
                //dirty = false;
                return;
            }
            drawer = null;
        }
    }
}
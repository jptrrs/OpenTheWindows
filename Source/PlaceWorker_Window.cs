using System.Linq;
using UnityEngine;
using Verse;

namespace OpenTheWindows
{
    public class PlaceWorker_Window : PlaceWorker
    {
        public override void DrawGhost(ThingDef def, IntVec3 center, Rot4 rot, Color ghostCol, Thing thing = null)
        {
            Map currentMap = Find.CurrentMap;
            IntVec3 start = WindowUtility.FindEnd(center, rot, def.size, false);
            IntVec3 end = WindowUtility.FindEnd(center, rot, def.size, true);
            int reach = WindowUtility.CalculateWindowReach(def.size);
            GenDraw.DrawFieldEdges(WindowUtility.CalculateWindowLightCells(def.size, reach, center, rot, currentMap, start, end).Keys.ToList());
        }
    }
}
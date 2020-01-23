using System.Linq;
using UnityEngine;
using Verse;

namespace OpenTheWindows
{
    public class PlaceWorker_Window : PlaceWorker
    {
        public override void DrawGhost(ThingDef def, IntVec3 center, Rot4 rot, Color ghostCol)
        {
            Map currentMap = Find.CurrentMap;
            GenDraw.DrawFieldEdges(WindowUtility.CalculateWindowLightCells(def, center, rot, currentMap).ToList());
        }
    }
}
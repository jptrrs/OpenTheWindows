using RimWorld;
using System.Collections.Generic;
//using System.Linq;
using UnityEngine;
using Verse;

namespace OpenTheWindows
{
    public class PlaceWorker_Window : PlaceWorker
    {
        public override void DrawGhost(ThingDef def, IntVec3 center, Rot4 rot, Color ghostCol, Thing thing = null)
        {
            List<IntVec3> field;
            if (thing == null || thing is Blueprint)
            {
                IntVec3 start = WindowUtility.FindEnd(center, rot, def.size, false);
                IntVec3 end = WindowUtility.FindEnd(center, rot, def.size, true);
                field = WindowUtility.GetWindowObfuscation(def.size, center, rot, Find.CurrentMap, start, end);
            }
            else
            {
                var window = thing as Building_Window;
                field = window?.EffectArea;
            }
            if (field != null) GenDraw.DrawFieldEdges(field);
        }
    }
}
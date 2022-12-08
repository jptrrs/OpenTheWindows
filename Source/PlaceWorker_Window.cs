using RimWorld;
using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace OpenTheWindows
{
    public class PlaceWorker_Window : PlaceWorker
    {
        public override void DrawGhost(ThingDef def, IntVec3 center, Rot4 rot, Color ghostCol, Thing thing = null)
        {
            List<IntVec3> field = new List<IntVec3>();
            if (thing == null || thing is Blueprint)
            {
                IntVec3 start = WindowUtility.FindEnd(center, rot, def.size, false);
                IntVec3 end = WindowUtility.FindEnd(center, rot, def.size, true);
                field = WindowUtility.GetWindowObfuscation(def.size, center, rot, Find.CurrentMap, start, end);
            }
            else
            {
                Building_Window window = thing as Building_Window;
                if (window?.isFacingSet ?? false)
                {
                    field.AddRange(window.illuminated);
                    field.AddRange(window.view);
                }
            }
            if (field != null) GenDraw.DrawFieldEdges(field);
        }
    }
}
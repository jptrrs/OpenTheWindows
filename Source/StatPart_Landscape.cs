using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;

namespace OpenTheWindows
{
    public class StatPart_Landscape : StatPart
    {
        public override string ExplanationPart(StatRequest req)
        {
            Building_Window window = req.Thing as Building_Window;
            if (req.HasThing && window != null)
            {
                StringBuilder stringBuilder = new StringBuilder();
                stringBuilder.AppendLine("LandscapeToThe".Translate(window.FacingCardinal().ToString().Translate().ToLower()) + ": " + WindowViewBeauty(window));
                return stringBuilder.ToString().TrimEndNewlines();
            }
            return null;
        }

        public override void TransformValue(StatRequest req, ref float value)
        {
            Building_Window window = req.Thing as Building_Window;
            if (req.HasThing && window != null)
            {
                value += WindowViewBeauty(window);
            }
            return;
        }

        public float WindowViewBeauty(Building_Window window)
        {
            if (window != null && window.open && window.isFacingSet && window.view.Count > 0)
            {
                var view = window.view.ToList();
                float result = 0;
                List<Thing> counted = new List<Thing>();
                foreach (IntVec3 c in view)
                {
                    var things = window.Map.thingGrid.ThingsListAt(c).Except(counted);
                    var skipped = things.Where(FilterOut);
                    result += BeautyUtility.CellBeauty(c, window.Map, skipped.Union(counted).ToList());
                    counted.AddRange(things);
                }
                return result;
            }
            return 0f;
        }

        private static Func<Thing, bool> FilterOut = (t) =>
        {
            if (t.def.category == ThingCategory.Building)
            {
                return t is Building_Window || !OpenTheWindowsSettings.BeautyFromBuildings;
            }
            else return false;
        };
    }
}
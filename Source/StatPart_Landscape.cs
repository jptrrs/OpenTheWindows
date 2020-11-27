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

        public static float WindowViewBeauty(Building_Window window)
        {
            if (window != null && window.open && window.isFacingSet && window.effectArea.Count > 0)
            {
                //List<IntVec3> view = new List<IntVec3>();
                //foreach (IntVec3 c in window.effectArea)
                //{
                //    switch (window.Facing)
                //    {
                //        case LinkDirections.Up:
                //            if (c.z > window.Position.z) view.Add(c);
                //            break;

                //        case LinkDirections.Right:
                //            if (c.x > window.Position.x) view.Add(c);
                //            break;

                //        case LinkDirections.Down:
                //            if (c.z < window.Position.z) view.Add(c);
                //            break;

                //        case LinkDirections.Left:
                //            if (c.x < window.Position.x) view.Add(c);
                //            break;

                //        case LinkDirections.None:
                //            break;
                //    }
                //}
                IEnumerable<IntVec3> view = window.effectArea.Except(window.illuminated);
                float result = 0;
                foreach (IntVec3 c in view)
                {
                    List<Thing> viewed = window.Map.thingGrid.ThingsListAt(c).Where(BeautyFilter).ToList();
                    result += BeautyUtility.CellBeauty(c, window.Map, viewed);
                }
                return result;
            }
            return 0f;
        }

        private static Func<Thing, bool> BeautyFilter = (t) =>
        {
            if (t.def.category == ThingCategory.Building)
            {
                return OpenTheWindowsSettings.BeautyFromBuildings && !(t is Building_Window);
            }
            else return true;
        };

        private static List<Thing> ThingsListAt()
        {
            throw new NotImplementedException();
        }
    }
}
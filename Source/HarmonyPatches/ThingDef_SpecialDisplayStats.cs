using HarmonyLib;
using RimWorld;
using System.Collections.Generic;
using Verse;

namespace OpenTheWindows
{
    //Tweaks the report on cover effectiveness for windows.
    //with Owlchemist
    [HarmonyPatch(typeof(ThingDef), nameof(ThingDef.SpecialDisplayStats))]
    public static class ThingDef_SpecialDisplayStats
    {
        //public static void Postfix(ThingDef __instance, ref IEnumerable<StatDrawEntry> __result)
        //{
        //    if (typeof(Building_Window).IsAssignableFrom(__instance.thingClass))
        //    {
        //        StatDrawEntry x = new StatDrawEntry(StatCategoryDefOf.Basics, "CoverEffectiveness".Translate(), WindowUtility.WindowBaseFillPercent.ToStringPercent(), "CoverEffectivenessExplanation".Translate(), 0);
        //        __result = new StatDrawEntry[] { x };
        //    }
        //}

        public static IEnumerable<StatDrawEntry> Postfix(IEnumerable<StatDrawEntry> values, ThingDef __instance)
        {
            foreach (var item in values) yield return item;
            if (__instance.thingClass == typeof(Building_Window))
            {
                yield return new StatDrawEntry(StatCategoryDefOf.Basics, "CoverEffectiveness".Translate(), WindowUtility.WindowBaseFillPercent.ToStringPercent(), "CoverEffectivenessExplanation".Translate(), 0);
            }
        }
    }
}
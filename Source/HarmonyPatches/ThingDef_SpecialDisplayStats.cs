using HarmonyLib;
using RimWorld;
using System.Collections.Generic;
using Verse;

namespace OpenTheWindows
{
    //Tweaks the report on cover effectiveness for windows.
    //fetched from Owlchemist's
    [HarmonyPatch(typeof(ThingDef), nameof(ThingDef.SpecialDisplayStats))]
    public static class ThingDef_SpecialDisplayStats
    {
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
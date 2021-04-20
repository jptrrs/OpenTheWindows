using HarmonyLib;
using RimWorld;
using System.Collections.Generic;
using Verse;

namespace OpenTheWindows
{
    //Tweaks the report on cover effectiveness for windows.
    [HarmonyPatch(typeof(ThingDef), nameof(ThingDef.SpecialDisplayStats))]
    public static class ThingDef_SpecialDisplayStats
    {
        public static void Postfix(ThingDef __instance, ref IEnumerable<StatDrawEntry> __result)
        {
            if (typeof(Building_Window).IsAssignableFrom(__instance.thingClass))
            {
                StatDrawEntry x = new StatDrawEntry(StatCategoryDefOf.Basics, "CoverEffectiveness".Translate(), CoverUtility_BaseBlockChance.WindowBaseFillPercent.ToStringPercent()/*__instance.fillPercent.ToStringPercent()*/, "CoverEffectivenessExplanation".Translate(), 0);
                __result = new StatDrawEntry[] { x };
            }
        }
    }
}
using HarmonyLib;
using RimWorld;
using System;
using Verse;

namespace OpenTheWindows
{
    using static WindowUtility;

    //Changes the window-provided cover when window is open.
    [HarmonyPatch(typeof(CoverUtility), nameof(CoverUtility.BaseBlockChance), new Type[] { typeof(Thing) })]
    public static class CoverUtility_BaseBlockChance
    {
        public static float Postfix(float __result, Thing thing)
        {
            return thing.def.thingClass == typeof(Building_Window) && !FlickUtility.WantsToBeOn(thing) ? WindowBaseFillPercent : __result;
        }
    }

    [HarmonyPatch(typeof(CoverUtility), nameof(CoverUtility.BaseBlockChance), new Type[] { typeof(ThingDef) })]
    public static class CoverUtility_BaseBlockChance_Def
    {
        public static float Postfix(float __result, ThingDef def)
        {
            return def.thingClass == typeof(Building_Window) ? WindowBaseFillPercent : __result;
        }
    }

}
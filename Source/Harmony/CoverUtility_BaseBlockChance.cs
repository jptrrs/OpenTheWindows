using HarmonyLib;
using RimWorld;
using System;
using Verse;

namespace OpenTheWindows
{
    [HarmonyPatch(typeof(CoverUtility), nameof(CoverUtility.BaseBlockChance), new Type[] { typeof(Thing) })]
    public static class CoverUtility_BaseBlockChance
    {
        //private const float BaseBlockChanceWhenFull = 0.75f;

        //public static void Postfix(Thing thing, ref float __result)
        //{
        //    if (thing is Building_Window window)
        //    {
        //        if (!FlickUtility.WantsToBeOn(window)) __result = BaseBlockChanceWhenFull;
        //    }
        //}

        public const float WindowBaseFillPercent = 0.65f;
        public static void Postfix(Thing thing, ref float __result)
        {
            if (thing is Building_Window)
            {
                __result = WindowBaseBlockChance(thing as Building_Window, __result);
            }
        }

        public static float WindowBaseBlockChance(Building_Window window, float result)
        {
            if (!FlickUtility.WantsToBeOn(window))
            {
                return WindowBaseFillPercent;
            }
            else return result;
        }
    }
}
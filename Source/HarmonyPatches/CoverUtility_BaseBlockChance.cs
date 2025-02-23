using HarmonyLib;
using OpenTheWindows;
using RimWorld;
using System;
using Verse;

namespace OpenTheWindows
{
    using static WindowUtility;

    //Changes the window-provided cover when window is open.
    //by Owlchemist
    [HarmonyPatch(typeof(CoverUtility), nameof(CoverUtility.BaseBlockChance), new Type[] { typeof(Thing) })]
    public static class CoverUtility_BaseBlockChance
    {
        //public static void Postfix(Thing thing, ref float __result)
        //{
        //    if (thing is Building_Window)
        //    {
        //        __result = WindowBaseBlockChance(thing as Building_Window, __result);
        //    }
        //}

        //public static float WindowBaseBlockChance(Building_Window window, float result)
        //{
        //    if (!FlickUtility.WantsToBeOn(window))
        //    {
        //        return WindowBaseFillPercent;
        //    }
        //    else return result;
        //}

        public static float Postfix(float __result, Thing thing)
        {
            return thing is Building_Window && !FlickUtility.WantsToBeOn(thing) ? WindowBaseFillPercent : __result;
        }
    }


    [HarmonyPatch(typeof(CoverUtility), nameof(CoverUtility.BaseBlockChance), new Type[] { typeof(ThingDef) })]
    public static class CoverUtility_BaseBlockChance_Def
    {
        //public static void Postfix(ThingDef def, ref float __result)
        //{
        //    if (def.thingClass == typeof(Building_Window))
        //    {
        //        __result = WindowBaseFillPercent;
        //    }
        //}
        public static float Postfix(float __result, ThingDef def)
        {
            return def.thingClass == typeof(Building_Window) ? WindowBaseFillPercent : __result;
        }
    }
}
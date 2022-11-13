using HarmonyLib;
using RimWorld;
using System.Reflection;
using UnityEngine;

namespace OpenTheWindows
{
    /*
    //Modifies beauty need sensitivity. Manually patched if any landscape beautification mod is present.
    public static class NeedBeauty_LevelFromBeauty
    {
        private static float ModifiedBeautyImpactFactor() => 0.1f - (OpenTheWindowsSettings.BeautySensitivityReduction / 10); // original is 0.1f ... testing

        public static void LevelFromBeauty(Need_Beauty __instance, float beauty, ref float __result)
        {
            FieldInfo baseLevelInfo = AccessTools.Field(__instance.def.GetType(), "baseLevel");
            float baseLevel = (float)baseLevelInfo.GetValue(__instance.def);
            __result = Mathf.Clamp01(baseLevel + beauty * ModifiedBeautyImpactFactor());
        }
    }
    */
}
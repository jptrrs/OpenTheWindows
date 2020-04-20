using HarmonyLib;
using RimWorld;
using System.Reflection;
using UnityEngine;

namespace OpenTheWindows
{
    public static class NeedBeauty_LevelFromBeauty
    {
        public static void PatchMod()
        {
            HarmonyPatches.Instance.Patch(AccessTools.Method(typeof(Need_Beauty), "LevelFromBeauty"), null, new HarmonyMethod(typeof(NeedBeauty_LevelFromBeauty), nameof(LevelFromBeauty)), null);
        }

        private static float ModifiedBeautyImpactFactor() => 0.1f - (OpenTheWindowsSettings.BeautySensitivityReduction / 10); // original is 0.1f ... testing

        public static void LevelFromBeauty(Need_Beauty __instance, float beauty, ref float __result)
        {
            FieldInfo baseLevelInfo = AccessTools.Field(__instance.def.GetType(), "baseLevel");
            float baseLevel = (float)baseLevelInfo.GetValue(__instance.def);
            __result = Mathf.Clamp01(baseLevel + beauty * ModifiedBeautyImpactFactor());
        }
    }
}
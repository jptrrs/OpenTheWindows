using Harmony;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using Verse;

namespace OpenTheWindows
{
    [StaticConstructorOnStartup]
    public static class HarmonyPatches
    {
        private static readonly Type patchType = typeof(HarmonyPatches);

        static HarmonyPatches()
        {
            HarmonyInstance.DEBUG = true;
            HarmonyInstance harmonyInstance = HarmonyInstance.Create("JPT_OpenTheWindows");

            harmonyInstance.Patch(original: AccessTools.Method(type: typeof(GlowGrid), name: "GameGlowAt"),
                prefix: null, postfix: new HarmonyMethod(type: patchType, name: nameof(GameGlowAt_Postfix)), transpiler: null);

            harmonyInstance.Patch(original: AccessTools.Method(type: typeof(SectionLayer_LightingOverlay), name: "Regenerate"),
                prefix: new HarmonyMethod(type: patchType, name: nameof(Regenerate_Prefix)), postfix: new HarmonyMethod(type: patchType, name: nameof(Regenerate_Postfix)), transpiler: null);

            harmonyInstance.Patch(original: AccessTools.Method(type: typeof(Need_Outdoors), name: "NeedInterval"),
                prefix: new HarmonyMethod(type: patchType, name: nameof(NeedInterval_Prefix)), postfix: null, transpiler: null);

            try
            {
                ((Action)(() =>
                {
                    if (LoadedModManager.RunningModsListForReading.Any(x => x.Name == "Nature is Beautiful v2.5 [1.0]"))
                    {
                        Log.Message("[OpenTheWindows] Nature is Beautiful detected! Adapting...");

                        harmonyInstance.Patch(original: AccessTools.Method(type: typeof(Need_Beauty), name: "LevelFromBeauty"),
                            prefix: null, postfix: new HarmonyMethod(type: patchType, name: nameof(LevelFromBeauty_Postfix)), transpiler: null);

                        //OpenTheWindowsSettings.IsBeautyOn = true;
                    }
                }))();
            }
            catch (TypeLoadException ex) { }
        }

        public static float WindowFiltering = 0.1f; //Make it variable?

        public static void GameGlowAt_Postfix(GlowGrid __instance, IntVec3 c, ref float __result)
        {
            //Experimenting w/ float light grid
            //FieldInfo mapinfo = AccessTools.Field(typeof(GlowGrid), "map");
            //Map map = (Map)mapinfo.GetValue(__instance);
            //if (__result < 1f && map.GetComponent<MapComp_Windows>().LightGrid[map.cellIndices.CellToIndex(c)] > 0f)
            //{
            //    __result = Mathf.Max(__result, map.skyManager.CurSkyGlow - WindowFiltering);
            //}

            //Experimenting w/ uniform light transmission
            FieldInfo mapinfo = AccessTools.Field(typeof(GlowGrid), "map");
            Map map = (Map)mapinfo.GetValue(__instance);
            if (__result < 1f && map.GetComponent<MapComp_Windows>().WindowGrid[map.cellIndices.CellToIndex(c)])
            {
                __result = Mathf.Max(__result, map.skyManager.CurSkyGlow - WindowFiltering);
            }
        }

        public static RoofDef[] roofGridCopy;

        public static void Regenerate_Prefix(SectionLayer_LightingOverlay __instance)
        {
            FieldInfo sectionInfo = AccessTools.Field(typeof(SectionLayer), "section");
            Section section = (Section)sectionInfo.GetValue(__instance);
            FieldInfo mapinfo = AccessTools.Field(typeof(Section), "map");
            Map map = (Map)mapinfo.GetValue(section);
            MapComp_Windows component = map.GetComponent<MapComp_Windows>();
            FieldInfo roofGridInfo = AccessTools.Field(typeof(RoofGrid), "roofGrid");
            RoofDef[] roofGrid = (RoofDef[])roofGridInfo.GetValue(map.roofGrid);

            roofGridCopy = new List<RoofDef>(roofGrid).ToArray();

            foreach (Building_Window t in component.cachedWindows)
            {
                if (t.Open)
                {
                    t.CastLight();
                    foreach (IntVec3 c in t.OccupiedRect())
                    {
                        roofGrid[map.cellIndices.CellToIndex(c)] = null;
                    }
                    foreach (IntVec3 c in t.illuminated)
                    {
                        roofGrid[map.cellIndices.CellToIndex(c)] = null;
                    }
                }
            }
        }

        public static void Regenerate_Postfix(SectionLayer_LightingOverlay __instance)
        {
            FieldInfo sectionInfo = AccessTools.Field(typeof(SectionLayer), "section");
            Section section = (Section)sectionInfo.GetValue(__instance);
            FieldInfo mapinfo = AccessTools.Field(typeof(Section), "map");
            Map map = (Map)mapinfo.GetValue(section);
            FieldInfo roofGridCInfo = AccessTools.Field(typeof(RoofGrid), "roofGrid");
            roofGridCInfo.SetValue(map.roofGrid, roofGridCopy);
        }

        public static bool NeedInterval_Prefix(object __instance)
        {
            NeedInterval(__instance);
            return true;
        }

        public static void NeedInterval(object __instance)
        {
            PropertyInfo disabledInfo = AccessTools.Property(__instance.GetType(), "Disabled");
            bool disabled = (bool)disabledInfo.GetValue(__instance, null);
            PropertyInfo curLevelInfo = AccessTools.Property(__instance.GetType(), "CurLevel");
            float curLevel = (float)curLevelInfo.GetValue(__instance, null);
            PropertyInfo isFrozenInfo = AccessTools.Property(__instance.GetType().BaseType, "IsFrozen");
            bool isFrozen = (bool)isFrozenInfo.GetValue(__instance, null);
            FieldInfo pawnInfo = AccessTools.Field(typeof(Need_Outdoors), "pawn");
            Pawn pawn = (Pawn)pawnInfo.GetValue(__instance);
            FieldInfo lastEffectiveDeltaInfo = AccessTools.Field(typeof(Need_Outdoors), "lastEffectiveDelta");

            if (disabled)
            {
                curLevelInfo.SetValue(__instance, 1f, null);
                return;
            }
            if (isFrozen)
            {
                return;
            }
            float floor = Minimum_IndoorsThinRoof;
            bool isOutdoors = !pawn.Spawned || pawn.Position.UsesOutdoorTemperature(pawn.Map);
            RoofDef roofDef = (!pawn.Spawned) ? null : pawn.Position.GetRoof(pawn.Map);
            float num;
            if (!isOutdoors) // indoors
            {
                if (roofDef == null)
                {
                    num = Delta_IndoorsNoRoof; //X
                }
                else if (!roofDef.isThickRoof)
                {
                    num = Delta_IndoorsThinRoof;
                }
                else
                {
                    num = Delta_IndoorsThickRoof;
                    floor = 0f;
                }
            }
            else if (roofDef == null)
            {
                num = Delta_OutdoorsNoRoof; //X
            }
            else if (roofDef.isThickRoof)
            {
                num = Delta_OutdoorsThickRoof;
            }
            else
            {
                num = Delta_OutdoorsThinRoof;
            }
            if (pawn.InBed() && num < 0f)
            {
                num *= DeltaFactor_InBed;
            }
            // no natural light penality:
            if (roofDef != null && !pawn.Map.GetComponent<MapComp_Windows>().WindowGrid[pawn.Map.cellIndices.CellToIndex(pawn.Position)])
            {
                if (num < 0f) num *= DeltaFactor_NoNaturalLight();
                else num /= DeltaFactor_NoNaturalLight();
                //Log.Message("NeedInterval Outdoors postfixed by " + DeltaFactor_NoNaturalLight() + " to " + num);
            }
            //
            num *= 0.0025f;
            float _curLevel = curLevel;
            if (num < 0f)
            {
                curLevelInfo.SetValue(__instance, Mathf.Min(curLevel, Mathf.Max(curLevel + num, floor)), null);
            }
            else
            {
                curLevelInfo.SetValue(__instance, Mathf.Min(curLevel + num, 1f), null);
            }
            lastEffectiveDeltaInfo.SetValue(__instance, curLevel - _curLevel);
        }

        private const float Delta_IndoorsThickRoof = -0.45f;
        private const float Delta_OutdoorsThickRoof = -0.4f;
        private const float Delta_IndoorsThinRoof = -0.32f;
        private const float Minimum_IndoorsThinRoof = 0.2f; //not a variation, but the need floor!
        private const float Delta_OutdoorsThinRoof = 1f;
        private const float Delta_IndoorsNoRoof = 5f;
        private const float Delta_OutdoorsNoRoof = 8f;
        private const float DeltaFactor_InBed = 0.2f;

        //modded
        private static float DeltaFactor_NoNaturalLight() => OpenTheWindowsSettings.IndoorsNoNaturalLightPenalty; //indoors accelerated degradation when not under windows

        public static void LevelFromBeauty_Postfix(Need_Beauty __instance, float beauty, ref float __result)
        {
            FieldInfo baseLevelInfo = AccessTools.Field(__instance.def.GetType(), "baseLevel");
            float baseLevel = (float)baseLevelInfo.GetValue(__instance.def);
            __result = Mathf.Clamp01(baseLevel + beauty * ModifiedBeautyImpactFactor());
            //Log.Message("LevelFromBeauty postfixed by "+ ModifiedBeautyImpactFactor() + " to " + __result);
        }

        private static float ModifiedBeautyImpactFactor() => 0.1f - (OpenTheWindowsSettings.BeautySensitivityReduction / 10); // original is 0.1f ... testing
    }
}
using Harmony;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using UnityEngine;
using Verse;

namespace OpenTheWindows
{
    [StaticConstructorOnStartup]
    public static class HarmonyPatches
    {
        private static readonly Type patchType = typeof(HarmonyPatches);

        public static bool ExpandedRoofing = false;

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

            harmonyInstance.Patch(original: AccessTools.Method(type: typeof(CompFlickable), name: "DoFlick"),
                prefix: null, postfix: new HarmonyMethod(type: patchType, name: nameof(DoFlick_Postfix)), transpiler: null);

            harmonyInstance.Patch(original: AccessTools.Method(type: typeof(CompFlickable), name: "PostExposeData", new Type[] { }),
                prefix: new HarmonyMethod(type: patchType, name: nameof(PostExposeData_Prefix)), postfix: null, transpiler: null);

            harmonyInstance.Patch(original: AccessTools.Method(type: typeof(CoverUtility), name: "BaseBlockChance", new Type[] { typeof(Thing) }), prefix: null, postfix: new HarmonyMethod(type: patchType, name: nameof(BaseBlockChance_Postfix)), transpiler: null);

            harmonyInstance.Patch(original: AccessTools.Method(type: typeof(GenGrid), name: "CanBeSeenOver", new Type[] { typeof(Building) }), prefix: null, postfix: new HarmonyMethod(type: patchType, name: nameof(CanBeSeenOver_Postfix)), transpiler: null);

            harmonyInstance.Patch(original: AccessTools.Method(type: typeof(ThingDef), name: "SpecialDisplayStats"), 
                prefix: null, postfix: new HarmonyMethod(type: patchType, name: nameof(SpecialDisplayStats_Postfix)), transpiler: null);

            if (LoadedModManager.RunningModsListForReading.Any(x => x.Name.Contains("Nature is Beautiful")))
            {
                Log.Message("[OpenTheWindows] Nature is Beautiful detected! Integrating...");

                harmonyInstance.Patch(original: AccessTools.Method(type: typeof(Need_Beauty), name: "LevelFromBeauty"),
                    prefix: null, postfix: new HarmonyMethod(type: patchType, name: nameof(LevelFromBeauty_Postfix)), transpiler: null);

                OpenTheWindowsSettings.IsBeautyOn = true;
            }
            
            if (LoadedModManager.RunningModsListForReading.Any(x => x.Name == "Dubs Skylights"))
            {
                Log.Message("[OpenTheWindows] Dubs Skylights detected! Integrating...");

                harmonyInstance.Patch(original: AccessTools.Method("Dubs_Skylight.MapComp_Skylights:RegenGrid"),
                    prefix: null, postfix: new HarmonyMethod(type: patchType, name: nameof(MapComp_Skylights_RegenGrid_Postfix)), transpiler: null);
            }

        //    if (AccessTools.TypeByName("ExpandedRoofing.HarmonyPatches") is Type expandedRoofingType)
        //    {
        //        Log.Message("[OpenTheWindows] Expanded Roofing detected! Integrating...");

        //        harmonyInstance.Patch(original: AccessTools.Method(expandedRoofingType,"TransparentRoofLightingOverlayFix"),
        //            prefix: new HarmonyMethod(type: patchType, name: nameof(TransparentRoofBlock_Prefix)), postfix: null, transpiler: null);

        //        harmonyInstance.Patch(original: AccessTools.Method(expandedRoofingType,"PlantLightingFix"),
        //            prefix: new HarmonyMethod(type: patchType, name: nameof(TransparentRoofBlock_Prefix)), postfix: null, transpiler: null);
        //    }
        }

        //[HarmonyBefore(new string[] { "rimworld.whyisthat.expandedroofing.main" })]
        //public static void TransparentRoofBlock_Prefix()
        //{
        //    Log.Message("Blocking TransparentRoof");
        //}

        //public static void TransparentRoofBlock_Postfix()
        //{
        //    Log.Message("TransparentRoof Postfix!");
        //}

        public static float WindowFiltering = 0.1f;

        public static void GameGlowAt_Postfix(GlowGrid __instance, IntVec3 c, ref float __result)
        {
            FieldInfo mapinfo = AccessTools.Field(typeof(GlowGrid), "map");
            Map map = (Map)mapinfo.GetValue(__instance);
            if (__result < 1f && map.GetComponent<MapComp_Windows>().WindowGrid[map.cellIndices.CellToIndex(c)])
            {
                __result = Mathf.Max(__result, map.skyManager.CurSkyGlow - WindowFiltering);
            }
        }

        public static RoofDef[] roofGridCopy;

        [HarmonyAfter(new string[] { "Dubwise.Dubs_Skylights" })]
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

            if (ExpandedRoofing)
            {
                Log.Message("ExpandedRoofing is on, expanding roofGrid.");
                Type type = AccessTools.TypeByName("ExpandedRoofing.RoofDefOf");
                FieldInfo roofTransparentInfo = AccessTools.Field(type, "RoofTransparent");
                RoofDef roofTransparent = (RoofDef)roofTransparentInfo.GetValue(map.roofGrid);

                foreach (RoofDef r in roofGrid)
                {
                    if (r == roofTransparent)
                    {
                        roofGrid.AddToArray(r);
                        Log.Message("Added transparent roof at " + r.index);
                    }
                }
            }

            foreach (Building_Window t in component.cachedWindows)
            {
                if (t.open)
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

        [HarmonyBefore(new string[] { "Dubwise.Dubs_Skylights" })]
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
            }   // no natural light penality:
            if (roofDef != null && !pawn.Map.GetComponent<MapComp_Windows>().WindowGrid[pawn.Map.cellIndices.CellToIndex(pawn.Position)])
            {
                if (num < 0f) num *= DeltaFactor_NoNaturalLight();
                else num /= DeltaFactor_NoNaturalLight();
            } //
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

        private static float DeltaFactor_NoNaturalLight() => OpenTheWindowsSettings.IndoorsNoNaturalLightPenalty; //indoors accelerated degradation when not under windows

        public static void LevelFromBeauty_Postfix(Need_Beauty __instance, float beauty, ref float __result)
        {
            FieldInfo baseLevelInfo = AccessTools.Field(__instance.def.GetType(), "baseLevel");
            float baseLevel = (float)baseLevelInfo.GetValue(__instance.def);
            __result = Mathf.Clamp01(baseLevel + beauty * ModifiedBeautyImpactFactor());
        }

        private static float ModifiedBeautyImpactFactor() => 0.1f - (OpenTheWindowsSettings.BeautySensitivityReduction / 10); // original is 0.1f ... testing

        public static void DoFlick_Postfix(CompFlickable __instance)
        {
            if (__instance is CompWindow compWindow)
            {
                compWindow.SwitchIsOn = !compWindow.SwitchIsOn;
            }
        }

        public static bool PostExposeData_Prefix(CompFlickable __instance, bool ___switchOnInt, bool ___wantSwitchOn)
        {
            if (__instance is CompWindow compWindow)
            {
                if (Scribe.mode == LoadSaveMode.PostLoadInit)
                {
                    compWindow.SetupState();
                }
                return false;
            }
            return true;
        }

        public static void BaseBlockChance_Postfix(Thing thing, ref float __result)
        {
            if (thing is Building_Window)
            {
                __result = WindowBaseBlockChance(thing as Building_Window, __result);
            }
        }

        public static float WindowBaseBlockChance(Building_Window window, float result)
        {
            if (FlickUtility.WantsToBeOn(window))
            {
                return WindowBaseFillPercent;
            }
            else return result;
        }

        private const float WindowBaseFillPercent = 0.7f;

        public static void CanBeSeenOver_Postfix(Building b, ref bool __result)
        {
            if (b is Building_Window && FlickUtility.WantsToBeOn(b))
            {
                __result = true;
            }
        }

        //public static void Fillage_Prefix(ref ThingDef __instance) //This can intercept a method when it calls for a specific property!
        //{
        //    StackTrace stackTrace = new StackTrace();
        //    MethodBase target = AccessTools.Method(type: typeof(ThingDef), name: "SpecialDisplayStats") as MethodBase;
        //    //if (stackTrace.GetFrame(3).GetMethod() == target) Log.Message("eureka!"); //lighter load, but gotta know the method position on the stack
        //    foreach (StackFrame sf in stackTrace.GetFrames())
        //    {
        //        //Log.Message(sf.GetMethod().Name);
        //        if (sf.GetMethod().Equals(target)) Log.Message("eureka!");
        //        //{
        //        //    Log.Message(sf.GetMethod().Name);
        //        //}
        //    }
        //}

        public static void SpecialDisplayStats_Postfix(ThingDef __instance, ref IEnumerable<StatDrawEntry> __result)
        {
            if (typeof(Building_Window).IsAssignableFrom(__instance.thingClass))
            {
                StatDrawEntry x = new StatDrawEntry(StatCategoryDefOf.Basics, "CoverEffectiveness".Translate(), WindowBaseFillPercent.ToStringPercent(), 0, string.Empty)
                {
                    overrideReportText = "CoverEffectivenessExplanation".Translate()
                };
                StatDrawEntry[] y = new StatDrawEntry[] { x };
                __result = y;
            }
        }

        public static void MapComp_Skylights_RegenGrid_Postfix(MapComponent __instance)
        {
            Type type = AccessTools.TypeByName("Dubs_Skylight.MapComp_Skylights");
            FieldInfo skylightGridinfo = AccessTools.Field(type, "SkylightGrid");
            bool[] skyLightGrid = (bool[])skylightGridinfo.GetValue(__instance);
            __instance.map.GetComponent<MapComp_Windows>().WindowGrid.AddRangeToArray(skyLightGrid);
        }
    }
}

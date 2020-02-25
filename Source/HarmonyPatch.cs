using Harmony;
using RimWorld;
using RimWorld.Planet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using Verse;

namespace OpenTheWindows
{
    [StaticConstructorOnStartup]
    public static class HarmonyPatches
    {
        private static readonly Type patchType = typeof(HarmonyPatches);

        public static bool ExpandedRoofing = false;

        public static bool DubsSkylights = false;

        static HarmonyPatches()
        {
            //HarmonyInstance.DEBUG = true;
            HarmonyInstance harmonyInstance = HarmonyInstance.Create("JPT_OpenTheWindows");

            //template:
            //harmonyInstance.Patch(original: AccessTools.Method(type: typeof(?), name: "?"),
            //prefix: null, postfix: new HarmonyMethod(type: patchType, name: nameof(?)), transpiler: null);

            harmonyInstance.Patch(AccessTools.Method(typeof(GlowGrid), "GameGlowAt"),
                null, new HarmonyMethod(patchType, nameof(GameGlowAt_Postfix)), null);

            harmonyInstance.Patch(AccessTools.Method(typeof(SectionLayer_LightingOverlay), "Regenerate"),
                new HarmonyMethod(patchType, nameof(Regenerate_Prefix)), new HarmonyMethod(patchType, nameof(Regenerate_Postfix)), null);

            harmonyInstance.Patch(AccessTools.Method(typeof(Need_Outdoors), "NeedInterval"),
                new HarmonyMethod(patchType, nameof(NeedInterval_Prefix)), null, null);

            harmonyInstance.Patch(AccessTools.Method(typeof(CompFlickable), "DoFlick"),
                null, new HarmonyMethod(patchType, nameof(DoFlick_Postfix)), null);

            harmonyInstance.Patch(AccessTools.Method(typeof(CompFlickable), "PostExposeData", new Type[] { }),
                new HarmonyMethod(patchType, nameof(PostExposeData_Prefix)), null, null);

            harmonyInstance.Patch(AccessTools.Method(typeof(CoverUtility), "BaseBlockChance", new Type[] { typeof(Thing) }), null, new HarmonyMethod(patchType, nameof(BaseBlockChance_Postfix)), null);

            harmonyInstance.Patch(AccessTools.Method(typeof(GenGrid), "CanBeSeenOver", new Type[] { typeof(Building) }), null, new HarmonyMethod(patchType, nameof(CanBeSeenOver_Postfix)), null);

            harmonyInstance.Patch(AccessTools.Method(typeof(ThingDef), "SpecialDisplayStats"),
                null, new HarmonyMethod(patchType, nameof(SpecialDisplayStats_Postfix)), null);

            harmonyInstance.Patch(AccessTools.Method(typeof(MapInterface), "MapInterfaceUpdate"),
                null, new HarmonyMethod(patchType, nameof(MapInterfaceUpdate_Postfix)), null);

            harmonyInstance.Patch(AccessTools.Method(typeof(PlaySettings), "DoPlaySettingsGlobalControls"),
                null, new HarmonyMethod(patchType, nameof(DoPlaySettingsGlobalControls_Postfix)), null);

            harmonyInstance.Patch(AccessTools.Method(typeof(ThingGrid), "RegisterInCell"),
                new HarmonyMethod(patchType, nameof(RegisterInCell_postfix)), null, null);

            harmonyInstance.Patch(AccessTools.Method(typeof(ThingGrid), "DeregisterInCell"),
                new HarmonyMethod(patchType, nameof(RegisterInCell_postfix)), null, null);

            harmonyInstance.Patch(AccessTools.Method(typeof(RoofGrid), "SetRoof"),
                new HarmonyMethod(patchType, nameof(SetRoof_postfix)), null, null);

            if (OpenTheWindowsSettings.LinkVents)
            {
                harmonyInstance.Patch(AccessTools.Method(typeof(Building), "SpawnSetup"),
                    null, new HarmonyMethod(patchType, nameof(Linking_SpawnSetup_postfix)), null);

                harmonyInstance.Patch(AccessTools.Method(typeof(Building), "DeSpawn"),
                    new HarmonyMethod(patchType, nameof(Linking_DeSpawn_prefix)), null, null);
            }

            if (LoadedModManager.RunningModsListForReading.Any(x => x.Name.Contains("Nature is Beautiful")))
            {
                Log.Message("[OpenTheWindows] Nature is Beautiful detected! Integrating...");

                harmonyInstance.Patch(AccessTools.Method(typeof(Need_Beauty), "LevelFromBeauty"),
                    null, new HarmonyMethod(patchType, nameof(LevelFromBeauty_Postfix)), null);

                OpenTheWindowsSettings.IsBeautyOn = true;
            }

            if (LoadedModManager.RunningModsListForReading.Any(x => x.Name == "Dubs Skylights"))
            {
                Log.Message("[OpenTheWindows] Dubs Skylights detected! Integrating...");
                DubsSkylights = true;

                harmonyInstance.Patch(AccessTools.Method("Dubs_Skylight.Patch_GameGlowAt:Postfix"),
                    new HarmonyMethod(patchType, nameof(Patch_Inhibitor_Prefix)), null, null);

                harmonyInstance.Patch(AccessTools.Method("Dubs_Skylight.Patch_SectionLayer_LightingOverlay_Regenerate:Prefix"),
                    new HarmonyMethod(patchType, nameof(Patch_Inhibitor_Prefix)), null, null);

                harmonyInstance.Patch(AccessTools.Method("Dubs_Skylight.Patch_SectionLayer_LightingOverlay_Regenerate:Postfix"),
                    new HarmonyMethod(patchType, nameof(Patch_Inhibitor_Prefix)), null, null);

                harmonyInstance.Patch(AccessTools.Method("Dubs_Skylight.MapComp_Skylights:RegenGrid"),
                    null, new HarmonyMethod(patchType, nameof(RegenGrid_Postfix)), null);
            }

            if (AccessTools.TypeByName("ExpandedRoofing.HarmonyPatches") is Type expandedRoofingType)
            {
                Log.Message("[OpenTheWindows] Expanded Roofing detected! Integrating...");
                ExpandedRoofing = true;

                harmonyInstance.Patch(AccessTools.Method("ExpandedRoofing.CompCustomRoof:PostSpawnSetup"),
                    null, new HarmonyMethod(patchType, nameof(RegenGrid_Postfix)), null);
            }
        }

        public static bool Patch_Inhibitor_Prefix(/*MethodBase __originalMethod*/)
        {
            //Log.Message(__originalMethod.Name + " was inhibited");
            return false;
        }

        private static float WindowFiltering() => OpenTheWindowsSettings.LightTransmission;

        public static void GameGlowAt_Postfix(IntVec3 c, ref float __result)
        {
            Map map = Find.CurrentMap;
            try
            { 
                if (__result < 1f && map.GetComponent<MapComp_Windows>().WindowGrid[map.cellIndices.CellToIndex(c)])
                {
                    float x = Mathf.Max(0f, map.skyManager.CurSkyGlow * WindowFiltering());
                    __result = Mathf.Max(__result, x);
                }
            }
            catch (IndexOutOfRangeException e)
            {
                //Log.Message(map.ToString() + " cell " + c + " is out of range on GameGlowAt");
            }
        }

        public static RoofDef[] roofGridCopy;

        //[HarmonyAfter(new string[] { "Dubwise.Dubs_Skylights" })]
        public static void Regenerate_Prefix()
        {
            Map map = Find.CurrentMap;
            MapComp_Windows component = map.GetComponent<MapComp_Windows>();
            FieldInfo roofGridInfo = AccessTools.Field(typeof(RoofGrid), "roofGrid");
            RoofDef[] roofGrid = (RoofDef[])roofGridInfo.GetValue(map.roofGrid);
            roofGridCopy = new List<RoofDef>(roofGrid).ToArray();
            foreach (int i in (from t in component.WindowGrid.Select((s, i) => new { s, i })
                               where t.s is true
                               select t.i).ToList())
            {
                roofGrid[i] = null;
            }
        }

        //[HarmonyBefore(new string[] { "Dubwise.Dubs_Skylights" })]
        public static void Regenerate_Postfix()
        {
            Map map = Find.CurrentMap;
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
                    num = Delta_IndoorsNoRoof;
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
                num = Delta_OutdoorsNoRoof;
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
        //    MethodBase target = AccessTools.Method(typeof(ThingDef), "SpecialDisplayStats") as MethodBase;
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

        public static void MapInterfaceUpdate_Postfix()
        {
            if (Find.CurrentMap == null || WorldRendererUtility.WorldRenderedNow)
            {
                return;
            }
            NaturalLightOverlay naturalLightMap = new NaturalLightOverlay();
            naturalLightMap.Update();
        }

        public static void DoPlaySettingsGlobalControls_Postfix(WidgetRow row, bool worldView)
        {
            if (worldView)
            {
                return;
            }
            if (row == null || NaturalLightOverlay.Icon() == null)
            {
                return;
            }
            row.ToggleableIcon(ref NaturalLightOverlay.toggleShow, NaturalLightOverlay.Icon(), NaturalLightOverlay.IconTip(), null, null);
        }

        public static void RegenGrid_Postfix()
        {
            Find.CurrentMap.GetComponent<MapComp_Windows>().RegenGrid();
        }

        public static void RegisterInCell_postfix(Thing t, IntVec3 c)
        {
            if (t is Building && t.def.passability == Traversability.Impassable)
            {
                MapComp_Windows mapComp = t.Map.GetComponent<MapComp_Windows>();
                if (mapComp != null && mapComp.WindowScanGrid[t.Map.cellIndices.CellToIndex(c)] > 0)
                {
                    mapComp.updateRequest = true;
                }
            }
        }

        public static void SetRoof_postfix(RoofGrid __instance, IntVec3 c, RoofDef def)
        {
            FieldInfo mapInfo = AccessTools.Field(typeof(RoofGrid), "map");
            Map map = (Map)mapInfo.GetValue(__instance);
            MapComp_Windows mapComp = map.GetComponent<MapComp_Windows>();
            if (mapComp != null && mapComp.WindowScanGrid[map.cellIndices.CellToIndex(c)] > 0)
            {
                mapComp.roofUpdateRequest = true;
            }
        }

        public static void Linking_SpawnSetup_postfix(Building __instance, Map map)
        {
            if (__instance.def.graphicData != null && __instance.def.graphicData.linkType == LinkDrawerType.None && __instance.def.graphicData.linkFlags == (LinkFlags.Rock | LinkFlags.Wall))
            {
                map.linkGrid.Notify_LinkerCreatedOrDestroyed(__instance);
                map.mapDrawer.MapMeshDirty(__instance.Position, MapMeshFlag.Things, true, false);
            }
        }

        public static void Linking_DeSpawn_prefix(Building __instance)
        {
            if (__instance.def.graphicData != null && __instance.def.graphicData.linkType == LinkDrawerType.None && __instance.def.graphicData.linkFlags == (LinkFlags.Rock | LinkFlags.Wall))
            {
                Map map = __instance.Map;
                map.thingGrid.Deregister(__instance, false);
                map.linkGrid.Notify_LinkerCreatedOrDestroyed(__instance);
                map.mapDrawer.MapMeshDirty(__instance.Position, MapMeshFlag.Things, true, false);
            }
        }
    }
}

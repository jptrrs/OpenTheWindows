using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using Verse;

namespace OpenTheWindows
{
    [StaticConstructorOnStartup]
    public static class HarmonyPatcher
    {
        public static readonly Type patchType = typeof(HarmonyPatcher);
        public static Type
            Building_Skylight,
            LocksType;
        public static bool
            BetterPawnControl = false,
            Blueprints = false,
            DubsSkylights = false,
            ExpandedRoofing = false;
        public static Harmony _instance = null;
        public static ModContentPack ExpandedRoofingMod;
        public static List<RoofDef> transparentRoofs = new List<RoofDef>();
        public static Harmony Instance
        {
            get
            {
                if (_instance == null)
                    _instance = new Harmony("JPT.OpenTheWindows");
                return _instance;
            }
        }

        static HarmonyPatcher()
        {
            Instance.PatchAll();
            //Harmony.DEBUG = true;

            if (LoadedModManager.RunningModsListForReading.Any(x => x.PackageIdPlayerFacing.StartsWith("Dubwise.DubsSkylights")))
            {
                Log.Message("[OpenTheWindows] Dubs Skylights detected! Integrating...");
                DubsSkylights = true;
                Instance.Patch(AccessTools.Method("Dubs_Skylight.Patch_GameGlowAt:Postfix"), new HarmonyMethod(patchType, nameof(Patch_Inhibitor_Prefix)), null, null);
                Instance.Patch(AccessTools.Method("Dubs_Skylight.Patch_SectionLayer_LightingOverlay_Regenerate:Prefix"), new HarmonyMethod(patchType, nameof(Patch_Inhibitor_Prefix)), null, null);
                Instance.Patch(AccessTools.Method("Dubs_Skylight.Patch_SectionLayer_LightingOverlay_Regenerate:Postfix"), new HarmonyMethod(patchType, nameof(Patch_Inhibitor_Prefix)), null, null);
                Building_Skylight = AccessTools.TypeByName("Dubs_Skylight.Building_skyLight");
            }

            ExpandedRoofingMod = LoadedModManager.RunningModsListForReading.FirstOrDefault(x => x.PackageIdPlayerFacing.StartsWith("wit.expandedroofing"));
            if (ExpandedRoofingMod != null)
            {
                Log.Message("[OpenTheWindows] Expanded Roofing detected! Integrating...");
                transparentRoofs.Add(DefDatabase<RoofDef>.GetNamed("RoofTransparent"));
                if (transparentRoofs.NullOrEmpty()) Log.Error($"[OpenTheWindows] No transparent roofs detected, Expanded Roofing integration failed!");
                else ExpandedRoofing = true;
            }

            //Raise the Roof integration -Needs more work!
            ////if (LoadedModManager.RunningModsListForReading.Any(x => x.PackageIdPlayerFacing.StartsWith("machine.rtr")))
            //if (AccessTools.TypeByName("RaiseTheRoof.Patches") is Type rtrType)
            //{
            //    Log.Message($"[OpenTheWindows] Raise The Roof detected! Integrating...");
            //    Instance.Patch(AccessTools.Method(AccessTools.Inner(rtrType, "Patch_SectionLayer_LightingOverlay_Regenerate"), "Prefix"), new HarmonyMethod(patchType, nameof(Patch_Inhibitor_Prefix)), null, null);
            //    Instance.Patch(AccessTools.Method(AccessTools.Inner(rtrType, "Patch_GlowGrid_GameGlowAt"), "Prefix"), new HarmonyMethod(patchType, nameof(Patch_Inhibitor_Prefix)), null, null);
            //}

            List<string> beautyMods = new List<string>() { "JPT.CustomNaturalBeauty", "zhrocks11.NatureIsBeautiful", "Meltup.BeautifulOutdoors" };
            if (LoadedModManager.RunningModsListForReading.Any(x => x.Name.Contains("Nature is Beautiful") || beautyMods.Select(id => x.PackageIdPlayerFacing.StartsWith(id)).Any()))
            {
                Log.Message("[OpenTheWindows] Landscape beautification mod detected! Integrating...");
                OpenTheWindowsSettings.IsBeautyOn = true;
                Instance.Patch(AccessTools.Method(typeof(Need_Beauty), "LevelFromBeauty"), null, new HarmonyMethod(typeof(NeedBeauty_LevelFromBeauty), nameof(NeedBeauty_LevelFromBeauty.LevelFromBeauty)), null);
            }

            if (LoadedModManager.RunningModsListForReading.Any(x => x.PackageIdPlayerFacing.StartsWith("VouLT.BetterPawnControl")))
            {
                Log.Message("[OpenTheWindows] Better Pawn Control detected! Integrating...");
                BetterPawnControl = true;
                Type AlertManagerType = AccessTools.TypeByName("BetterPawnControl.AlertManager");
                Instance.Patch(AccessTools.Method(AlertManagerType, "LoadState"), null, new HarmonyMethod(typeof(AlertManager_LoadState), nameof(AlertManager_LoadState.LoadState_Postfix)));
                Instance.CreateReversePatcher(AccessTools.PropertyGetter(AlertManagerType, "OnAlert"), new HarmonyMethod(AccessTools.Method(typeof(AlertManagerProxy), nameof(AlertManagerProxy.OnAlert)))).Patch();
            }

            if (LoadedModManager.RunningModsListForReading.Any(x => x.PackageIdPlayerFacing.StartsWith("fluffy.blueprints")))
            {
                Log.Message("[OpenTheWindows] Blueprints detected! Adapting...");
                Blueprints = true;
                Instance.Patch(AccessTools.Method("Blueprints.BuildableInfo:DrawGhost"), new HarmonyMethod(typeof(BuildableInfo_DrawGhost), nameof(BuildableInfo_DrawGhost.DrawGhost_Prefix)), new HarmonyMethod(typeof(BuildableInfo_DrawGhost), nameof(BuildableInfo_DrawGhost.DrawGhost_Postfix)));
            }

            if (LoadedModManager.RunningModsListForReading.Any(x => x.PackageIdPlayerFacing.StartsWith("avius.locks")))
            {
                Log.Message("[OpenTheWindows] Locks detected! Adapting...");
                LocksType = AccessTools.TypeByName("Locks.LockGizmo");
            }
        }

        public static bool Patch_Inhibitor_Prefix()
        {
            return false;
        }

        //public static void RegenGrid_Postfix()
        //{
        //    Find.CurrentMap.GetComponent<MapComp_Windows>().RegenGrid();
        //}
    }
}
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
        public static Harmony _instance = null;
        public static bool
            BetterPawnControl = false,
            Blueprints = false,
            DubsSkylights = false,
            RaiseTheRoof = false;
        public static Type
            Building_Skylight,
            LocksType;
        public static List<RoofDef> TransparentRoofsList = new List<RoofDef>();
        const string roofsFailed = "[OpenTheWindows] ...but no roofs detected! Integration failed.";
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

            if (LoadedModManager.RunningModsListForReading.Any(x => x.PackageIdPlayerFacing.StartsWith("wit.expandedroofing")))
            {
                Log.Message("[OpenTheWindows] Expanded Roofing detected! Integrating...");
                TransparentRoofsList.Add(DefDatabase<RoofDef>.GetNamed("RoofTransparent"));
                if (!TransparentRoofs) Log.Error(roofsFailed);
            }

            //if (LoadedModManager.RunningModsListForReading.Any(x => x.PackageIdPlayerFacing.StartsWith("machine.rtr")))
            if (AccessTools.TypeByName("RaiseTheRoof.Patches") is Type rtrType)
            {
                Log.Message($"[OpenTheWindows] Raise the roof detected! Integrating...");
                RaiseTheRoof = true;
                TransparentRoofsList.AddRange(DefDatabase<RoofDef>.AllDefsListForReading.Where(x => x.defName.StartsWith("RTR_RoofTransparent")));
                if (!TransparentRoofs) Log.Error(roofsFailed);
            }

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

            if (LoadedModManager.RunningModsListForReading.Any(x => x.PackageIdPlayerFacing.StartsWith("roolo.giddyupcore")))
            {
                Log.Message("[OpenTheWindows] Giddy-up! detected! Adapting...");
                Instance.Patch(AccessTools.Method("GiddyUpCore.Jobs.JobDriver_Mount:letMountParticipate"), null, new HarmonyMethod(typeof(JobDriver_Mount_letMountParticipate), nameof(JobDriver_Mount_letMountParticipate.letMountParticipate_Postfix)));
            }
        }

        public static Harmony Instance
        {
            get
            {
                if (_instance == null)
                    _instance = new Harmony("JPT.OpenTheWindows");
                return _instance;
            }
        }

        public static bool TransparentRoofs => !TransparentRoofsList.NullOrEmpty();
        public static bool Patch_Inhibitor_Prefix()
        {
            return false;
        }
    }
}
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Globalization; 
using Verse;

namespace OpenTheWindows
{
    [StaticConstructorOnStartup]
    public static class HarmonyPatcher
    {
        public static Harmony _instance = null;
        public static bool BetterPawnControl, Blueprints, DubsSkylights, RaiseTheRoof;
        public static Type Building_Skylight, LocksType;
        public static List<RoofDef> TransparentRoofsList = new List<RoofDef>();
        static HarmonyPatcher()
        {
            Instance.PatchAll();

            var list = LoadedModManager.RunningModsListForReading;
            int length = list.Count;
            for (int i = 0; i < length; i++)
            {
                string name = list[i].packageIdPlayerFacingInt;
                if (name == "Dubwise.DubsSkylights")
                {
                    DubsSkylights = true;
                    if (Prefs.DevMode) Log.Message("[Windows] Dubs Skylights detected! Integrating...");
                    Instance.Patch(AccessTools.Method("Dubs_Skylight.Patch_GameGlowAt:Postfix"), new HarmonyMethod(typeof(HarmonyPatcher), nameof(Patch_Inhibitor_Prefix)), null, null);
                    Instance.Patch(AccessTools.Method("Dubs_Skylight.Patch_SectionLayer_LightingOverlay_Regenerate:Prefix"), new HarmonyMethod(typeof(HarmonyPatcher), nameof(Patch_Inhibitor_Prefix)), null, null);
                    Instance.Patch(AccessTools.Method("Dubs_Skylight.Patch_SectionLayer_LightingOverlay_Regenerate:Postfix"), new HarmonyMethod(typeof(HarmonyPatcher), nameof(Patch_Inhibitor_Prefix)), null, null);
                    Building_Skylight = AccessTools.TypeByName("Dubs_Skylight.Building_skyLight");
                }
                else if (name == "wit.expandedroofing")
                {
                    if (Prefs.DevMode) Log.Message("[Windows] Expanded Roofing detected! Integrating...");
                    TransparentRoofsList.Add(DefDatabase<RoofDef>.GetNamed("RoofTransparent"));
                    if (Prefs.DevMode && !TransparentRoofs) Log.Error("[Windows] ...but no roofs detected! Integration failed.");
                }
                else if (name == "VouLT.BetterPawnControl")
                {
                    if (Prefs.DevMode) Log.Message("[Windows] Better Pawn Control detected! Integrating...");
                    BetterPawnControl = true;
                    Type AlertManagerType = AccessTools.TypeByName("BetterPawnControl.AlertManager");
                    Instance.Patch(AccessTools.Method(AlertManagerType, "LoadState"), null, new HarmonyMethod(typeof(AlertManager_LoadState), nameof(AlertManager_LoadState.LoadState_Postfix)));
                    Instance.CreateReversePatcher(AccessTools.PropertyGetter(AlertManagerType, "OnAlert"), new HarmonyMethod(AccessTools.Method(typeof(AlertManagerProxy), nameof(AlertManagerProxy.OnAlert)))).Patch();
                }
                else if (name == "avius.locks")
                {
                    if (Prefs.DevMode) Log.Message("[Windows] Locks detected! Adapting...");
                    LocksType = AccessTools.TypeByName("Locks.LockGizmo");
                }
                else if (name == "roolo.giddyupcore")
                {
                    if (Prefs.DevMode) Log.Message("[Windows] Giddy-up! detected! Adapting...");
                    Instance.Patch(AccessTools.Method("GiddyUpCore.Jobs.JobDriver_Mount:letMountParticipate"), null, new HarmonyMethod(typeof(JobDriver_Mount_letMountParticipate), nameof(JobDriver_Mount_letMountParticipate.letMountParticipate_Postfix)));
                }
                else if (name == "Owlchemist.GiddyUp")
                {
                    if (Prefs.DevMode) Log.Message("[Windows] Giddy-up 2 detected! Adapting...");
                    Instance.Patch(AccessTools.Method("GiddyUp.Jobs.JobDriver_Mount:LetMountParticipate"), null, new HarmonyMethod(typeof(JobDriver_Mount_letMountParticipate), nameof(JobDriver_Mount_letMountParticipate.letMountParticipate_Postfix)));
                }
                else if (name == "machine.rtr")
                {
                    if (Prefs.DevMode) Log.Message("[Windows] Raise the roof detected! Integrating...");
                    RaiseTheRoof = true;
                    CompareInfo ci = CultureInfo.CurrentCulture.CompareInfo;
                    for (int k = DefDatabase<RoofDef>.defsList.Count; k-- > 0;)
                    {
                        var def = DefDatabase<RoofDef>.defsList[k];
                        if (ci.IsPrefix(def.defName, "RTR_RoofTransparent")) TransparentRoofsList.Add(def);
                    }
                    if (Prefs.DevMode && !TransparentRoofs) Log.Error("[Windows] ...but no roofs detected! Integration failed.");
                }
            }
        }

        public static Harmony Instance
        {
            get
            {
                if (_instance == null) _instance = new Harmony("JPT.OpenTheWindows");
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
using HarmonyLib;
using System;
using System.Linq;
using Verse;

namespace OpenTheWindows
{
    [StaticConstructorOnStartup]
    public static class HarmonyPatches
    {
        public static readonly Type patchType = typeof(HarmonyPatches);
        public static bool ExpandedRoofing = false;

        public static bool DubsSkylights = false;

        public static Harmony instance = null;

        public static Harmony Instance
        {
            get
            {
                if (instance == null)
                    instance = new Harmony("jptrrs.openthewindows");
                return instance;
            }
        }

        static HarmonyPatches()
        {
            Instance.PatchAll();

            if (LoadedModManager.RunningModsListForReading.Any(x => x.Name == "Dubs Skylights"))
            {
                Log.Message("[OpenTheWindows] Dubs Skylights detected! Integrating...");
                Destroy_DubsFunctions.DemolishFunctions(); // also patches in RegenGrid, as well as culling functionality
                DubsSkylights = true;
            }

            if (AccessTools.TypeByName("ExpandedRoofing.HarmonyPatches") is Type expandedRoofingType)
            {
                Log.Message("[OpenTheWindows] Expanded Roofing detected! Integrating...");
                ExpandedRoofing = true;

                // i'm too lazy to reimplement - you should however.
                Instance.Patch(AccessTools.Method("ExpandedRoofing.CompCustomRoof:PostSpawnSetup"),
                    null, new HarmonyMethod(typeof(Destroy_DubsFunctions), nameof(Destroy_DubsFunctions.RegenGrid)), null);
            }

            if (LoadedModManager.RunningModsListForReading.Any(x => x.Name.Contains("Nature is Beautiful") || x.Name.Contains("Beautiful Outdoors") || x.Name.Contains("Custom Natural Beauty")))
            {
                Log.Message("[OpenTheWindows] Landscape beautification mod detected! Integrating...");
                OpenTheWindowsSettings.IsBeautyOn = true;
            }
        }
    }
}
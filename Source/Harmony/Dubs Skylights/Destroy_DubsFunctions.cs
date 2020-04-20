using HarmonyLib;
using Verse;

namespace OpenTheWindows
{
    public static class Destroy_DubsFunctions
    {
        public static void DemolishFunctions()
        {
            HarmonyPatches.Instance.Patch(AccessTools.Method("Dubs_Skylight.Patch_GameGlowAt:Postfix"), new HarmonyMethod(typeof(Destroy_DubsFunctions), nameof(KullFunction)), null, null);
            HarmonyPatches.Instance.Patch(AccessTools.Method("Dubs_Skylight.Patch_SectionLayer_LightingOverlay_Regenerate:Prefix"), new HarmonyMethod(typeof(Destroy_DubsFunctions), nameof(KullFunction)), null, null);
            HarmonyPatches.Instance.Patch(AccessTools.Method("Dubs_Skylight.Patch_SectionLayer_LightingOverlay_Regenerate:Postfix"), new HarmonyMethod(typeof(Destroy_DubsFunctions), nameof(KullFunction)), null, null);
            HarmonyPatches.Instance.Patch(AccessTools.Method("Dubs_Skylight.MapComp_Skylights:RegenGrid"), null, new HarmonyMethod(typeof(Destroy_DubsFunctions), nameof(RegenGrid)), null);
        }

        public static bool KullFunction()
        {
            return false;
        }

        public static void RegenGrid()
        {
            Find.CurrentMap.GetComponent<MapComp_Windows>().RegenGrid();
        }
    }
}
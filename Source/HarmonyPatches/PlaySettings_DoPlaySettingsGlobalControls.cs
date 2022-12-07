using HarmonyLib;
using RimWorld;
using Verse;

namespace OpenTheWindows
{
    //Inserts the natural light overlay icon.
    [HarmonyPatch(typeof(PlaySettings), nameof(PlaySettings.DoPlaySettingsGlobalControls))]
    public static class PlaySettings_DoPlaySettingsGlobalControls
    {
        public static void Postfix(WidgetRow row, bool worldView)
        {
            if (!OpenTheWindowsSettings.ShowButton || worldView || row == null) return;

            row.ToggleableIcon(ref NaturalLightOverlay.toggleShow, ResourceBank.overlayIcon, NaturalLightOverlay.tooltip);
        }
    }
}
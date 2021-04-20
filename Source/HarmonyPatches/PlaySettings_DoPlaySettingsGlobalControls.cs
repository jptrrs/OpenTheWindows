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
    }
}
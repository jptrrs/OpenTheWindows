using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace OpenTheWindows
{
    //Inserts the natural light overlay icon.
    [HarmonyPatch(typeof(PlaySettings), nameof(PlaySettings.DoPlaySettingsGlobalControls))]
    public static class PlaySettings_DoPlaySettingsGlobalControls
    {
        static Texture2D Icon => ContentFinder<Texture2D>.Get("NaturalLightMap", true);

        public static void Postfix(WidgetRow row, bool worldView)
        {
            if (worldView || row == null || Icon == null) return;
            row.ToggleableIcon(ref NaturalLightOverlay.toggleShow, Icon, NaturalLightOverlay.tooltip, null, null);
        }
    }
}
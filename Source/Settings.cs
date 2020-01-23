using UnityEngine;
using Verse;

namespace OpenTheWindows
{
    public class OpenTheWindowsMod : Mod
    {
        private OpenTheWindowsSettings settings;

        public OpenTheWindowsMod(ModContentPack content) : base(content)
        {
            settings = GetSettings<OpenTheWindowsSettings>();
        }

        public override void DoSettingsWindowContents(Rect inRect)
        {
            OpenTheWindowsSettings.DoWindowContents(inRect);
        }

        public override string SettingsCategory()
        {
            return "OpenTheWindows".Translate();
        }

        public override void WriteSettings()
        {
            base.WriteSettings();
        }
    }

    public class OpenTheWindowsSettings : ModSettings
    {
        private const float indoorsNoNaturalLightPenaltyDefault = 3f; //indoors accelerated degradation when not under windows

        public static float IndoorsNoNaturalLightPenalty = indoorsNoNaturalLightPenaltyDefault;

        public const float beautySensitivityReductionDefault = 0.25f; // zero for vanilla

        public static float BeautySensitivityReduction = beautySensitivityReductionDefault;

        public static bool IsBeautyOn = false;

        public static void DoWindowContents(Rect inRect)
        {
            Listing_Standard listing = new Listing_Standard();
            listing.Begin(inRect);
            string label = "IndoorsNoNaturalLightPenalty".Translate() + ": " + IndoorsNoNaturalLightPenalty.ToStringDecimalIfSmall() + "x";
            string desc = ("IndoorsNoNaturalLightPenaltyDesc").Translate();
            listing.Label(label, -1f, desc);
            IndoorsNoNaturalLightPenalty = listing.Slider(IndoorsNoNaturalLightPenalty, 1f, 10f);
            if (IsBeautyOn)
            {
                listing.Gap(12f);
                string label2 = "BeautySensitivityReduction".Translate() + ": " + BeautySensitivityReduction.ToStringPercent();
                string desc2 = ("BeautySensitivityReductionDesc").Translate();
                listing.Label(label2, -1f, desc2);
                BeautySensitivityReduction = listing.Slider(BeautySensitivityReduction, 0f, 1f);
            }
            listing.Gap(12f);
            if (listing.ButtonText("Reset", null))
            {
                BeautySensitivityReduction = 0.25f;
                IndoorsNoNaturalLightPenalty = 3f;
            }
            listing.End();
        }

        public override void ExposeData()
        {
            Scribe_Values.Look(ref IndoorsNoNaturalLightPenalty, "IndoorsNoNaturalLightPenalty", indoorsNoNaturalLightPenaltyDefault);
            Scribe_Values.Look(ref BeautySensitivityReduction, "ModifiedBeautyImpactFactor", beautySensitivityReductionDefault);
            base.ExposeData();
        }
    }
}
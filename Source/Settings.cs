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

        public const float LightTransmissionDefault = 0.9f; // light actually transmitted through windows       
        
        public static float LightTransmission = LightTransmissionDefault;

        public static bool IsBeautyOn = false;

        public static bool LinkWindows = true;

        public static bool LinkVents = true;

        public static void DoWindowContents(Rect inRect)
        {
            Listing_Standard listing = new Listing_Standard();
            listing.Begin(inRect);
            string label = "IndoorsNoNaturalLightPenalty".Translate() + ": " + IndoorsNoNaturalLightPenalty.ToStringDecimalIfSmall() + "x";
            string desc = ("IndoorsNoNaturalLightPenaltyDesc").Translate();
            listing.Label(label, -1f, desc);
            IndoorsNoNaturalLightPenalty = listing.Slider(IndoorsNoNaturalLightPenalty, 1f, 10f);
            listing.Gap(12f);
            string labelNoteOnSkylights = (HarmonyPatches.DubsSkylights || HarmonyPatches.ExpandedRoofing)? " ("+"LightTransmissionIncludesRoofs".Translate()+")" : null;
            string label2 = "LightTransmission".Translate() + labelNoteOnSkylights + ": " + LightTransmission.ToStringPercent();
            string desc2 = ("LightTransmissionDesc").Translate() ;
            listing.Label(label2, -1f, desc2);
            LightTransmission = listing.Slider(LightTransmission, 0f, 1f);
            if (IsBeautyOn)
            {
                listing.Gap(12f);
                string label3 = "BeautySensitivityReduction".Translate() + ": " + BeautySensitivityReduction.ToStringPercent();
                string desc3 = ("BeautySensitivityReductionDesc").Translate();
                listing.Label(label3, -1f, desc3);
                BeautySensitivityReduction = listing.Slider(BeautySensitivityReduction, 0f, 1f);
            }
            listing.Gap(12f);
            listing.Label(("LinkOptionsLabel").Translate()+" (" + ("RequiresRestart").Translate() + "):");
            listing.GapLine();
            listing.CheckboxLabeled(("LinkWindowsAndWalls").Translate(), ref LinkWindows);
            if (LoadedModManager.RunningModsListForReading.Any(x => x.Name.Contains("RimFridge")))
            {
                    listing.CheckboxLabeled(("LinkFridgesAndWalls").Translate(), ref LinkVents);
            }
            else
            {
                listing.CheckboxLabeled(("LinkVentsAndWalls").Translate(), ref LinkVents);
            }
            listing.Gap(24f);
            if (listing.ButtonText("Reset", null))
            {
                IndoorsNoNaturalLightPenalty = indoorsNoNaturalLightPenaltyDefault;
                BeautySensitivityReduction = beautySensitivityReductionDefault;
                LightTransmission = LightTransmissionDefault;
                LinkWindows = true;
                LinkVents = true;
            }
            listing.End();
        }

        public override void ExposeData()
        {
            Scribe_Values.Look(ref IndoorsNoNaturalLightPenalty, "IndoorsNoNaturalLightPenalty", indoorsNoNaturalLightPenaltyDefault);
            Scribe_Values.Look(ref BeautySensitivityReduction, "ModifiedBeautyImpactFactor", beautySensitivityReductionDefault);
            Scribe_Values.Look(ref LinkWindows, "LinkWindows", true);
            Scribe_Values.Look(ref LinkVents, "LinkVents", true);
            base.ExposeData();
        }
    }
}
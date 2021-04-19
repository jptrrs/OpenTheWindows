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
            //Rect compact = new Rect(inRect.position, inRect.size / 2);
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

        //public const float UpdateIntervalDefault = 1.0f; // affects tick functions on overlays.

        //public static float UpdateInterval = UpdateIntervalDefault;

        public static bool IsBeautyOn = false;

        public static bool BeautyFromBuildings = false;

        public static bool LinkWindows = true;

        public static bool LinkVents = true;

        public static void DoWindowContents(Rect inRect)
        {
            float padding = 15f;
            //float vertLimit = 0.75f;
            float footer = 60f;
            Rect leftSide = new Rect(inRect.x, inRect.y, (inRect.width - padding) / 2, inRect.height - footer);
            Vector2 rightStart = new Vector2(leftSide.max.x + padding, inRect.y);
            Rect rigthSide = new Rect(rightStart, leftSide.size);

            string resetBtnText = "Reset";
            float resetBtnWidth = resetBtnText.GetWidthCached() * 2;
            Vector2 resetBtnPos = new Vector2((inRect.max.x - resetBtnWidth) / 2, inRect.max.y - footer);
            Vector2 resetBtnSize = new Vector2(resetBtnWidth, footer);
            Rect resetButtonRect = new Rect(resetBtnPos,resetBtnSize);

            Listing_Standard leftColumn = new Listing_Standard();
            leftColumn.Begin(leftSide);

            //Outdoors need acceleration
            string label = "IndoorsNoNaturalLightPenalty".Translate() + ": " + IndoorsNoNaturalLightPenalty.ToStringDecimalIfSmall() + "x";
            string desc = "IndoorsNoNaturalLightPenaltyDesc".Translate();
            leftColumn.Label(label, -1f, desc);
            IndoorsNoNaturalLightPenalty = leftColumn.Slider(IndoorsNoNaturalLightPenalty, 1f, 10f);
            leftColumn.Gap(12f);

            //Light transmission through windows
            string labelNoteOnSkylights = (HarmonyPatcher.DubsSkylights || HarmonyPatcher.ExpandedRoofing) ? " (" + "LightTransmissionIncludesRoofs".Translate() + ")" : null;
            string label2 = "LightTransmission".Translate() + labelNoteOnSkylights + ": " + LightTransmission.ToStringPercent();
            string desc2 = "LightTransmissionDesc".Translate();
            leftColumn.Label(label2, -1f, desc2);
            LightTransmission = leftColumn.Slider(LightTransmission, 0f, 1f);

            //Beauty sensitivity reduction
            if (IsBeautyOn)
            {
                leftColumn.Gap(12f);
                string label3 = "BeautySensitivityReduction".Translate() + ": " + BeautySensitivityReduction.ToStringPercent();
                string desc3 = "BeautySensitivityReductionDesc".Translate();
                leftColumn.Label(label3, -1f, desc3);
                BeautySensitivityReduction = leftColumn.Slider(BeautySensitivityReduction, 0f, 1f);
                leftColumn.CheckboxLabeled("BeautyFromBuildings".Translate(), ref BeautyFromBuildings, "BeautyFromBuildingsDesc".Translate());
            }

            ////Performance adjust
            //listing.Gap(12f);
            //string label4 = " ".Translate() + ": " + UpdateInterval.ToStringDecimalIfSmall() + "s";
            //string desc4 = ("UpdateIntervalDesc").Translate();
            //listing.Label(label4, -1f, desc4);
            //UpdateInterval = listing.Slider(UpdateInterval, 1f, 10f);
            //leftColumn.Gap(12f);
            leftColumn.End();

            Listing_Standard rightColumn = new Listing_Standard();
            rightColumn.Begin(rigthSide);
            rightColumn.Label(("LinkOptionsLabel").Translate() + " (" + ("RequiresRestart").Translate() + "):");
            rightColumn.GapLine();
            rightColumn.CheckboxLabeled(("LinkWindowsAndWalls").Translate(), ref LinkWindows);
            if (LoadedModManager.RunningModsListForReading.Any(x => x.Name.Contains("RimFridge")))
            {
                rightColumn.CheckboxLabeled(("LinkFridgesAndWalls").Translate(), ref LinkVents);
            }
            else
            {
                rightColumn.CheckboxLabeled(("LinkVentsAndWalls").Translate(), ref LinkVents);
            }
            rightColumn.End();

            Listing_Standard bottomBar = new Listing_Standard();
            bottomBar.Begin(resetButtonRect);
            bottomBar.Gap(24f);
            if (bottomBar.ButtonText(resetBtnText, null))
            {
                IndoorsNoNaturalLightPenalty = indoorsNoNaturalLightPenaltyDefault;
                BeautySensitivityReduction = beautySensitivityReductionDefault;
                LightTransmission = LightTransmissionDefault;
                LinkWindows = true;
                LinkVents = true;
                //UpdateInterval = UpdateIntervalDefault;
                BeautyFromBuildings = false;
            }
            bottomBar.End();
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
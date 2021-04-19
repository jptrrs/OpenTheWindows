using RimWorld;
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
        private const float 
            IndoorsNoNaturalLightPenaltyDefault = 3f, //indoors accelerated degradation when not under windows
            BeautySensitivityReductionDefault = 0.25f, // zero for vanilla
            LightTransmissionDefault = 0.9f; // light actually transmitted through windows
            //UpdateIntervalDefault = 1.0f; // affects tick functions on overlays.

        public static float 
            IndoorsNoNaturalLightPenalty = IndoorsNoNaturalLightPenaltyDefault,
            BeautySensitivityReduction = BeautySensitivityReductionDefault,
            LightTransmission = LightTransmissionDefault;
            //UpdateInterval = UpdateIntervalDefault;

        public static bool 
            IsBeautyOn = false,
            BeautyFromBuildings = false,
            LinkWindows = true,
            LinkVents = true;

        //private static IntRange ComfortTempDefault;

        private static IntRange _comfortTemp;

        public static IntRange ComfortTempDefault
        {
            get
            {
                if (PlayDataLoader.Loaded)
                {
                    return new IntRange((int)ThingDefOf.Human.GetStatValueAbstract(StatDefOf.ComfyTemperatureMin), (int)ThingDefOf.Human.GetStatValueAbstract(StatDefOf.ComfyTemperatureMax));
                }
                return new IntRange();
            }
        }

        public static IntRange ComfortTemp
        {
            get
            {
                if (_comfortTemp == null)
                {
                    return ValidateAndSetupComfortTemp();
                }
                return _comfortTemp;
            }
            set
            {
                _comfortTemp = value;
            }
        }

        private static IntRange ValidateAndSetupComfortTemp()
        {
            if (_comfortTemp == null || (!Input.GetMouseButton(0) && _comfortTemp.max == 0 && _comfortTemp.min == 0))
            {
                var defaultValue = ComfortTempDefault;
                if (defaultValue.max == 0 && defaultValue.min == 0)
                {
                    Log.Error("[OpenTheWindows] Can't determine the default comfortable temperature range before the defs are loaded!");
                }
                else
                {
                    _comfortTemp = defaultValue;
                }
                return defaultValue;
            }
            return _comfortTemp;
        }

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
            string label = $"{"IndoorsNoNaturalLightPenalty".Translate()}: {IndoorsNoNaturalLightPenalty.ToStringDecimalIfSmall()}x";
            string desc = "IndoorsNoNaturalLightPenaltyDesc".Translate();
            leftColumn.Label(label, -1f, desc);
            IndoorsNoNaturalLightPenalty = leftColumn.Slider(IndoorsNoNaturalLightPenalty, 1f, 10f);
            leftColumn.Gap(12f);

            //Light transmission through windows
            string labelNoteOnSkylights = (HarmonyPatcher.DubsSkylights || HarmonyPatcher.ExpandedRoofing) ? $" ({"LightTransmissionIncludesRoofs".Translate()})" : null;
            string label2 = $"{"LightTransmission".Translate()}{labelNoteOnSkylights}: {LightTransmission.ToStringPercent()}";
            string desc2 = "LightTransmissionDesc".Translate();
            leftColumn.Label(label2, -1f, desc2);
            LightTransmission = leftColumn.Slider(LightTransmission, 0f, 1f);
            leftColumn.Gap(12f);

            //Temperature comfort range for auto-ventilation
            string label3 = "ComfortableTemperature".Translate();
            string desc3 = "ComfortableTemperatureDesc".Translate();
            leftColumn.Label(label3, -1f, desc3);
            ValidateAndSetupComfortTemp();
            leftColumn.IntRange(ref _comfortTemp, -40, 100);

            //Beauty sensitivity reduction
            if (IsBeautyOn)
            {
                leftColumn.Gap(12f);
                string label4 = $"{"BeautySensitivityReduction".Translate()}: {BeautySensitivityReduction.ToStringPercent()}";
                string desc4 = "BeautySensitivityReductionDesc".Translate();
                leftColumn.Label(label4, -1f, desc4);
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
            rightColumn.Label($"{"LinkOptionsLabel".Translate()} ({"RequiresRestart".Translate()}):");
            rightColumn.GapLine();
            rightColumn.CheckboxLabeled("LinkWindowsAndWalls".Translate(), ref LinkWindows);
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
                IndoorsNoNaturalLightPenalty = IndoorsNoNaturalLightPenaltyDefault;
                BeautySensitivityReduction = BeautySensitivityReductionDefault;
                LightTransmission = LightTransmissionDefault;
                LinkWindows = true;
                LinkVents = true;
                //UpdateInterval = UpdateIntervalDefault;
                BeautyFromBuildings = false;
                ComfortTemp = ComfortTempDefault;
            }
            bottomBar.End();
        }

        public override void ExposeData()
        {
            Scribe_Values.Look(ref IndoorsNoNaturalLightPenalty, "IndoorsNoNaturalLightPenalty", IndoorsNoNaturalLightPenaltyDefault);
            Scribe_Values.Look(ref BeautySensitivityReduction, "ModifiedBeautyImpactFactor", BeautySensitivityReductionDefault);
            Scribe_Values.Look(ref LinkWindows, "LinkWindows", true);
            Scribe_Values.Look(ref LinkVents, "LinkVents", true);
            Scribe_Values.Look(ref _comfortTemp, "ComfortTemp", ComfortTempDefault);
            base.ExposeData();
        }
    }
}
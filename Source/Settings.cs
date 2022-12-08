using RimWorld;
using UnityEngine;
using Verse;
using static OpenTheWindows.OpenTheWindowsSettings;

namespace OpenTheWindows
{
    public class OpenTheWindowsMod : Mod
    {

        public OpenTheWindowsMod(ModContentPack content) : base(content)
        {
            base.GetSettings<OpenTheWindowsSettings>();
        }

        public override void DoSettingsWindowContents(Rect inRect)
        {
            dialogOpen = true;
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
            leftColumn.Gap();

            //Light transmission through windows
            string labelNoteOnSkylights = (HarmonyPatcher.DubsSkylights || HarmonyPatcher.TransparentRoofs) ? $"\n({"LightTransmissionIncludesRoofs".Translate()})" : null;
            string label2 = $"{"LightTransmission".Translate()}: {LightTransmission.ToStringPercent()}{labelNoteOnSkylights}";
            string desc2 = "LightTransmissionDesc".Translate();
            leftColumn.Label(label2, -1f, desc2);
            LightTransmission = leftColumn.Slider(LightTransmission, 0f, 1f);
            leftColumn.Gap();

            //Temperature comfort range for auto-ventilation
            string label3 = "ComfortableTemperature".Translate();
            string desc3 = "ComfortableTemperatureDesc".Translate();
            leftColumn.Label(label3, -1f, desc3);
            ValidateAndSetupComfortTemp();
            leftColumn.IntRange(ref _comfortTemp, -40, 100);


            leftColumn.CheckboxLabeled("ShowButton".Translate(), ref ShowButton);

            leftColumn.End();
            Listing_Standard rightColumn = new Listing_Standard();

            //======RIGHT COLUMN======
            rightColumn.Begin(rigthSide);
            //Better Pawn Control Alarm options
            if (HarmonyPatcher.BetterPawnControl)
            {
                rightColumn.Gap();
                rightColumn.Label("AlarmOptionsLabel".Translate());
                rightColumn.GapLine();
                rightColumn.CheckboxLabeled("AlarmReactDefault".Translate(), ref AlarmReactDefault);
            }

            rightColumn.End();

            Listing_Standard bottomBar = new Listing_Standard();
            bottomBar.Begin(resetButtonRect);
            bottomBar.Gap(24f);
            if (bottomBar.ButtonText(resetBtnText, null))
            {
                Reset();
            }
            bottomBar.End();
            dialogOpen = false;
        }

        public static void Reset()
        {
            IndoorsNoNaturalLightPenalty = IndoorsNoNaturalLightPenaltyDefault;
            BeautySensitivityReduction = BeautySensitivityReductionDefault;
            LightTransmission = LightTransmissionDefault;
            AlarmReactDefault = false;
            ComfortTemp = ComfortTempDefault();
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
        public static IntRange ComfortTempDefault()
        {
            return PlayDataLoader.Loaded ? 
            new IntRange((int)ThingDefOf.Human.GetStatValueAbstract(StatDefOf.ComfyTemperatureMin), (int)ThingDefOf.Human.GetStatValueAbstract(StatDefOf.ComfyTemperatureMax)) : IntRange.zero;
        }

        public static IntRange ComfortTemp
        {
            get
            {
                if (_comfortTemp == IntRange.zero) return ValidateAndSetupComfortTemp();
                return _comfortTemp;
            }
            set { _comfortTemp = value; }
        }

        public static IntRange ValidateAndSetupComfortTemp()
        {
            if (_comfortTemp == IntRange.zero && (!dialogOpen || (dialogOpen && !Input.GetMouseButton(0))))
            {
                _comfortTemp = ComfortTempDefault();
            }
            return _comfortTemp;
        }

        public override void ExposeData()
        {
            Scribe_Values.Look(ref IndoorsNoNaturalLightPenalty, "IndoorsNoNaturalLightPenalty", IndoorsNoNaturalLightPenaltyDefault);
            Scribe_Values.Look(ref LightTransmission, "LightTransmission", LightTransmissionDefault);
            Scribe_Values.Look(ref ShowButton, "ShowButton");
            Scribe_Values.Look(ref AlarmReactDefault, "AlarmReactDefault");
            Scribe_Values.Look(ref _comfortTemp, "ComfortTemp", ComfortTempDefault());
            base.ExposeData();
        }

        public const float 
            IndoorsNoNaturalLightPenaltyDefault = 3f, //indoors accelerated degradation when not under windows
            BeautySensitivityReductionDefault = 0f, // zero for vanilla
            LightTransmissionDefault = 0.5f; // light actually transmitted through windows

        public static float 
            IndoorsNoNaturalLightPenalty = IndoorsNoNaturalLightPenaltyDefault,
            BeautySensitivityReduction = BeautySensitivityReductionDefault,
            LightTransmission = LightTransmissionDefault;

        public static bool AlarmReactDefault, dialogOpen, ShowButton;
        public static IntRange _comfortTemp;
    }
}
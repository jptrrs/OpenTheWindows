using System.Reflection;
using HarmonyLib;
using RimWorld;
using Verse;

namespace OpenTheWindows
{
    public class CompWindow : CompFlickable
    {
        FieldInfo baseWantSwitchInfo = AccessTools.Field(typeof(CompFlickable), "wantSwitchOn");
        FieldInfo baseSwitchOnIntInfo = AccessTools.Field(typeof(CompFlickable), "switchOnInt");

        public CompProperties_Window Props
        {
            get
            {
                return (CompProperties_Window)props;
            }
        }

        public bool switchOnInt
        {
            get
            {
                return (bool)baseSwitchOnIntInfo.GetValue(this);
            }
            set
            {
                baseSwitchOnIntInfo.SetValue(this, value);
            }
        }

        public new bool SwitchIsOn
        {
            get
            {
                return switchOnInt;
            }
            set
            {
                if (switchOnInt == value)
                {
                    return;
                }
                if (switchOnInt)
                {
                    parent.BroadcastCompSignal(FlickedOnSignal());
                }
                else
                {
                    parent.BroadcastCompSignal(FlickedOffSignal());
                }
                if (parent.Spawned)
                {
                    parent.Map.mapDrawer.MapMeshDirty(parent.Position, MapMeshFlag.Things | MapMeshFlag.Buildings);
                }
            }
        }

        public new string FlickedOffSignal() => Props.signal + "Off";

        public new string FlickedOnSignal() => Props.signal + "On";

        public override void Initialize(CompProperties props)
        {
            base.Initialize(props);
            FlickedOnSignal();
            FlickedOffSignal();
            SetupState();
        }

        public void SetupState()
        {
            Building_Window window = parent as Building_Window;
            bool state = false;
            if (Props.signal == "light" || Props.signal == "both") state = window.open;
            else if (Props.signal == "air") state = window.venting;
            baseWantSwitchInfo.SetValue(this, state);
            baseSwitchOnIntInfo.SetValue(this, state);
            SwitchIsOn = state;
        }
    }
}
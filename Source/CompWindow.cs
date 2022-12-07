using HarmonyLib;
using RimWorld;
using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace OpenTheWindows
{
    public class CompWindow : CompFlickable
    {

        public new CompProperties_Window Props;

        public bool WantSwitchOn
        {
            get { return wantSwitchOn; }
            set { wantSwitchOn = value; }
        }

        public bool SwitchOnInt
        {
            get { return switchOnInt; }
            set { switchOnInt = value; }
        }

        public new string FlickedOffSignal => Props.signal.ToString() + "Off";
        public new string FlickedOnSignal => Props.signal.ToString() + "On";

        public override void Initialize(CompProperties props)
        {
            base.Initialize(props);
            SetupState();
        }

        public void SetupState()
        {
            Props = (CompProperties_Window)props;
            Building_Window window = parent as Building_Window;
            bool state = false;
            if (Props.signal == CompProperties_Window.Signal.light || Props.signal == CompProperties_Window.Signal.both) state = window.open;
            else if (Props.signal == CompProperties_Window.Signal.air) state = window.venting;
            WantSwitchOn = SwitchOnInt = SwitchIsOn = state;
        }

        private CompPowerTrader PowerComp
        {
            get
            {
                Building_Window parentWindow = parent as Building_Window;
                return parentWindow.powerComp;
            }
        }

        private bool Powered
        {
            get
            {
                return Props.automated && PowerComp != null && PowerComp.PowerOn;
            }
        }

        public void FlickFor(bool request)
        {
            if (SwitchIsOn != request || WantsFlick())
            {
                WantSwitchOn = request;
                if (Powered)
                {
                    DoFlick();
                    return;
                }
                FlickUtility.UpdateFlickDesignation(parent);
            }
        }

        public string ManualNote => Props.automated ? "" : $" {"ManualCommandNote".Translate()}";

        public override IEnumerable<Gizmo> CompGetGizmosExtra()
        {
            if (parent.Faction == Faction.OfPlayer)
            {
                Building_Window window = parent as Building_Window;
                yield return new Command_Toggle()
                {
                    hotKey = KeyBindingDefOf.Command_TogglePower,
                    icon = (Texture2D)AccessTools.Property(typeof(CompFlickable), "CommandTex").GetValue(this),
                    defaultLabel = Props.commandLabelKey.Translate(),
                    defaultDesc = Props.commandDescKey.Translate() + ManualNote,
                    isActive = () => WantSwitchOn,
                    disabled = GizmoDisable,
                    disabledReason = window.alarmReact ? "DisabledByEmergency".Translate() : "DisabledForAutoVentilation".Translate(),
                    toggleAction = delegate ()
                    {
                        FlickFor(!WantSwitchOn);
                    }
                };
            }
            yield break;
        }

        private bool GizmoDisable
        {
            get
            {
                Building_Window window = parent as Building_Window;
                bool ifAutovent = (Props.signal == CompProperties_Window.Signal.air || Props.signal == CompProperties_Window.Signal.both) && window.autoVent;
                bool ifAlarm = window.alarmReact && AlertManagerProxy.onAlert;
                return ifAutovent || ifAlarm;
            }
        }

    }
}
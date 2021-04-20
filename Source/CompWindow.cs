using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using Verse;

namespace OpenTheWindows
{
    public class CompWindow : CompFlickable
    {
        private FieldInfo baseWantSwitchInfo = AccessTools.Field(typeof(CompFlickable), "wantSwitchOn");
        private FieldInfo baseSwitchOnIntInfo = AccessTools.Field(typeof(CompFlickable), "switchOnInt");

        public new CompProperties_Window Props
        {
            get
            {
                return (CompProperties_Window)props;
            }
        }

        public new bool wantSwitchOn
        {
            get
            {
                return (bool)baseWantSwitchInfo.GetValue(this);
            }
            set
            {
                baseWantSwitchInfo.SetValue(this, value);
            }
        }

        public new bool switchOnInt
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

        public new string FlickedOffSignal => Props.signal + "Off";

        public new string FlickedOnSignal => Props.signal + "On";

        public override void Initialize(CompProperties props)
        {
            base.Initialize(props);
            SetupState();
        }

        public void SetupState()
        {
            Building_Window window = parent as Building_Window;
            bool state = false;
            if (Props.signal == "light" || Props.signal == "both") state = window.open;
            else if (Props.signal == "air") state = window.venting;
            wantSwitchOn = state;
            switchOnInt = state;
            SwitchIsOn = state;
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

        public void AutoFlickRequest()
        {
            if (Powered)
            {
                DoFlick();
                return;
            }
            if (!WantsFlick())
            {
                wantSwitchOn = !wantSwitchOn;
                FlickUtility.UpdateFlickDesignation(parent);
            }
        }

        public void FlickFor(bool state)
        {
            if (state != SwitchIsOn) AutoFlickRequest();
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
                    isActive = () => wantSwitchOn,
                    disabled = GizmoDisable,
                    disabledReason = window.alarmReact ? "DisabledByEmergency".Translate() : "DisabledForAutoVentilation".Translate(),
                    toggleAction = delegate ()
                    {
                        AutoFlickRequest();
                    }
                };

                //foreach (Gizmo gizmo in base.CompGetGizmosExtra())
                //{
                //    if (gizmo is Command_Toggle toggle)
                //    {
                //        toggle.isActive = () => wantSwitchOn;
                //        toggle.toggleAction = delegate ()
                //        {
                //            wantSwitchOn = !wantSwitchOn;
                //            FlickUtility.UpdateFlickDesignation(parent);
                //        };
                //    }
                //}

            }
            yield break;
        }

        private bool GizmoDisable
        {
            get
            {
                Building_Window window = parent as Building_Window;
                bool ifAutovent = (Props.signal == "air" || Props.signal == "both") && window.autoVent;
                bool ifAlarm = window.alarmReact && AlertManagerProxy.onAlert;
                return ifAutovent || ifAlarm;
            }
        }

    }
}
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
        public new CompProperties_Window Props;
        Building_Window window;

        public new string FlickedOffSignal => Props.signal + "Off";

        public new string FlickedOnSignal => Props.signal + "On";

        public override void Initialize(CompProperties props)
        {
            base.Initialize(props);
            SetupState();
        }

        //public void SetupState()
        //{
        //    Building_Window window = parent as Building_Window;
        //    bool state = false;
        //    if (Props.signal == "light" || Props.signal == "both") state = window.open;
        //    else if (Props.signal == "air") state = window.venting;
        //    WantSwitchOn = state;
        //    SwitchOnInt = state;
        //    SwitchIsOn = state;
        //}

        //by Owlchemist
        public void SetupState()
        {
            Props = (CompProperties_Window)props;
            window = parent as Building_Window;
            bool state = false;
            if (Props.signal == CompProperties_Window.Signal.light || Props.signal == CompProperties_Window.Signal.both) state = window.open;
            else if (Props.signal == CompProperties_Window.Signal.air) state = window.venting;
            wantSwitchOn = switchOnInt = SwitchIsOn = state;
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

        //public void FlickFor(bool request)
        //{
        //    if (SwitchIsOn != request || WantsFlick())
        //    {
        //        WantSwitchOn = request;
        //        if (Powered)
        //        {
        //            DoFlick();
        //            return;
        //        }
        //        FlickUtility.UpdateFlickDesignation(parent);
        //    }
        //}

        public void FlickFor(bool request)
        {
            if (switchOnInt != request || WantsFlick())
            {
                wantSwitchOn = request;
                if (Props.automated && (window.powerComp?.PowerOn ?? false))
                {
                    DoFlick();
                    return;
                }
                FlickUtility.UpdateFlickDesignation(parent);
            }
        }

        public string ManualNote => Props.automated ? "" : $" {"ManualCommandNote".Translate()}";

        //public override IEnumerable<Gizmo> CompGetGizmosExtra()
        //{
        //    if (parent.Faction == Faction.OfPlayer)
        //    {
        //        Building_Window window = parent as Building_Window;
        //        yield return new Command_Toggle()
        //        {
        //            hotKey = KeyBindingDefOf.Command_TogglePower,
        //            icon = (Texture2D)AccessTools.Property(typeof(CompFlickable), "CommandTex").GetValue(this),
        //            defaultLabel = Props.commandLabelKey.Translate(),
        //            defaultDesc = Props.commandDescKey.Translate() + ManualNote,
        //            isActive = () => WantSwitchOn,
        //            Disabled = GizmoDisable,
        //            disabledReason = window.alarmReact ? "DisabledByEmergency".Translate() : "DisabledForAutoVentilation".Translate(),
        //            toggleAction = delegate ()
        //            {
        //                FlickFor(!WantSwitchOn);
        //            }
        //        };
        //    }
        //    yield break;
        //}

        public override IEnumerable<Gizmo> CompGetGizmosExtra()
        {
            if (parent.factionInt.def.isPlayer)
            {
                yield return new Command_Toggle()
                {
                    hotKey = KeyBindingDefOf.Command_TogglePower,
                    icon = CommandTex,
                    defaultLabel = Props.commandLabelKey.Translate(),
                    defaultDesc = Props.automated ? Props.commandDescKey.Translate() + ManualNote : Props.commandDescKey.Translate(),
                    isActive = () => wantSwitchOn,
                    disabled = (window.autoVent && Props.signal != CompProperties_Window.Signal.light) || (window.alarmReact && AlertManagerProxy.onAlert),
                    disabledReason = window.alarmReact ? "DisabledByEmergency".Translate() : "DisabledForAutoVentilation".Translate(),
                    toggleAction = delegate ()
                    {
                        FlickFor(!wantSwitchOn);
                    }
                };
            }
        }

        //private bool GizmoDisable
        //{
        //    get
        //    {
        //        Building_Window window = parent as Building_Window;
        //        bool ifAutovent = (Props.signal == "air" || Props.signal == "both") && window.autoVent;
        //        bool ifAlarm = window.alarmReact && AlertManagerProxy.onAlert;
        //        return ifAutovent || ifAlarm;
        //    }
        //}
    }
}
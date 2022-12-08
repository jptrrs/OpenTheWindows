using RimWorld;
using System.Collections.Generic;
using Verse;

namespace OpenTheWindows
{
    public class CompWindow : CompFlickable
    {
        public new CompProperties_Window Props;
        Building_Window window;
        public override void Initialize(CompProperties props)
        {
            base.Initialize(props);
            SetupState();
        }
        public void SetupState()
        {
            Props = (CompProperties_Window)props;
            window = parent as Building_Window;
            bool state = false;
            if (Props.signal == CompProperties_Window.Signal.light || Props.signal == CompProperties_Window.Signal.both) state = window.open;
            else if (Props.signal == CompProperties_Window.Signal.air) state = window.venting;
            wantSwitchOn = switchOnInt = SwitchIsOn = state;
        }
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
        public override IEnumerable<Gizmo> CompGetGizmosExtra()
        {
            if (parent.factionInt.def.isPlayer)
            {
                yield return new Command_Toggle()
                {
                    hotKey = KeyBindingDefOf.Command_TogglePower,
                    icon = CommandTex,
                    defaultLabel = Props.commandLabelKey.Translate(),
                    defaultDesc = Props.automated ? Props.commandDescKey.Translate() + "ManualCommandNote".Translate() : Props.commandDescKey.Translate(),
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
    }
}
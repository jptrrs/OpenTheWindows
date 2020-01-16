using System.Reflection;
using Harmony;
using RimWorld;
using Verse;
using Verse.Sound;

namespace OpenTheWindows
{
    public class CompWindow : CompFlickable
    {
        public bool switchOnInt = true;

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
                switchOnInt = value;
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

        public CompProperties_Window Props
        {
            get
            {
                return (CompProperties_Window)props;
            }
        }

        public new void DoFlick()
        {
            SwitchIsOn = !SwitchIsOn;
            SoundDefOf.FlickSwitch.PlayOneShot(new TargetInfo(this.parent.Position, this.parent.Map, false));
        }

        public new string FlickedOffSignal() => Props.signal + "Off";

        public new string FlickedOnSignal() => Props.signal + "On";

        public override void Initialize(CompProperties props)
        {
            base.Initialize(props);
            FlickedOnSignal();
            FlickedOffSignal();
        }
        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look<bool>(ref switchOnInt, "switchOn", true, false);
        }
    }
}
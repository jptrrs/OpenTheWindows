using RimWorld;

namespace OpenTheWindows
{
    public class CompProperties_Window : CompProperties_Flickable
    {
        public CompProperties_Window()
        {
            compClass = typeof(CompWindow);
        }

        public Signal signal = Signal.light;
        public enum Signal { light, air, both };
        public bool automated = false;
    }
}
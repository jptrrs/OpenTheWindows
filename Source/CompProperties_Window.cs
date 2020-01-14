using RimWorld;

namespace OpenTheWindows
{
    public class CompProperties_Window : CompProperties_Flickable
    {
        public CompProperties_Window()
        {
            this.compClass = typeof(CompWindow);
        }

        public string signal = "";
        public bool startsOn = true;
    }
}
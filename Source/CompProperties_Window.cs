﻿using RimWorld;

namespace OpenTheWindows
{
    public class CompProperties_Window : CompProperties_Flickable
    {
        public CompProperties_Window()
        {
            compClass = typeof(CompWindow);
        }

        public string signal = "";
        public bool automated = false;
    }
}
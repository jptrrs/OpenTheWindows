using System;
using Verse;

namespace OpenTheWindows
{
    public static class MapUpdateWatcher
    {
        //public delegate void Notify();  // delegate: "template" for the handler to be defined on the subscriber class, replaced by .Net's EventHandler
        
        public static event EventHandler<IntVec3> MapUpdate; // event

        public static void OnMapUpdate(object sender, IntVec3 center) //if event is not null then call delegate
        {
            MapUpdate?.Invoke(sender, center);
        }

    }
}
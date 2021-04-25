using System;
using Verse;

namespace OpenTheWindows
{
    public static class MapUpdateWatcher
    {
        //public delegate void Notify();  // delegate: "template" for the handler to be defined on the subscriber class, replaced by .Net's EventHandler
        
        public static event EventHandler<MapUpdateInfo> MapUpdate; // event

        public static void OnMapUpdate(object sender, MapUpdateInfo info) //if event is not null then call delegate
        {
            MapUpdate?.Invoke(sender, info);
        }

        public class MapUpdateInfo : EventArgs
        {
            public IntVec3 center;
            public bool removing;
        }
    }
}
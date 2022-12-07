using System;
using Verse;

namespace OpenTheWindows
{
    public static class MapUpdateWatcher
    {   
        public static event EventHandler<MapUpdateInfo> MapUpdate; // event
        public static void OnMapUpdate(object sender, MapUpdateInfo info) //if event is not null then call delegate
        {
            MapUpdate?.Invoke(sender, info);
        }

        public class MapUpdateInfo : EventArgs
        {
            public IntVec3 center;
            public bool removed;
            public RoofDef roofDef;
            public Map map;
        }
    }
}
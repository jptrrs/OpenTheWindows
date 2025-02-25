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
            public IntVec3 origin;
            public bool removed;
            public RoofDef roofDef;
            public Map map;
            private int originIdx;
            public int Origin
            {
                get
                {
                    if (originIdx == 0)
                    {
                        originIdx = map.cellIndices.CellToIndex(origin);
                    }
                    return originIdx;
                }
                set
                {
                    originIdx = value;
                    if (map == null)
                    {
                        Log.Error($"[HumanResources] Error while trying to set the origin of a MapUpdate event: the map field must be set first");
                        return;
                    }
                    origin = map.cellIndices.IndexToCell(value);
                }
            }
        }
    }
}
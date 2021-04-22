using System;
using Verse;

namespace OpenTheWindows
{
    //Dodges the Linkflags check when drawing blueprints. Manually patched if Blueprints is present.
    public static class BuildableInfo_DrawGhost
    {
        public static void DrawGhost_Prefix(ThingDef ____thingDef, ref LinkFlags __state)
        {
            if (____thingDef.thingClass == typeof(Building_Window))
            {
                __state = ____thingDef.graphicData.linkFlags;
                ____thingDef.graphicData.linkFlags = LinkFlags.None;
            }
        }
        public static void DrawGhost_Postfix(ThingDef ____thingDef, LinkFlags __state)
        {
            if (__state != LinkFlags.None)
            {
                ____thingDef.graphicData.linkFlags = __state;
            }
        }
    }
}
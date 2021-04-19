using HarmonyLib;
using Verse;

namespace OpenTheWindows
{
    [HarmonyPatch(typeof(Building), nameof(Building.SpawnSetup))]
    public static class Building_SpawnSetup
    {
        public static void Postfix(Building __instance, Map map)
        {
            if (!OpenTheWindowsSettings.LinkVents) return; // no need to restart to change settings

            if (__instance.def.graphicData != null && __instance.def.graphicData.linkType == LinkDrawerType.None && __instance.def.graphicData.linkFlags == (LinkFlags.Rock | LinkFlags.Wall))
            {
                map.linkGrid.Notify_LinkerCreatedOrDestroyed(__instance);
                map.mapDrawer.MapMeshDirty(__instance.Position, MapMeshFlag.Things, true, false);
            }
        }
    }

    [HarmonyPatch(typeof(Building), nameof(Building.SpawnSetup))]
    public static class Building_Despawn
    {
        public static void Postix(Building __instance)
        {
            if (!OpenTheWindowsSettings.LinkVents) return; // no need to restart to change settings

            if (__instance.def.graphicData != null && __instance.def.graphicData.linkType == LinkDrawerType.None && __instance.def.graphicData.linkFlags == (LinkFlags.Rock | LinkFlags.Wall))
            {
                Map map = __instance.Map;
                map.thingGrid.Deregister(__instance, false);
                map.linkGrid.Notify_LinkerCreatedOrDestroyed(__instance);
                map.mapDrawer.MapMeshDirty(__instance.Position, MapMeshFlag.Things, true, false);
            }
        }
    }
}
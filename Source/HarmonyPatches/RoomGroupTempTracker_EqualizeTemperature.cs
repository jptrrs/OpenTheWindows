using HarmonyLib;
using System.Reflection;
using Verse;

namespace OpenTheWindows
{
    //Cleans up after invalid rooms left by despawned windows.
    [HarmonyPatch(typeof(RoomTempTracker), nameof(RoomTempTracker.EqualizeTemperature))]
    [HarmonyPriority(Priority.First)]
    public static class RoomGroupTempTracker_EqualizeTemperature
    {
        public static bool Prefix(RoomTempTracker __instance)
        {
            var length = __instance.room.districts.Count;
            for (int i = 0; i < length; i++)
            {
                var district = __instance.room.districts[i];
                if (district.IsDoorway && district.CellCount == 0)
                {
                    Map map = district.Map;
                    foreach (var region in __instance.room.Regions)
                    {
                        foreach (var link in region.links)
                        {
                            link.Deregister(region);
                        }
                    }
                    map.regionGrid.allRooms.Remove(__instance.room);
                    map.regionAndRoomUpdater.TryRebuildDirtyRegionsAndRooms();
                    return false;
                }
            }
            return true;
        }
    }
}
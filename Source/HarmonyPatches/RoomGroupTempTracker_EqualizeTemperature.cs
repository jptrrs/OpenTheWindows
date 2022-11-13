using HarmonyLib;
using System.Reflection;
using Verse;

namespace OpenTheWindows
{
    //Cleans up after invalid rooms left by despawned windows.
    [HarmonyPatch(typeof(RoomTempTracker), nameof(RoomTempTracker.EqualizeTemperature))]
    public static class RoomGroupTempTracker_EqualizeTemperature
    {
        private static FieldInfo roomsInfo = AccessTools.Field(typeof(Room), "rooms");
        public static bool Prefix(RoomTempTracker __instance, Room ___room)
        {
            var orphan = ___room.Districts.FirstOrFallback(x => x.IsDoorway == true && x.Cells.EnumerableNullOrEmpty());
            if (orphan != null)
            {
                Map map = orphan.Map;
                foreach (var region in ___room.Regions)
                {
                    foreach (var link in region.links)
                    {
                        link.Deregister(region);
                    }
                }
                map.regionGrid.allRooms.Remove(___room);
                map.regionAndRoomUpdater.TryRebuildDirtyRegionsAndRooms();
                return false;
            }
            return true;
        }
    }
}
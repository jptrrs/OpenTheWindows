using HarmonyLib;
using System.Collections.Generic;
using System.Reflection;
using Verse;

namespace OpenTheWindows
{
    //Cleans up after invalid rooms left by despawned windows.
    [HarmonyPatch(typeof(RoomGroupTempTracker), nameof(RoomGroupTempTracker.EqualizeTemperature))]
    public static class RoomGroupTempTracker_EqualizeTemperature
    {
        private static FieldInfo roomsInfo = AccessTools.Field(typeof(RoomGroup), "rooms");
        public static bool Prefix(RoomGroupTempTracker __instance, RoomGroup ___roomGroup)
        {
            List<Room> rooms = (List<Room>)roomsInfo.GetValue(___roomGroup);
            var orphan = rooms.FirstOrFallback(x => x.RegionType == RegionType.Portal && x.Cells.EnumerableNullOrEmpty());
            if (orphan != null)
            {
                Map map = orphan.Map;
                rooms.Clear();
                roomsInfo.SetValue(___roomGroup, rooms);
                foreach (Region region in ___roomGroup.Regions)
                {
                    map.regionDirtyer.SetRegionDirty(region);
                }
                map.regionGrid.allRooms.Remove(orphan);
                map.regionAndRoomUpdater.TryRebuildDirtyRegionsAndRooms();
                return false;
            }
            return true;
        }
    }
}
using HarmonyLib;
using Verse;

namespace OpenTheWindows
{
    //[HarmonyPatch(typeof(RegionTypeUtility), nameof(RegionTypeUtility.GetExpectedRegionType))]
    public static class RegionTypeUtility_GetExpectedRegionType
    {
        public static void Postfix(IntVec3 c, Map map, ref RegionType __result)
        {
            Log.Message("DEBUG region type changed at " + c);
            if (Current.ProgramState == ProgramState.Playing && __result == RegionType.None)
            {
                if ((c.GetEdifice(map) as Building_Window) != null)
                {
                    __result = RegionType.Portal;
                    Log.Message("DEBUG region type changed at " + c);
                }
            }
        }
    }
}
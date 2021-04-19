using HarmonyLib;
using Verse;

namespace OpenTheWindows
{
    //[HarmonyPatch(typeof(Region), nameof(Region.Allows))]
    public static class Region_Allows
    {
        public static void Postfix(TraverseParms tp, int ___id, ref bool __result)
        {
            if (!__result && tp.canBash)
            {
                __result = true;
            }
        }
    }
}
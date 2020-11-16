using HarmonyLib;
using System;
using UnityEngine;
using Verse;

namespace OpenTheWindows
{
    //[HarmonyPatch(typeof(Region), nameof(Region.Allows))]
    public static class Region_Allows
    {
        public static void Postfix(TraverseParms tp, int ___id, ref bool __result)
        {
            //Log.Message("DEBUG postfixing Region_Allows");
            if (!__result && tp.canBash)
            {
                __result = true;
                Log.Message("DEBUG region id: " + ___id + " was patched to allow. TraverseParms pawn is" + tp.pawn +" and mode is "+tp.mode);
            }
        }
    }
}
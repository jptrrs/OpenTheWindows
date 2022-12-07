using HarmonyLib;
using RimWorld;
using System.Reflection;
using UnityEngine;
using Verse;

namespace OpenTheWindows
{
    //Tweaks the Outdoor need calculation
    [HarmonyPatch(typeof(Need_Outdoors), nameof(Need_Outdoors.NeedInterval))]
    public static class NeedOutdoors_NeedInterval
    {
        private const float 
            Delta_IndoorsThickRoof = -0.45f,
            Delta_OutdoorsThickRoof = -0.4f,
            Delta_IndoorsThinRoof = -0.32f,
            Minimum_IndoorsThinRoof = 0.2f,
            Delta_OutdoorsThinRoof = 1f,
            Delta_IndoorsNoRoof = 5f,
            Delta_OutdoorsNoRoof = 8f,
            DeltaFactor_InBed = 0.2f;

        public static void Prefix(Need_Outdoors __instance)
        {
            NeedInterval(__instance);
        }

        public static void NeedInterval(Need_Outdoors __instance)
        {
            var pawn = __instance.pawn; //alias

            if (__instance.Disabled)
            {
                __instance.CurLevel = 1f;
                return;
            }
            if (__instance.IsFrozen)
            {
                return;
            }
            float floor = Minimum_IndoorsThinRoof;
            bool spawned = pawn.Spawned;
            Map map = pawn.Map;
            bool isOutdoors = !spawned || pawn.positionInt.UsesOutdoorTemperature(map);
            RoofDef roofDef = (!spawned) ? null : pawn.positionInt.GetRoof(map);
            float num;
            if (!isOutdoors) // indoors
            {
                if (roofDef == null)
                {
                    num = Delta_IndoorsNoRoof;
                }
                else if (!roofDef.isThickRoof)
                {
                    num = Delta_IndoorsThinRoof;
                }
                else
                {
                    num = Delta_IndoorsThickRoof;
                    floor = 0f;
                }
            }
            else if (roofDef == null)
            {
                num = Delta_OutdoorsNoRoof;
            }
            else if (roofDef.isThickRoof)
            {
                num = Delta_OutdoorsThickRoof;
            }
            else
            {
                num = Delta_OutdoorsThinRoof;
            }
            if (pawn.InBed() && num < 0f)
            {
                num *= DeltaFactor_InBed;
            }
            // no natural light penality:
            if (roofDef != null && !map.GetComponent<MapComp_Windows>().WindowCells.Contains(pawn.Position))
            {
                if (num < 0f) num *= OpenTheWindowsSettings.IndoorsNoNaturalLightPenalty;
                else num /= OpenTheWindowsSettings.IndoorsNoNaturalLightPenalty;
            }
            //
            num *= 0.0025f;
            float curLevel = __instance.CurLevel;
            if (num < 0f)
            {
                __instance.CurLevel = Mathf.Min(curLevel, Mathf.Max(curLevel + num, floor));
            }
            else
            {
                __instance.CurLevel = Mathf.Min(curLevel + num, 1f);
            }
            __instance.lastEffectiveDelta = __instance.CurLevel - curLevel;
        }
    }
}
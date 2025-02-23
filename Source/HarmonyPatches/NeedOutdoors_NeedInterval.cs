using HarmonyLib;
using RimWorld;
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

        private static float DeltaFactor_NoNaturalLight() => OpenTheWindowsSettings.IndoorsNoNaturalLightPenalty; //indoors accelerated degradation when not under windows

        public static void Prefix(Need_Outdoors __instance)
        {
            NeedInterval(__instance);
        }

        public static void NeedInterval(Need_Outdoors __instance)
        {
            Pawn pawn = __instance.pawn;
            if (__instance.Disabled)
            {
                __instance.CurLevel = 1f;
                return;
            }
            if (__instance.IsFrozen) return;
            float floor = Minimum_IndoorsThinRoof;
            bool isOutdoors = !pawn.Spawned || pawn.Position.UsesOutdoorTemperature(pawn.Map);
            RoofDef roofDef = (!pawn.Spawned) ? null : pawn.Position.GetRoof(pawn.Map);
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
            if (roofDef != null && !pawn.Map.GetComponent<MapComp_Windows>().IsUnderWindow(pawn.Position))
            {
                if (num < 0f) num *= DeltaFactor_NoNaturalLight();
                else num /= DeltaFactor_NoNaturalLight();
            }
            //
            num *= 0.0025f;
            float prevLevel = __instance.CurLevel;
            if (num < 0f)
            {
                __instance.CurLevel = Mathf.Min(prevLevel, Mathf.Max(prevLevel + num, floor));
            }
            else
            {
                __instance.CurLevel = Mathf.Min(prevLevel + num, 1f);
            }
            __instance.lastEffectiveDelta = __instance.CurLevel - prevLevel;
        }
    }
}
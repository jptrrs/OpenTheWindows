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

        private static PropertyInfo
            disabledInfo = AccessTools.Property(typeof(Need_Outdoors), "Disabled"),
            curLevelInfo = AccessTools.Property(typeof(Need_Outdoors), "CurLevel"),
            isFrozenInfo = AccessTools.Property(typeof(Need), "IsFrozen");

        private static FieldInfo 
            pawnInfo = AccessTools.Field(typeof(Need_Outdoors), "pawn"),
            lastEffectiveDeltaInfo = AccessTools.Field(typeof(Need_Outdoors), "lastEffectiveDelta");

        private static float DeltaFactor_NoNaturalLight() => OpenTheWindowsSettings.IndoorsNoNaturalLightPenalty; //indoors accelerated degradation when not under windows

        public static void Prefix(object __instance)
        {
            NeedInterval(__instance);
        }

        public static void NeedInterval(object __instance)
        {
            bool disabled = (bool)disabledInfo.GetValue(__instance, null);
            float curLevel = (float)curLevelInfo.GetValue(__instance, null);
            bool isFrozen = (bool)isFrozenInfo.GetValue(__instance, null);
            Pawn pawn = (Pawn)pawnInfo.GetValue(__instance);

            if (disabled)
            {
                curLevelInfo.SetValue(__instance, 1f, null);
                return;
            }
            if (isFrozen)
            {
                return;
            }
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
            if (roofDef != null && !pawn.Map.GetComponent<MapComp_Windows>().WindowCells.Contains(pawn.Position))
            {
                if (num < 0f) num *= DeltaFactor_NoNaturalLight();
                else num /= DeltaFactor_NoNaturalLight();
            } //
            num *= 0.0025f;
            float _curLevel = curLevel;
            if (num < 0f)
            {
                curLevelInfo.SetValue(__instance, Mathf.Min(curLevel, Mathf.Max(curLevel + num, floor)), null);
            }
            else
            {
                curLevelInfo.SetValue(__instance, Mathf.Min(curLevel + num, 1f), null);
            }
            lastEffectiveDeltaInfo.SetValue(__instance, curLevel - _curLevel);
        }
    }
}
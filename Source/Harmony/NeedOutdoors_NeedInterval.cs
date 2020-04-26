using HarmonyLib;
using RimWorld;
using System.Reflection;
using UnityEngine;
using Verse;

namespace OpenTheWindows
{
    [HarmonyPatch(typeof(Need_Outdoors), nameof(Need_Outdoors.NeedInterval))]
    public static class NeedOutdoors_NeedInterval
    {
        private const float Delta_IndoorsThickRoof = -0.45f;
        private const float Delta_OutdoorsThickRoof = -0.4f;
        private const float Delta_IndoorsThinRoof = -0.32f;
        private const float Minimum_IndoorsThinRoof = 0.2f;
        private const float Delta_OutdoorsThinRoof = 1f;
        private const float Delta_IndoorsNoRoof = 5f;
        private const float Delta_OutdoorsNoRoof = 8f;
        private const float DeltaFactor_InBed = 0.2f;

        private static float DeltaFactor_NoNaturalLight() => OpenTheWindowsSettings.IndoorsNoNaturalLightPenalty; //indoors accelerated degradation when not under windows

        public static void Prefix(object __instance)
        {
            NeedInterval(__instance);
        }

        public static void NeedInterval(object __instance)
        {
            PropertyInfo disabledInfo = AccessTools.Property(__instance.GetType(), "Disabled");
            bool disabled = (bool)disabledInfo.GetValue(__instance, null);
            PropertyInfo curLevelInfo = AccessTools.Property(__instance.GetType(), "CurLevel");
            float curLevel = (float)curLevelInfo.GetValue(__instance, null);
            PropertyInfo isFrozenInfo = AccessTools.Property(__instance.GetType().BaseType, "IsFrozen");
            bool isFrozen = (bool)isFrozenInfo.GetValue(__instance, null);
            FieldInfo pawnInfo = AccessTools.Field(typeof(Need_Outdoors), "pawn");
            Pawn pawn = (Pawn)pawnInfo.GetValue(__instance);
            FieldInfo lastEffectiveDeltaInfo = AccessTools.Field(typeof(Need_Outdoors), "lastEffectiveDelta");

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
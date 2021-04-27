using HarmonyLib;
using RimWorld;
using System.Reflection;
using UnityEngine;
using Verse;

namespace OpenTheWindows
{
    //Prevents Raises the Roof from shadowing areas lit by windows.
    public static class RoofGrid_Roofed
    {
        public static void Roofed_Postfix(RoofGrid __instance, int index, Map ___map, ref bool __result)
        {
            if (__result)
            {
                Log.Message("Postfix called!");
                IntVec3 cell = ___map.cellIndices.IndexToCell(index);
                __result = !___map.GetComponent<MapComp_Windows>().WindowCells.Contains(cell);
            }
        }
    }
}
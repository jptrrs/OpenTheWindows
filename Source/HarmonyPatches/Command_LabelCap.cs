using HarmonyLib;
using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace OpenTheWindows
{
    //Hides the icon labels.
    [HarmonyPatch(typeof(Command), nameof(Command.LabelCap), MethodType.Getter)]
    public static class Command_LabelCap
    {
        public static Dictionary<Command, Rect> Buttons = new Dictionary<Command, Rect>();
        static string Postfix(string label, Command __instance)
        {
            return Buttons.ContainsKey(__instance) && Mouse.IsOver(Buttons[__instance]) ? __instance.Label.CapitalizeFirst() : null;
        }
    }

    [HarmonyPatch(typeof(Command), nameof(Command.GizmoOnGUIInt))]
    public static class Command_GizmoOnGUIInt
    {
        public static void Prefix(Command __instance, Rect butRect)
        {
            if (!Command_LabelCap.Buttons.ContainsKey(__instance)) Command_LabelCap.Buttons.Add(__instance, butRect);
        }
        public static void Postfix(Command __instance)
        {
            if (Command_LabelCap.Buttons.ContainsKey(__instance)) Command_LabelCap.Buttons.Remove(__instance);
        }
    }

}
using HarmonyLib;
using RimWorld;
using Verse;

namespace OpenTheWindows
{
    //Replaces SwitchIsOn set functionality for CompWindow (because overriding won't cut it)
    [HarmonyPatch(typeof(CompFlickable), nameof(CompFlickable.SwitchIsOn), MethodType.Setter)]
    public static class CompFlickable_SwitchIsOn
    {
        public static bool Prefix(CompFlickable __instance, bool value)
        {
            if (__instance is CompWindow compWindow)
            {
                if (__instance.switchOnInt == value) return false;
                compWindow.switchOnInt = value;

                if (__instance.switchOnInt) compWindow.parent.BroadcastCompSignal(compWindow.Props.signal.ToString() + "On");
                else compWindow.parent.BroadcastCompSignal(compWindow.Props.signal.ToString() + "Off");

                if (compWindow.parent.Spawned) compWindow.parent.Map.mapDrawer.MapMeshDirty(compWindow.parent.Position, MapMeshFlag.Things | MapMeshFlag.Buildings);
                return false;
            }
            return true;
        }
    }
}
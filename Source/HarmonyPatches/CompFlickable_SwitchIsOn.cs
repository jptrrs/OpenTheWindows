using HarmonyLib;
using RimWorld;
using Verse;

namespace OpenTheWindows
{
    //Replaces SwitchIsOn set functionality for CompWindow (because overriding won't cut it)
    [HarmonyPatch(typeof(CompFlickable),"SwitchIsOn", MethodType.Setter)]
    public static class CompFlickable_SwitchIsOn
    {
        public static bool Prefix(CompFlickable __instance, bool value, ref bool ___switchOnInt)
        {
            if (__instance is CompWindow compWindow)
            {
                if (___switchOnInt == value)
                {
                    return false;
                }
                compWindow.switchOnInt = value;
                var parent = compWindow.parent;
                if (___switchOnInt)
                {
                    parent.BroadcastCompSignal(compWindow.FlickedOnSignal);
                }
                else
                {
                    parent.BroadcastCompSignal(compWindow.FlickedOffSignal);
                }
                if (parent.Spawned)
                {
                    parent.Map.mapDrawer.MapMeshDirty(parent.Position, MapMeshFlag.Things | MapMeshFlag.Buildings);
                }
                return false;
            }
            return true;
        }
    }
}
using System;

namespace OpenTheWindows
{
    //Creates an event from the emergency button. Manually patched if Better Pawn Control is present.
    public static class AlertManager_LoadState
    {
        //public delegate void Notify();  // delegate: "template" for the handler to be defined on the subscriber class, replaced by .Net's EventHandler

        public static event EventHandler<bool> Alarm; // event
        
        public static void LoadState_Postfix(object __instance, int level) //Raises the event
        {
            OnAlarm(__instance, level > 0);
        }

        public static void OnAlarm(object sender, bool active) //if event is not null then call delegate
        {
            Alarm?.Invoke(sender, active);
        }
    }
}
using System;

namespace OpenTheWindows
{
    //Houses the reverse patch for the AlertManager.OnAlert property. Manually patched if Better Pawn Control is present.
    static class AlertManagerProxy
    {
        public static bool onAlert => HarmonyPatcher.BetterPawnControl && OnAlert();
        public static bool OnAlert()
        {
            throw new NotImplementedException("Expected BetterPawnControl!");
        }

    }
}

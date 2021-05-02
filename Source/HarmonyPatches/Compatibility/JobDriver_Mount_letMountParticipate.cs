using Verse.AI;

namespace OpenTheWindows
{
    //Interferes with the mount job. Manually patched if Giddy Up! is present.
    public static class JobDriver_Mount_letMountParticipate
    {
        public static void letMountParticipate_Postfix(JobDriver __instance)
        {
            __instance.job.canBash = false;
        }
    }
}
using BepInEx.Bootstrap;
using DynamicMaps.ExternalModSupport.SamSWATHeliCrash;

namespace DynamicMaps.ExternalModSupport
{
    public static class ModDetection
    {
        public static bool HeliCrashLoaded { get; private set; }

        public static void CheckforMods()
        {
            // Check for the presence of SamSwats HeliCrashSides mod
            if (Chainloader.PluginInfos.ContainsKey("com.SamSWAT.HeliCrash.ArysReloaded"))
            {
                HeliCrashLoaded = true;
            }
            // Additional mod checks can be added here
        }
    }
}

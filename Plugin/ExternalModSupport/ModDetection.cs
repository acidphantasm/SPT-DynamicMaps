using BepInEx.Bootstrap;
using DynamicMaps.ExternalModSupport.SamSWATHeliCrash;

namespace DynamicMaps.ExternalModSupport
{
    public static class ModDetection
    {
        public static bool HeliCrashLoaded { get; private set; }
        public static bool FikaLoaded { get; private set; }
        public static bool FikaHeadlessLoaded { get; private set; }

        public static void CheckforMods()
        {
            // Check for the presence of SamSwats HeliCrashSides mod
            if (Chainloader.PluginInfos.ContainsKey("com.SamSWAT.HeliCrash.ArysReloaded"))
            {
                HeliCrashLoaded = true;
            }
            
            // Check for the presence of Fika mod
            if (Chainloader.PluginInfos.ContainsKey("com.fika.core"))
            {
                FikaLoaded = true;
            }
            
            // Check for the presence of Fika Headless mod
            if (Chainloader.PluginInfos.ContainsKey("com.fika.headless"))
            {
                FikaHeadlessLoaded = true;
            }
            
            // Additional mod checks can be added here
        }
    }
}

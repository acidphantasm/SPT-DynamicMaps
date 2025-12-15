using DynamicMaps.UI;
using DynamicMaps.Utils;
using EFT.UI;
using EFT.UI.Map;
using HarmonyLib;
using Newtonsoft.Json;
using SPT.Common.Http;
using SPT.Reflection.Patching;
using SPT.Reflection.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using static DynamicMaps.UI.ModdedMapScreen;

namespace DynamicMaps.Patches
{
    internal class MenuLoadPatch : ModulePatch
    {
        private static bool serverConfigLoaded = false;

        protected override MethodBase GetTargetMethod()
        {
            return PatchConstants.EftTypes
                .SingleCustom(x => x.GetField("Taxonomy", BindingFlags.Public | BindingFlags.Instance) != null)
                .GetMethod("Create", BindingFlags.Public | BindingFlags.Static);
        }

        [PatchPrefix]
        public static async void PatchPostfix()
        {
            try
            {
                if (serverConfigLoaded == false)
                {
                    serverConfigLoaded = true;
                    ModdedMapScreen._config = new(await LoadFromServer());

                    Plugin.Log.LogInfo($"Loaded server config");
                }
                else return;
            }
            catch (Exception e)
            {
                Plugin.Log.LogError($"Caught error while trying to load config");
                Plugin.Log.LogError($"{e.Message}");
                Plugin.Log.LogError($"{e.StackTrace}");
            }
        }

        private static async Task<DMServerConfig> LoadFromServer()
        {
            try
            {
                string payload = await RequestHandler.GetJsonAsync("/dynamicmaps/load");
                return JsonConvert.DeserializeObject<DMServerConfig>(payload);

            }
            catch (Exception ex)
            {
                Plugin.Log.LogError("Failed to load: " + ex.ToString());
                NotificationManagerClass.DisplayWarningNotification("Failed to load Dynamic Maps server config - check the server");
                return null;
            }
        }

        internal class DMServerConfig
        {
            public bool AllowShowPlayerMarker { get; set; } = true;
            public bool AllowShowFriendlyPlayerMarkersInRaid { get; set; } = true;
            public bool AllowShowEnemyPlayerMarkersInRaid { get; set; } = true;
            public bool AllowShowScavMarkersInRaid { get; set; } = true;
            public bool AllowShowBossMarkersInRaid { get; set; } = true;
            public bool AllowShowLockedDoorStatus { get; set; } = true;
            public bool AllowShowQuestsInRaid { get; set; } = true;
            public bool AllowShowExtractsInRaid { get; set; } = true;
            public bool AllowShowExtractStatusInRaid { get; set; } = true;
            public bool AllowShowTransitPointsInRaid { get; set; } = true;
            public bool AllowShowSecretExtractsInRaid { get; set; } = true;
            public bool AllowShowDroppedBackpackInRaid { get; set; } = true;
            public bool AllowShowWishlistedItemsInRaid { get; set; } = true;
            public bool AllowShowBTRInRaid { get; set; } = true;
            public bool AllowShowAirdropsInRaid { get; set; } = true;
            public bool AllowShowHiddenStashesInRaid { get; set; } = true;
            public bool AllowShowFriendlyCorpses { get; set; } = true;
            public bool AllowShowKilledCorpses { get; set; } = true;
            public bool AllowShowFriendlyKilledCorpses { get; set; } = true;
            public bool AllowShowBossCorpses { get; set; } = true;
            public bool AllowShowOtherCorpses { get; set; } = true;
            public bool AllowShowHeliCrashSiteInRaid { get; set; } = true;
            public bool AllowMiniMap { get; set; } = true;
            public bool RequireMapInInventory { get; set; } = false;
            public int ShowScavIntelLevel { get; set; } = 0;
            public int ShowPmcIntelLevel { get; set; } = 0;
            public int ShowBossIntelLevel { get; set; } = 0;
            public int ShowFriendlyIntelLevel { get; set; } = 0;
            public int ShowAirdropIntelLevel { get; set; } = 0;
            public int ShowCorpseIntelLevel { get; set; } = 0;
            public int ShowWishListIntelLevel { get; set; } = 0;
            public int ShowHiddenStashIntelLevel { get; set; } = 0;
        }
    }
}

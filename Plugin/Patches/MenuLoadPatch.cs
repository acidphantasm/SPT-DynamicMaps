using DynamicMaps.UI;
using Newtonsoft.Json;
using SPT.Common.Http;
using SPT.Reflection.Patching;
using SPT.Reflection.Utils;
using System;
using System.Reflection;
using System.Threading.Tasks;
using EFT.Communications;
using static DynamicMaps.UI.ModdedMapScreen;

namespace DynamicMaps.Patches
{
    internal class MenuLoadPatch : ModulePatch
    {
        private static bool _serverConfigLoaded;

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
                if (_serverConfigLoaded)
                {
                    return;
                }
                
                _serverConfigLoaded = true;
                ModdedMapScreen.ServerConfig = await LoadFromServer();

                Plugin.Log.LogInfo($"Loaded server config");
            }
            catch (Exception e)
            {
                Plugin.Log.LogError($"Caught error while trying to load config");
                Plugin.Log.LogError($"{e.Message}");
                Plugin.Log.LogError($"{e.StackTrace}");
            }
        }
        private static async Task<DmServerConfig> LoadFromServer()
        {
            try
            {
                var payload = await RequestHandler.GetJsonAsync("/dynamicmaps/load");
                var config = JsonConvert.DeserializeObject<DmServerConfig>(payload);
                return config ?? throw new InvalidOperationException("Server config was empty");

            }
            catch (Exception ex)
            {
                Plugin.Log.LogError("Failed to load: " + ex.ToString());
                NotificationManager.DisplayWarningNotification("Failed to load Dynamic Maps server config - check the server");
                return null;
            }
        }
    }
}

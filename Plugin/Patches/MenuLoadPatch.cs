using DynamicMaps.Common;
using DynamicMaps.UI;
using Newtonsoft.Json;
using SPT.Common.Http;
using SPT.Reflection.Patching;
using SPT.Reflection.Utils;
using System;
using System.Reflection;
using System.Threading.Tasks;

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

        private static async Task<ModConfig> LoadFromServer()
        {
            try
            {
                string payload = await RequestHandler.GetJsonAsync(Routes.LoadConfigRoute);
                return JsonConvert.DeserializeObject<ModConfig>(payload);

            }
            catch (Exception ex)
            {
                Plugin.Log.LogError("Failed to load: " + ex.ToString());
                NotificationManagerClass.DisplayWarningNotification("Failed to load Dynamic Maps server config - check the server");
                return null;
            }
        }
    }
}

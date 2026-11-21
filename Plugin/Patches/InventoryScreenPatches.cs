using System;
using System.Linq;
using System.Reflection;
using Comfort.Common;
using EFT.UI;
using HarmonyLib;
using SPT.Reflection.Patching;

namespace DynamicMaps.Patches
{
    // reopen the map tab when coming back from the flea market if it was opened from the map
    internal class InventoryScreenShowPatch : ModulePatch
    {
        private static object _returnToMapController;

        internal static void ReturnToMapOnNextShow()
        {
            var inventoryScreen = Singleton<CommonUI>.Instance?.InventoryScreen;
            if (inventoryScreen == null)
                return;

            _returnToMapController = Traverse.Create(inventoryScreen).Field("ScreenController").GetValue();
        }

        protected override MethodBase GetTargetMethod()
        {
            return typeof(InventoryScreen)
                .GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
                .Single(m => m.Name == nameof(InventoryScreen.Show) && m.GetParameters().Length == 1);
        }

        [PatchPrefix]
        public static void PatchPrefix(object __0)
        {
            if (_returnToMapController == null)
                return;

            try
            {
                if (ReferenceEquals(__0, _returnToMapController))
                {
                    Traverse.Create(__0).Property("LastSelectedTab").SetValue((EInventoryTab?)EInventoryTab.Map);
                }
            }
            catch (Exception e)
            {
                Plugin.Log.LogError($"Failed to restore map tab: {e}");
            }
            finally
            {
                _returnToMapController = null;
            }
        }
    }
}

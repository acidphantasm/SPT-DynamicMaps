using BepInEx;
using BepInEx.Logging;
using Comfort.Common;
using DrakiaXYZ.VersionChecker;
using DynamicMaps.Config;
using DynamicMaps.Patches;
using DynamicMaps.UI;
using DynamicMaps.Utils;
using EFT;
using EFT.UI;
using EFT.UI.Map;
using SPT.Custom.Utils;
using System;
using System.Diagnostics;
using System.Reflection;

namespace DynamicMaps
{
    [BepInPlugin("com.mpstark.dynamicmaps", "DynamicMaps", BuildInfo.Version)]
    [BepInDependency("com.SPT.custom", "4.1.0")]
    [BepInDependency("com.SamSWAT.HeliCrash.ArysReloaded", BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency("com.fika.core", BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency("com.fika.headless", BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency("com.terkoiz.freecam", BepInDependency.DependencyFlags.SoftDependency)]
    [BepInIncompatibility("com.blackhawk.dynamicmapsextended")]
    public class Plugin : BaseUnityPlugin
    {
        public const int TarkovVersion = 40743;
        public static Plugin Instance;
        public static ManualLogSource Log
        {
            get => Instance.Logger;
        }
        public static readonly string Path = System.IO.Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

        public ModdedMapScreen Map;

        internal void Awake()
        {
            if (!VersionChecker.CheckEftVersion(Logger, Info, Config))
            {
                throw new Exception("Invalid EFT Version");
            }

            ExternalModSupport.ModDetection.CheckforMods();

            Settings.Init(Config);
            Config.SettingChanged += (x, y) => Map?.ReadConfig();
            
            Instance = this;

            new BattleUIScreenShowPatch().Enable();
            new CommonUIAwakePatch().Enable();
            new MapScreenShowPatch().Enable();
            new MapScreenClosePatch().Enable();
            new GameStartedPatch().Enable();
            new GameWorldOnDestroyPatch().Enable();
            new GameWorldUnregisterPlayerPatch().Enable();
            new LootItemInitPatch().Enable();
            new GameWorldDestroyLootPatch().Enable();
            new AirdropBoxOnBoxLandPatch().Enable();
            new PlayerOnDeadPatch().Enable();
            new PlayerInventoryThrowItemPatch().Enable();
            new ShowViewButtonPatch().Enable();
            new MenuLoadPatch().Enable();
        }

        internal void TryAttachToMapScreen(MapScreen mapScreen)
        {
            if (Map != null) return;
            
            Log.LogInfo("Trying to attach to MapScreen");

            Map = ModdedMapScreen.Create(Singleton<CommonUI>.Instance.gameObject);
            Map.transform.SetParent(mapScreen.transform);
        }

        internal void TryAttachToRaidCanvas()
        {
            if (!GameUtils.HasActiveRaidDetails())
            {
                GameUtils.SetRaidDetails();
            }
            
            if (Map == null || !GameUtils.HasActiveRaidDetails())
            {
                return;
            }
            
            Map.TryAddPeekComponent();
        }

        static Plugin()
        {
            if (Type.GetType("Unity.VectorGraphics.SVGParser, Unity.VectorGraphics") != null)
            {
                return;
            }
            
            MessageBoxHelper.Show("You have incorrectly installed Dynamic Maps.\nPlease reinstall correctly.\nQuitting.", "DynamicMaps Error", MessageBoxHelper.MessageBoxType.OK);
            Process.GetCurrentProcess().Kill();
        }
    }
}

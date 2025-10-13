﻿using System;
using System.Reflection;
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

namespace DynamicMaps
{
    // the version number here is generated on build and may have a warning if not yet built
    [BepInPlugin("com.mpstark.DynamicMaps", "DynamicMaps", BuildInfo.Version)]
    [BepInDependency("com.SPT.custom", "4.0.0")]
    [BepInDependency("com.SamSWAT.HeliCrash.ArysReloaded", BepInDependency.DependencyFlags.SoftDependency)]
    public class Plugin : BaseUnityPlugin
    {
        public const int TarkovVersion = 40087;
        public static Plugin Instance;
        public static ManualLogSource Log => Instance.Logger;
        public static string Path = System.IO.Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

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

            // Logger.LogWarning("TEST BUILD OF DYNAMIC MAPS, NO SUPPORT OFFERED.");
            
            Instance = this;

            // patches
            new BattleUIScreenShowPatch().Enable();
            new CommonUIAwakePatch().Enable();
            new MapScreenShowPatch().Enable();
            new MapScreenClosePatch().Enable();
            new GameStartedPatch().Enable();
            new GameWorldOnDestroyPatch().Enable();
            new GameWorldUnregisterPlayerPatch().Enable();
            new GameWorldRegisterLootItemPatch().Enable();
            new GameWorldDestroyLootPatch().Enable();
            new AirdropBoxOnBoxLandPatch().Enable();
            new PlayerOnDeadPatch().Enable();
            new PlayerInventoryThrowItemPatch().Enable();
            new ShowViewButtonPatch().Enable();
            new MenuLoadPatch().Enable();
        }

        /// <summary>
        /// Attach to the map screen
        /// </summary>
        internal void TryAttachToMapScreen(MapScreen mapScreen)
        {
            if (Map is not null) return;
            
            Log.LogInfo("Trying to attach to MapScreen");

            // attach to common UI first to call awake and set things up, then attach to sleeping map screen
            Map = ModdedMapScreen.Create(Singleton<CommonUI>.Instance.gameObject);
            Map.transform.SetParent(mapScreen.transform);
        }

        /// <summary>
        /// Attach the peek component
        /// </summary>
        internal void TryAttachToBattleUIScreen(EftBattleUIScreen battleUI)
        {
            if (Map is null || GameUtils.GetMainPlayer() is HideoutPlayer)
            {
                return;
            }
            
            Map.TryAddPeekComponent(battleUI);
        }
    }
}

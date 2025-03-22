using System;
using System.Collections.Generic;
using BepInEx.Configuration;
using DynamicMaps.ExternalModSupport;
using DynamicMaps.Utils;
using UnityEngine;

// THIS IS HEAVILY BASED ON DRAKIAXYZ'S SPT-QuickMoveToContainer
namespace DynamicMaps.Config
{
    internal static class Settings
    {
        private static readonly List<ConfigEntryBase> ConfigEntries = [];

        #region General

        private const string GeneralTitle = "1. General";
        public static ConfigEntry<bool> ReplaceMapScreen;
        public static ConfigEntry<KeyboardShortcut> CenterOnPlayerHotkey;
        public static ConfigEntry<KeyboardShortcut> DumpInfoHotkey;
        public static ConfigEntry<KeyboardShortcut> MoveMapUpHotkey;
        public static ConfigEntry<KeyboardShortcut> MoveMapDownHotkey;
        public static ConfigEntry<KeyboardShortcut> MoveMapLeftHotkey;
        public static ConfigEntry<KeyboardShortcut> MoveMapRightHotkey;
        public static ConfigEntry<float> MapMoveHotkeySpeed;
        public static ConfigEntry<KeyboardShortcut> ChangeMapLevelUpHotkey;
        public static ConfigEntry<KeyboardShortcut> ChangeMapLevelDownHotkey;
        public static ConfigEntry<KeyboardShortcut> ZoomMapInHotkey;
        public static ConfigEntry<KeyboardShortcut> ZoomMapOutHotkey;
        public static ConfigEntry<float> ZoomMapHotkeySpeed;

        #endregion
        
        #region Dynamic Markers

        private const string DynamicMarkerTitle = "2. Dynamic Markers";
        public static ConfigEntry<bool> ShowPlayerMarker;
        public static ConfigEntry<bool> ShowFriendlyPlayerMarkersInRaid;
        public static ConfigEntry<bool> ShowEnemyPlayerMarkersInRaid;
        public static ConfigEntry<bool> ShowScavMarkersInRaid;
        public static ConfigEntry<bool> ShowBossMarkersInRaid;
        public static ConfigEntry<bool> ShowLockedDoorStatus;
        public static ConfigEntry<bool> ShowQuestsInRaid;
        public static ConfigEntry<bool> ShowExtractsInRaid;
        public static ConfigEntry<bool> ShowExtractStatusInRaid;
        public static ConfigEntry<bool> ShowTransitPointsInRaid;
        public static ConfigEntry<bool> ShowSecretPointsInRaid;
        public static ConfigEntry<bool> ShowDroppedBackpackInRaid;
        public static ConfigEntry<bool> ShowWishListItemsInRaid;
        public static ConfigEntry<bool> ShowBTRInRaid;
        public static ConfigEntry<bool> ShowAirdropsInRaid;
        public static ConfigEntry<bool> ShowHiddenStashesInRaid;
        public static ConfigEntry<bool> ShowFriendlyCorpsesInRaid;
        public static ConfigEntry<bool> ShowKilledCorpsesInRaid;
        public static ConfigEntry<bool> ShowFriendlyKilledCorpsesInRaid;
        public static ConfigEntry<bool> ShowBossCorpsesInRaid;
        public static ConfigEntry<bool> ShowOtherCorpsesInRaid;
        
        #endregion

        #region IntelCenter

        private const string ProgressionTitle = "3. Progression";
        public static ConfigEntry<bool> RequireMapInInventory;
        public static ConfigEntry<int> ShowPmcIntelLevel;
        public static ConfigEntry<int> ShowBossIntelLevel;
        public static ConfigEntry<int> ShowScavIntelLevel;
        public static ConfigEntry<int> ShowFriendlyIntelLevel;
        public static ConfigEntry<int> ShowCorpseIntelLevel;
        public static ConfigEntry<int> ShowAirdropIntelLevel;
        public static ConfigEntry<int> ShowWishListItemsIntelLevel;
        public static ConfigEntry<int> ShowHiddenStashIntelLevel;
        
        #endregion
        
        #region In Raid

        private const string InRaidTitle = "4. In-Raid";
        public static ConfigEntry<bool> ResetZoomOnCenter;
        public static ConfigEntry<float> CenteringZoomResetPoint;
        public static ConfigEntry<float> ZoomMainMap;
        public static ConfigEntry<bool> AutoCenterOnPlayerMarker;
        public static ConfigEntry<bool> RetainMapPosition;
        public static ConfigEntry<bool> AutoSelectLevel;
        public static ConfigEntry<KeyboardShortcut> PeekShortcut;
        public static ConfigEntry<bool> HoldForPeek;

        #endregion

        #region Mini Map

        private const string MiniMapTitle = "5. Mini-map";
        public static ConfigEntry<bool> MiniMapEnabled;
        public static ConfigEntry<EMiniMapPosition> MiniMapPosition;
        public static ConfigEntry<float> MiniMapSizeX;
        public static ConfigEntry<float> MiniMapSizeY;
        public static ConfigEntry<float> MiniMapScreenOffsetX;
        public static ConfigEntry<float> MiniMapScreenOffsetY;
        public static ConfigEntry<bool> MapTransitionEnabled;
        public static ConfigEntry<KeyboardShortcut> MiniMapShowOrHide;
        public static ConfigEntry<float> ZoomMiniMap;
        public static ConfigEntry<KeyboardShortcut> ZoomInMiniMapHotkey;
        public static ConfigEntry<KeyboardShortcut> ZoomOutMiniMapHotkey;
        
        #endregion

        #region Colors

        private const string MarkerColors = "6. Marker Colors";
        public static ConfigEntry<Color> PlayerColor;
        public static ConfigEntry<Color> PmcBearColor;
        public static ConfigEntry<Color> PmcUsecColor;
        public static ConfigEntry<Color> ScavColor;
        public static ConfigEntry<Color> BossColor;
        public static ConfigEntry<Color> AirdropColor;
        public static ConfigEntry<Color> BackpackColor;
        public static ConfigEntry<Color> LootItemColor;
        public static ConfigEntry<Color> KilledCorpseColor;
        public static ConfigEntry<Color> KilledBossColor;
        public static ConfigEntry<Color> KilledOtherColor;
        public static ConfigEntry<Color> BtrColor;
        public static ConfigEntry<Color> ExtractDefaultColor;
        public static ConfigEntry<Color> ExtractOpenColor;
        public static ConfigEntry<Color> ExtractClosedColor;
        public static ConfigEntry<Color> ExtractHasRequirementsColor;
        public static ConfigEntry<Color> TransPointColor;
        public static ConfigEntry<Color> SecretPointColor;
        public static ConfigEntry<Color> HiddenStashColor;
        
        #endregion
        
        #region External Mod Support
        
        private const string ExternModSupport = "7. External Mod Support";
        public static ConfigEntry<bool> ShowHeliCrashMarker;
        
        #endregion
        
        // public static ConfigEntry<KeyboardShortcut> KeyboardShortcut;

        public static void Init(ConfigFile config)
        {
            #region General

            ConfigEntries.Add(ReplaceMapScreen = config.Bind(
                GeneralTitle,
                "Replace Map Screen",
                true,
                new ConfigDescription(
                    "If the map should replace the BSG default map screen, requires swapping away from modded map to refresh",
                    null,
                    new ConfigurationManagerAttributes { })));

            ConfigEntries.Add(CenterOnPlayerHotkey = config.Bind(
                GeneralTitle,
                "Center on Player Hotkey",
                new KeyboardShortcut(KeyCode.Semicolon),
                new ConfigDescription(
                    "Pressed while the map is open, centers the player",
                    null,
                    new ConfigurationManagerAttributes { })));

            ConfigEntries.Add(MoveMapUpHotkey = config.Bind(
                GeneralTitle,
                "Move Map Up Hotkey",
                new KeyboardShortcut(KeyCode.UpArrow),
                new ConfigDescription(
                    "Hotkey to move the map up",
                    null,
                    new ConfigurationManagerAttributes { })));

            ConfigEntries.Add(MoveMapDownHotkey = config.Bind(
                GeneralTitle,
                "Move Map Down Hotkey",
                new KeyboardShortcut(KeyCode.DownArrow),
                new ConfigDescription(
                    "Hotkey to move the map down",
                    null,
                    new ConfigurationManagerAttributes { })));

            ConfigEntries.Add(MoveMapLeftHotkey = config.Bind(
                GeneralTitle,
                "Move Map Left Hotkey",
                new KeyboardShortcut(KeyCode.LeftArrow),
                new ConfigDescription(
                    "Hotkey to move the map left",
                    null,
                    new ConfigurationManagerAttributes { })));

            ConfigEntries.Add(MoveMapRightHotkey = config.Bind(
                GeneralTitle,
                "Move Map Right Hotkey",
                new KeyboardShortcut(KeyCode.RightArrow),
                new ConfigDescription(
                    "Hotkey to move the map right",
                    null,
                    new ConfigurationManagerAttributes { })));

            ConfigEntries.Add(MapMoveHotkeySpeed = config.Bind(
                GeneralTitle,
                "Move Map Hotkey Speed",
                0.25f,
                new ConfigDescription(
                    "How fast the map should move, units are map percent per second",
                    new AcceptableValueRange<float>(0.05f, 2f),
                    new ConfigurationManagerAttributes { })));

            ConfigEntries.Add(ChangeMapLevelUpHotkey = config.Bind(
                GeneralTitle,
                "Change Map Level Up Hotkey",
                new KeyboardShortcut(KeyCode.Period),
                new ConfigDescription(
                    "Hotkey to move the map level up (shift-scroll-up also does this in map screen)",
                    null,
                    new ConfigurationManagerAttributes { })));

            ConfigEntries.Add(ChangeMapLevelDownHotkey = config.Bind(
                GeneralTitle,
                "Change Map Level Down Hotkey",
                new KeyboardShortcut(KeyCode.Comma),
                new ConfigDescription(
                    "Hotkey to move the map level down (shift-scroll-down also does this in map screen)",
                    null,
                    new ConfigurationManagerAttributes { })));

            ConfigEntries.Add(ZoomMapInHotkey = config.Bind(
                GeneralTitle,
                "Zoom Map In Hotkey",
                new KeyboardShortcut(KeyCode.Equals),
                new ConfigDescription(
                    "Hotkey to zoom the map in (scroll-up also does this in map screen)",
                    null,
                    new ConfigurationManagerAttributes { })));

            ConfigEntries.Add(ZoomMapOutHotkey = config.Bind(
                GeneralTitle,
                "Zoom Map Out Hotkey",
                new KeyboardShortcut(KeyCode.Minus),
                new ConfigDescription(
                    "Hotkey to zoom the map out (scroll-down also does this in map screen)",
                    null,
                    new ConfigurationManagerAttributes { })));

            ConfigEntries.Add(ZoomMapHotkeySpeed = config.Bind(
                GeneralTitle,
                "Zoom Map Hotkey Speed",
                2.5f,
                new ConfigDescription(
                    "How fast the map should zoom by hotkey",
                    new AcceptableValueRange<float>(1f, 10f),
                    new ConfigurationManagerAttributes { })));

            ConfigEntries.Add(DumpInfoHotkey = config.Bind(
                GeneralTitle,
                "Dump Info Hotkey",
                new KeyboardShortcut(KeyCode.D, KeyCode.LeftShift, KeyCode.LeftAlt),
                new ConfigDescription(
                    "Pressed while the map is open, dumps json MarkerDefs for extracts, loot, and switches into root of plugin folder",
                    null,
                    new ConfigurationManagerAttributes { IsAdvanced = true })));

            #endregion

            #region Dynamic Markers

            ConfigEntries.Add(ShowPlayerMarker = config.Bind(
                DynamicMarkerTitle,
                "Show Player Marker",
                true,
                new ConfigDescription(
                    "If the player marker should be shown in raid",
                    null,
                    new ConfigurationManagerAttributes { })));

            ConfigEntries.Add(ShowFriendlyPlayerMarkersInRaid = config.Bind(
                DynamicMarkerTitle,
                "Show Friendly Player Markers",
                true,
                new ConfigDescription(
                    "If friendly player markers should be shown in-raid",
                    null,
                    new ConfigurationManagerAttributes { })));

            ConfigEntries.Add(ShowEnemyPlayerMarkersInRaid = config.Bind(
                DynamicMarkerTitle,
                "Show Enemy Player Markers",
                false,
                new ConfigDescription(
                    "If enemy player markers should be shown in-raid (generally for debug)",
                    null,
                    new ConfigurationManagerAttributes { })));

            ConfigEntries.Add(ShowScavMarkersInRaid = config.Bind(
                DynamicMarkerTitle,
                "Show Scav Markers",
                false,
                new ConfigDescription(
                    "If enemy scav markers should be shown in-raid (generally for debug)",
                    null,
                    new ConfigurationManagerAttributes { })));

            ConfigEntries.Add(ShowBossMarkersInRaid = config.Bind(
                DynamicMarkerTitle,
                "Show Boss Markers",
                false,
                new ConfigDescription(
                    "If enemy boss markers should be shown in-raid",
                    null,
                    new ConfigurationManagerAttributes { })));

            ConfigEntries.Add(ShowLockedDoorStatus = config.Bind(
                DynamicMarkerTitle,
                "Show Locked Door Status",
                true,
                new ConfigDescription(
                    "If locked door markers should be updated with status based on key acquisition",
                    null,
                    new ConfigurationManagerAttributes { })));

            ConfigEntries.Add(ShowQuestsInRaid = config.Bind(
                DynamicMarkerTitle,
                "Show Quests In Raid",
                true,
                new ConfigDescription(
                    "If quests should be shown in raid",
                    null,
                    new ConfigurationManagerAttributes { })));

            ConfigEntries.Add(ShowExtractsInRaid = config.Bind(
                DynamicMarkerTitle,
                "Show Extracts In Raid",
                true,
                new ConfigDescription(
                    "If extracts should be shown in raid",
                    null,
                    new ConfigurationManagerAttributes { })));

            ConfigEntries.Add(ShowExtractStatusInRaid = config.Bind(
                DynamicMarkerTitle,
                "Show Extracts Status In Raid",
                true,
                new ConfigDescription(
                    "If extracts should be colored according to their status in raid",
                    null,
                    new ConfigurationManagerAttributes { })));
            
            ConfigEntries.Add(ShowTransitPointsInRaid = config.Bind(
                DynamicMarkerTitle,
                "Show Transit Points In Raid",
                true,
                new ConfigDescription(
                    "If transits should be shown in raid",
                    null,
                    new ConfigurationManagerAttributes { })));

            ConfigEntries.Add(ShowSecretPointsInRaid = config.Bind(
                DynamicMarkerTitle,
                "Show Secret Exfil Points In Raid",
                true,
                new ConfigDescription(
                    "If secret exfils should be shown in raid",
                    null,
                    new ConfigurationManagerAttributes { })));

            ConfigEntries.Add(ShowDroppedBackpackInRaid = config.Bind(
                DynamicMarkerTitle,
                "Show Dropped Backpack In Raid",
                true,
                new ConfigDescription(
                    "If the player's dropped backpacks (not anyone elses) should be shown in raid",
                    null,
                    new ConfigurationManagerAttributes { })));
            
            ConfigEntries.Add(ShowWishListItemsInRaid = config.Bind(
                DynamicMarkerTitle,
                "Show wish listed items In Raid",
                true,
                new ConfigDescription(
                    "Shows items that are in your wishlist on the map in raid",
                    null,
                    new ConfigurationManagerAttributes { })));

            ConfigEntries.Add(ShowBTRInRaid = config.Bind(
                DynamicMarkerTitle,
                "Show BTR In Raid",
                true,
                new ConfigDescription(
                    "If the BTR should be shown in raid",
                    null,
                    new ConfigurationManagerAttributes { })));

            ConfigEntries.Add(ShowAirdropsInRaid = config.Bind(
                DynamicMarkerTitle,
                "Show Airdrops In Raid",
                true,
                new ConfigDescription(
                    "If airdrops should be shown in raid when they land",
                    null,
                    new ConfigurationManagerAttributes { })));
            
            ConfigEntries.Add(ShowHiddenStashesInRaid = config.Bind(
                DynamicMarkerTitle,
                "Show Hidden Stashes In Raid",
                true,
                new ConfigDescription(
                    "If hidden stashes should be shown in raid",
                    null,
                    new ConfigurationManagerAttributes { })));

            ConfigEntries.Add(ShowFriendlyCorpsesInRaid = config.Bind(
                DynamicMarkerTitle,
                "Show Friendly Corpses In Raid",
                true,
                new ConfigDescription(
                    "If friendly corpses should be shown in raid",
                    null,
                    new ConfigurationManagerAttributes { })));

            ConfigEntries.Add(ShowKilledCorpsesInRaid = config.Bind(
                DynamicMarkerTitle,
                "Show Player-killed Corpses In Raid",
                true,
                new ConfigDescription(
                    "If corpses killed by the player should be shown in raid, killed bosses will be shown in another color",
                    null,
                    new ConfigurationManagerAttributes { })));

            ConfigEntries.Add(ShowFriendlyKilledCorpsesInRaid = config.Bind(
                DynamicMarkerTitle,
                "Show Friendly-killed Corpses In Raid",
                true,
                new ConfigDescription(
                    "If corpses killed by friendly players should be shown in raid, killed bosses will be shown in another color",
                    null,
                    new ConfigurationManagerAttributes { })));

            ConfigEntries.Add(ShowBossCorpsesInRaid = config.Bind(
                DynamicMarkerTitle,
                "Show Boss Corpses In Raid",
                false,
                new ConfigDescription(
                    "If boss corpses (other than ones killed by the player) should be shown in raid",
                    null,
                    new ConfigurationManagerAttributes { })));

            ConfigEntries.Add(ShowOtherCorpsesInRaid = config.Bind(
                DynamicMarkerTitle,
                "Show Other Corpses In Raid",
                false,
                new ConfigDescription(
                    "If corpses (other than friendly ones or ones killed by the player) should be shown in raid",
                    null,
                    new ConfigurationManagerAttributes { })));

            #endregion
            
            #region Progression

            ConfigEntries.Add(RequireMapInInventory = config.Bind(
                ProgressionTitle,
                "Require a map in your inventory",
                false,
                new ConfigDescription(
                    "Requires you to have a map in your inventory in order to view the map in raid.",
                    null,
                    new ConfigurationManagerAttributes { })));
            
            ConfigEntries.Add(ShowPmcIntelLevel = config.Bind(
                ProgressionTitle,
                "Intel level required to show PMCs",
                0,
                new ConfigDescription(
                    "If intel level is at or below this value it will show PMCs",
                    new AcceptableValueRange<int>(0, 3),
                    new ConfigurationManagerAttributes { })));
            
            ConfigEntries.Add(ShowBossIntelLevel = config.Bind(
                ProgressionTitle,
                "Intel level required to show bosses",
                0,
                new ConfigDescription(
                    "If intel level is at or below this value it will show bosses",
                    new AcceptableValueRange<int>(0, 3),
                    new ConfigurationManagerAttributes { })));
            
            ConfigEntries.Add(ShowScavIntelLevel = config.Bind(
                ProgressionTitle,
                "Intel level required to show scavs",
                0,
                new ConfigDescription(
                    "If intel level is at or below this value it will show scavs",
                    new AcceptableValueRange<int>(0, 3),
                    new ConfigurationManagerAttributes { })));
            
            ConfigEntries.Add(ShowFriendlyIntelLevel = config.Bind(
                ProgressionTitle,
                "Intel level required to show friendly PMCs",
                0,
                new ConfigDescription(
                    "If intel level is at or below this value it will show friendly PMCs",
                    new AcceptableValueRange<int>(0, 3),
                    new ConfigurationManagerAttributes { })));
            
            ConfigEntries.Add(ShowCorpseIntelLevel = config.Bind(
                ProgressionTitle,
                "Intel level required to show corpses",
                0,
                new ConfigDescription(
                    "If intel level is at or below this value it will show corpses",
                    new AcceptableValueRange<int>(0, 3),
                    new ConfigurationManagerAttributes { })));
            
            ConfigEntries.Add(ShowAirdropIntelLevel = config.Bind(
                ProgressionTitle,
                "Intel level required to show airdrops",
                0,
                new ConfigDescription(
                    "If intel level is at or below this value it will show airdrops",
                    new AcceptableValueRange<int>(0, 3),
                    new ConfigurationManagerAttributes { })));
            
            ConfigEntries.Add(ShowWishListItemsIntelLevel = config.Bind(
                ProgressionTitle,
                "Intel level required to show wish listed loot items",
                0,
                new ConfigDescription(
                    "If intel level is at or below this value it will show wish listed loot items",
                    new AcceptableValueRange<int>(0, 3),
                    new ConfigurationManagerAttributes { })));
            
            ConfigEntries.Add(ShowHiddenStashIntelLevel = config.Bind(
                ProgressionTitle,
                "Intel level required to show hidden stashes",
                0,
                new ConfigDescription(
                    "If intel level is at or below this value it will show hidden stashes",
                    new AcceptableValueRange<int>(0, 3),
                    new ConfigurationManagerAttributes { })));

            #endregion
            
            #region In Raid

             ConfigEntries.Add(AutoSelectLevel = config.Bind(
                InRaidTitle,
                "Auto Select Level",
                true,
                new ConfigDescription(
                    "If the level should be automatically selected based on the players position in raid",
                    null,
                    new ConfigurationManagerAttributes { })));

            ConfigEntries.Add(AutoCenterOnPlayerMarker = config.Bind(
                InRaidTitle,
                "Auto Center On Player Marker",
                false,
                new ConfigDescription(
                    "If the player marker should be centered when showing the map in raid (Conflicts with 'Remember Map Position')",
                    null,
                    new ConfigurationManagerAttributes { })));

            ConfigEntries.Add(ResetZoomOnCenter = config.Bind(
                InRaidTitle,
                "Reset Zoom On Center",
                false,
                new ConfigDescription(
                    "If the zoom level should be reset each time that the map is opened while in raid (Conflicts with 'Remember Map Position')",
                    null,
                    new ConfigurationManagerAttributes { })));

            ConfigEntries.Add(RetainMapPosition = config.Bind(
                InRaidTitle,
                "Remember Map Position",
                true,
                new ConfigDescription(
                    "Should we remember the map position (Map position memory is only maintained for the current raid) (Conflicts with 'Auto Center On Player Marker' and 'Reset Zoom On Center')",
                    null,
                    new ConfigurationManagerAttributes { })));
            
            ConfigEntries.Add(CenteringZoomResetPoint = config.Bind(
                InRaidTitle,
                "Centering On Player Zoom Level",
                0.15f,
                new ConfigDescription(
                    "What zoom level should be used as while centering on the player (0 is fully zoomed out, and 1 is fully zoomed in)",
                    new AcceptableValueRange<float>(0f, 1f),
                    new ConfigurationManagerAttributes { })));
            
            ConfigEntries.Add(ZoomMainMap = config.Bind(
                InRaidTitle,
                "Main map zoom",
                0f,
                new ConfigDescription(
                    "What zoom level should be used for the main map. (Tab view/Peek view) (0 is fully zoomed out, and 1 is fully zoomed in)",
                    new AcceptableValueRange<float>(0f, 15f),
                    new ConfigurationManagerAttributes { })));
            
            ConfigEntries.Add(PeekShortcut = config.Bind(
                InRaidTitle,
                "Peek at Map Shortcut",
                new KeyboardShortcut(KeyCode.M),
                new ConfigDescription(
                    "The keyboard shortcut to peek at the map",
                    null,
                    new ConfigurationManagerAttributes { })));

            ConfigEntries.Add(HoldForPeek = config.Bind(
                InRaidTitle,
                "Hold for Peek",
                true,
                new ConfigDescription(
                    "If the shortcut should be held to keep it open. If disabled, button toggles",
                    null,
                    new ConfigurationManagerAttributes { })));
            
            ConfigEntries.Add(MapTransitionEnabled = config.Bind(
                InRaidTitle,
                "Peek Transition enabled",
                true,
                new ConfigDescription(
                    "Enable the map transition animations (When disabled everything will snap)",
                    null,
                    new ConfigurationManagerAttributes { })));

            #endregion

            AutoCenterOnPlayerMarker.SettingChanged += OnAutoOrCenterEnable;
            ResetZoomOnCenter.SettingChanged += OnAutoOrCenterEnable;
            RetainMapPosition.SettingChanged += OnPositionRetainEnable;
            
            #region Mini Map

            ConfigEntries.Add(MiniMapEnabled = config.Bind(
                MiniMapTitle,
                "Mini-map enabled",
                true,
                new ConfigDescription(
                    "Enable the mini-map",
                    null,
                    new ConfigurationManagerAttributes { })));
            
            ConfigEntries.Add(MiniMapPosition = config.Bind(
                MiniMapTitle,
                "Mini-map position",
                EMiniMapPosition.TopRight,
                new ConfigDescription(
                    "What corner is the mini-map displayed in",
                    null,
                    new ConfigurationManagerAttributes { })));
            
            ConfigEntries.Add(MiniMapSizeX = config.Bind(
                MiniMapTitle,
                "Mini-map size horizontal",
                275.0f,
                new ConfigDescription(
                    "Horizontal size of the mini-map",
                    new AcceptableValueRange<float>(0f, 850f),
                    new ConfigurationManagerAttributes { })));
            
            ConfigEntries.Add(MiniMapSizeY = config.Bind(
                MiniMapTitle,
                "Mini-map size vertical",
                275.0f,
                new ConfigDescription(
                    "Vertical size of the mini-map",
                    new AcceptableValueRange<float>(0f, 850f),
                    new ConfigurationManagerAttributes { })));
            
            ConfigEntries.Add(MiniMapScreenOffsetX = config.Bind(
                MiniMapTitle,
                "Mini-map offset horizontal",
                0.0f,
                new ConfigDescription(
                    "Horizontal Offset from the edge (These values update according to screen resolution, REQUIRES RESTART IF YOU CHANGED YOUR RESOLUTION)",
                    new AcceptableValueRange<float>(-Screen.width / 4f, Screen.width),
                    new ConfigurationManagerAttributes { })));
            
            ConfigEntries.Add(MiniMapScreenOffsetY = config.Bind(
                MiniMapTitle,
                "Mini-map offset vertical",
                0.0f,
                new ConfigDescription(
                    "Vertical offset from the edge (These values update according to screen resolution, REQUIRES RESTART IF YOU CHANGED YOUR RESOLUTION)",
                    new AcceptableValueRange<float>(-Screen.height / 4f, Screen.height),
                    new ConfigurationManagerAttributes { })));
            
            ConfigEntries.Add(MiniMapShowOrHide = config.Bind(
                MiniMapTitle,
                "Show or Hide the mini-map",
                new KeyboardShortcut(KeyCode.End),
                new ConfigDescription(
                    "Show or hide the mini-map",
                    null,
                    new ConfigurationManagerAttributes { })));
            
            ConfigEntries.Add(ZoomMiniMap = config.Bind(
                MiniMapTitle,
                "Mini map zoom",
                5.0f,
                new ConfigDescription(
                    "What zoom level should be used for the mini map. (0 is fully zoomed out, and 15 is fully zoomed in)",
                    new AcceptableValueRange<float>(0f, 15f),
                    new ConfigurationManagerAttributes { })));

            ConfigEntries.Add(ZoomInMiniMapHotkey = config.Bind(
                MiniMapTitle,
                "Zoom in key bind",
                new KeyboardShortcut(KeyCode.Keypad8),
                new ConfigDescription(
                    "Zoom in on mini map key bind",
                    null,
                    new ConfigurationManagerAttributes { })));
            
            ConfigEntries.Add(ZoomOutMiniMapHotkey = config.Bind(
                MiniMapTitle,
                "Zoom out key bind",
                new KeyboardShortcut(KeyCode.Keypad5),
                new ConfigDescription(
                    "Zoom out on mini map key bind",
                    null,
                    new ConfigurationManagerAttributes { })));
            
            #endregion

            #region MarkerColors

            ConfigEntries.Add(PlayerColor = config.Bind(
                MarkerColors,
                "Your player marker color",
                new Color(0, 1, 0),
                new ConfigDescription(
                    "Color of the marker",
                    null,
                    new ConfigurationManagerAttributes { })));
            
            ConfigEntries.Add(PmcBearColor = config.Bind(
                MarkerColors,
                "Bear marker color",
                new Color(1, 0, 0),
                new ConfigDescription(
                    "Color of the marker",
                    null,
                    new ConfigurationManagerAttributes { })));
            
            ConfigEntries.Add(PmcUsecColor = config.Bind(
                MarkerColors,
                "Usec marker color",
                new Color(1, 1, 0),
                new ConfigDescription(
                    "Color of the marker",
                    null,
                    new ConfigurationManagerAttributes { })));
            
            ConfigEntries.Add(ScavColor = config.Bind(
                MarkerColors,
                "Scav marker color",
                new Color(1, 0.45f, 0.007f),
                new ConfigDescription(
                    "Color of the marker",
                    null,
                    new ConfigurationManagerAttributes { })));
            
            ConfigEntries.Add(BossColor = config.Bind(
                MarkerColors,
                "Boss marker color",
                new Color(1f, 0.45f, 0.007f),
                new ConfigDescription(
                    "Color of the marker",
                    null,
                    new ConfigurationManagerAttributes { })));
            
            ConfigEntries.Add(AirdropColor = config.Bind(
                MarkerColors,
                "Airdrop marker color",
                new Color(1f, 0.30f, 0.007f),
                new ConfigDescription(
                    "Color of the marker",
                    null,
                    new ConfigurationManagerAttributes { })));
            
            ConfigEntries.Add(BackpackColor = config.Bind(
                MarkerColors,
                "Backpack marker color",
                new Color(0f, 1f, 0.0f),
                new ConfigDescription(
                    "Color of the marker",
                    null,
                    new ConfigurationManagerAttributes { })));
            
            ConfigEntries.Add(LootItemColor = config.Bind(
                MarkerColors,
                "Loot marker color",
                new Color(0.98f, 0.81f, 0.007f),
                new ConfigDescription(
                    "Color of the marker",
                    null,
                    new ConfigurationManagerAttributes { })));
            
            ConfigEntries.Add(KilledCorpseColor = config.Bind(
                MarkerColors,
                "Killed corpse marker color",
                new Color(1f, 0.0f, 0.0f),
                new ConfigDescription(
                    "Color of the marker",
                    null,
                    new ConfigurationManagerAttributes { })));

            ConfigEntries.Add(KilledBossColor = config.Bind(
                MarkerColors,
                "Killed boss corpse marker color",
                new Color(1f, 0.0f, 1.0f),
                new ConfigDescription(
                    "Color of the marker",
                    null,
                    new ConfigurationManagerAttributes { })));
            
            ConfigEntries.Add(KilledOtherColor = config.Bind(
                MarkerColors,
                "Killed by other corpse marker color",
                new Color(1f, 1f, 1f),
                new ConfigDescription(
                    "Color of the marker",
                    null,
                    new ConfigurationManagerAttributes { })));
            
            ConfigEntries.Add(BtrColor = config.Bind(
                MarkerColors,
                "BTR marker color",
                new Color(0.21f, 0.39f, 0.16f),
                new ConfigDescription(
                    "Color of the marker",
                    null,
                    new ConfigurationManagerAttributes { })));
            
            ConfigEntries.Add(ExtractDefaultColor = config.Bind(
                MarkerColors,
                "Extract default marker color",
                new Color(1f, 0.92f, 0.01f),
                new ConfigDescription(
                    "Color of the marker",
                    null,
                    new ConfigurationManagerAttributes { })));
            
            ConfigEntries.Add(ExtractOpenColor = config.Bind(
                MarkerColors,
                "Extract open marker color",
                new Color(0f, 1f, 0f),
                new ConfigDescription(
                    "Color of the marker",
                    null,
                    new ConfigurationManagerAttributes { })));
            
            ConfigEntries.Add(ExtractClosedColor = config.Bind(
                MarkerColors,
                "Extract closed marker color",
                new Color(1f, 0f, 0f),
                new ConfigDescription(
                    "Color of the marker",
                    null,
                    new ConfigurationManagerAttributes { })));
            
            ConfigEntries.Add(ExtractHasRequirementsColor = config.Bind(
                MarkerColors,
                "Extract has requirements marker color",
                new Color(1f, 0.92f, 0.01f),
                new ConfigDescription(
                    "Color of the marker",
                    null,
                    new ConfigurationManagerAttributes { })));
            
            ConfigEntries.Add(TransPointColor = config.Bind(
                MarkerColors,
                "Transit point marker color",
                new Color(1f, 0.62f, 0.20f),
                new ConfigDescription(
                    "Color of the marker",
                    null,
                    new ConfigurationManagerAttributes { })));

            ConfigEntries.Add(SecretPointColor = config.Bind(
                MarkerColors,
                "Secret exfil point marker color",
                new Color(0.1f, 0.6f, 0.6f),
                new ConfigDescription(
                    "Color of the marker",
                    null,
                    new ConfigurationManagerAttributes { })));

            ConfigEntries.Add(HiddenStashColor = config.Bind(
                MarkerColors,
                "Extract has requirements marker color",
                new Color(0.1f, 0.0f, 0.51f),
                new ConfigDescription(
                    "Color of the marker",
                    null,
                    new ConfigurationManagerAttributes { })));
            
            #endregion
            
            #region ExternalModSupport

            ConfigEntries.Add(ShowHeliCrashMarker = config.Bind(
                ExternModSupport,
                "Show Heli Crash Marker",
                true,
                new ConfigDescription(
                    "If the heli crash site should be marked in raid",
                    null,
                    new ConfigurationManagerAttributes { 
                        Browsable = ModDetection.HeliCrashLoaded
                    })));
            
            #endregion
            
            RecalcOrder();
        }
        
        private static void RecalcOrder()
        {
            // Set the Order field for all settings, to avoid unnecessary changes when adding new settings
            var settingOrder = ConfigEntries.Count;
            foreach (var entry in ConfigEntries)
            {
                var attributes = entry.Description.Tags[0] as ConfigurationManagerAttributes;
                if (attributes != null)
                {
                    attributes.Order = settingOrder;
                }

                settingOrder--;
            }
        }
        
        private static void OnAutoOrCenterEnable(object sender, EventArgs e)
        {
            if (AutoCenterOnPlayerMarker.Value || ResetZoomOnCenter.Value)
            {
                RetainMapPosition.Value = false;
            }
        }
        
        private static void OnPositionRetainEnable(object sender, EventArgs e)
        {
            if (RetainMapPosition.Value)
            {
                AutoCenterOnPlayerMarker.Value = false;
                ResetZoomOnCenter.Value = false;
            }
        }
    }
}

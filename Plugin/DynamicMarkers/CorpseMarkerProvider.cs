using System.Collections.Generic;
using System.Linq;
using Comfort.Common;
using DynamicMaps.Config;
using DynamicMaps.Data;
using DynamicMaps.Patches;
using DynamicMaps.UI.Components;
using DynamicMaps.Utils;
using EFT;
using UnityEngine;

namespace DynamicMaps.DynamicMarkers
{
    public class CorpseMarkerProvider : IDynamicMarkerProvider
    {
        private const string SkullImagePath = "Markers/skull.png";

        // TODO: move to config
        private const string FriendlyCorpseCategory = "Friendly Corpse";
        private const string FriendlyCorpseImagePath = SkullImagePath;
        private static readonly Color FriendlyCorpseColor = Color.Lerp(Color.blue, Color.white, 0.5f);

        private const string KilledCorpseCategory = "Killed Corpse";
        private const string KilledCorpseImagePath = SkullImagePath;

        private const string KilledBossCorpseCategory = "Killed Boss Corpse";
        private const string KilledBossCorpseImagePath = SkullImagePath;

        private const string BossCorpseCategory = "Boss Corpse";
        private const string BossCorpseImagePath = SkullImagePath;

        private const string FriendlyKilledCorpseCategory = "Friendly Killed Corpse";
        private const string FriendlyKilledCorpseImagePath = SkullImagePath;
        private static readonly Color FriendlyKilledCorpseColor = Color.Lerp(Color.Lerp(Color.blue, Color.white, 0.5f), Color.red, 0.5f);

        private const string FriendlyKilledBossCorpseCategory = "Friendly Killed Boss Corpse";
        private const string FriendlyKilledBossCorpseImagePath = SkullImagePath;

        private const string OtherCorpseCategory = "Other Corpse";
        private const string OtherCorpseImagePath = SkullImagePath;
        //

        private bool _showFriendlyCorpses = true;
        public bool ShowFriendlyCorpses
        {
            get
            {
                return _showFriendlyCorpses;
            }

            set
            {
                HandleSetBoolOption(ref _showFriendlyCorpses, value);
            }
        }

        private bool _showKilledCorpses = true;
        public bool ShowKilledCorpses
        {
            get
            {
                return _showKilledCorpses;
            }

            set
            {
                HandleSetBoolOption(ref _showKilledCorpses, value);
            }
        }

        private bool _showFriendlyKilledCorpses = true;
        public bool ShowFriendlyKilledCorpses
        {
            get
            {
                return _showFriendlyKilledCorpses;
            }

            set
            {
                HandleSetBoolOption(ref _showFriendlyKilledCorpses, value);
            }
        }

        private bool _showBossCorpses = false;
        public bool ShowBossCorpses
        {
            get
            {
                return _showBossCorpses;
            }

            set
            {
                HandleSetBoolOption(ref _showBossCorpses, value);
            }
        }

        private bool _showOtherCorpses = false;
        public bool ShowOtherCorpses
        {
            get
            {
                return _showOtherCorpses;
            }

            set
            {
                HandleSetBoolOption(ref _showOtherCorpses, value);
            }
        }

        private MapView _lastMapView;
        private readonly Dictionary<Player, MapMarker> _corpseMarkers = [];

        public void OnShowInRaid(MapView map)
        {
            _lastMapView = map;
            
            TryAddMarkers();

            GameWorldUnregisterPlayerPatch.OnUnregisterPlayer += OnUnregisterPlayer;
        }

        public void OnHideInRaid(MapView map)
        {
            GameWorldUnregisterPlayerPatch.OnUnregisterPlayer -= OnUnregisterPlayer;
        }

        public void OnRaidEnd(MapView map)
        {
            GameWorldUnregisterPlayerPatch.OnUnregisterPlayer -= OnUnregisterPlayer;

            _lastMapView = map;
            TryRemoveMarkers();
        }

        public void OnMapChanged(MapView map, MapDef mapDef)
        {
            _lastMapView = map;

            foreach (var corpse in _corpseMarkers.Keys.ToList())
            {
                TryRemoveMarker(corpse);
                TryAddMarker(corpse);
            }
        }

        public void OnDisable(MapView map)
        {
            GameWorldUnregisterPlayerPatch.OnUnregisterPlayer -= OnUnregisterPlayer;
            TryRemoveMarkers();
        }

        private void OnUnregisterPlayer(IPlayer iPlayer)
        {
            if (iPlayer is not Player)
            {
                return;
            }

            var player = iPlayer as Player;
            if (player.HasCorpse())
            {
                TryAddMarker(player);
            }
        }

        private void TryRemoveMarkers()
        {
            foreach (var corpse in _corpseMarkers.Keys.ToList())
            {
                TryRemoveMarker(corpse);
            }

            _corpseMarkers.Clear();
        }

        private void TryAddMarkers()
        {
            if (!GameUtils.IsInRaid())
            {
                return;
            }

            // add all players that have spawned already in raid
            var gameWorld = Singleton<GameWorld>.Instance;
            foreach (var player in gameWorld.AllPlayersEverExisted)
            {
                if (!player.HasCorpse() || _corpseMarkers.ContainsKey(player))
                {
                    continue;
                }

                TryAddMarker(player);
            }
        }

        public void RefreshMarkers()
        {
            if (!GameUtils.IsInRaid()) return;

            foreach (var marker in _corpseMarkers.ToArray())
            {
                TryRemoveMarker(marker.Key);
            }

            var corpses = Singleton<GameWorld>.Instance.AllPlayersEverExisted
                .Where(p => p.HasCorpse());
            
            foreach (var corpse in corpses)
            {
                TryAddMarker(corpse);
            }
        }
        
        private void TryAddMarker(Player player)
        {
            if (_lastMapView is null || _corpseMarkers.ContainsKey(player))
            {
                return;
            }

            var intelLevel = GameUtils.GetIntelLevel();
            
            if (Settings.ShowCorpseIntelLevel.Value > intelLevel) return;
            
            // set category and color
            var category = OtherCorpseCategory;
            var imagePath = OtherCorpseImagePath;
            var color = Settings.KilledOtherColor.Value;
            
            if (player.IsGroupedWithMainPlayer())
            {
                category = FriendlyCorpseCategory;
                imagePath = FriendlyCorpseImagePath;
                color = FriendlyCorpseColor;
            }
            else if (player.IsTrackedBoss() && player.DidMainPlayerKill())
            {
                category = KilledBossCorpseCategory;
                imagePath = KilledBossCorpseImagePath;
                color = Settings.KilledBossColor.Value;
            }
            else if (player.DidMainPlayerKill())
            {
                category = KilledCorpseCategory;
                imagePath = KilledCorpseImagePath;
                color = Settings.KilledCorpseColor.Value;
            }
            else if (player.IsTrackedBoss() && player.DidTeammateKill())
            {
                category = FriendlyKilledBossCorpseCategory;
                imagePath = FriendlyKilledBossCorpseImagePath;
                color = Settings.KilledBossColor.Value;
            }
            else if (player.DidTeammateKill())
            {
                category = FriendlyKilledCorpseCategory;
                imagePath = FriendlyKilledCorpseImagePath;
                color = FriendlyKilledCorpseColor;
            }
            else if (player.IsTrackedBoss())
            {
                category = BossCorpseCategory;
                imagePath = BossCorpseImagePath;
                color = Settings.KilledBossColor.Value;
            }

            if (!ShouldShowCategory(category))
            {
                return;
            }

            var markerDef = new MapMarkerDef
            {
                Category = category,
                ImagePath = imagePath,
                Text = player.Profile.GetCorrectedNickname(),
                Color = color,
                Position = MathUtils.ConvertToMapPosition(((IPlayer)player).Position)
            };

            // try adding marker
            var marker = _lastMapView.AddMapMarker(markerDef);
            _corpseMarkers[player] = marker;
        }

        private void RemoveDisabledMarkers()
        {
            foreach (var corpse in _corpseMarkers.Keys.ToList())
            {
                var marker = _corpseMarkers[corpse];
                if (!ShouldShowCategory(marker.Category))
                {
                    TryRemoveMarker(corpse);
                }
            }
        }

        private void TryRemoveMarker(Player player)
        {
            if (!_corpseMarkers.ContainsKey(player))
            {
                return;
            }

            _corpseMarkers[player].ContainingMapView.RemoveMapMarker(_corpseMarkers[player]);
            _corpseMarkers.Remove(player);
        }

        private bool ShouldShowCategory(string category)
        {
            switch (category)
            {
                case FriendlyCorpseCategory:
                    return _showFriendlyCorpses;
                case KilledCorpseCategory:
                case KilledBossCorpseCategory:
                    return _showKilledCorpses;
                case FriendlyKilledCorpseCategory:
                case FriendlyKilledBossCorpseCategory:
                    return _showFriendlyKilledCorpses;
                case BossCorpseCategory:
                    return _showBossCorpses;
                case OtherCorpseCategory:
                    return _showOtherCorpses;
                default:
                    return false;
            }
        }

        private void HandleSetBoolOption(ref bool boolOption, bool value)
        {
            if (value == boolOption)
            {
                return;
            }

            boolOption = value;

            if (boolOption)
            {
                TryAddMarkers();
            }
            else
            {
                RemoveDisabledMarkers();
            }
        }

        public void OnShowOutOfRaid(MapView map)
        {
            // do nothing
        }

        public void OnHideOutOfRaid(MapView map)
        {
            // do nothing
        }
    }
}

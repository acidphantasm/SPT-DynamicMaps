using System;
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
using CompoundItem = EFT.InventoryLogic.CompoundItem;
using IItemOwner = EFT.InventoryLogic.IItemOwner;

namespace DynamicMaps.DynamicMarkers
{
    public class CorpseMarkerProvider : IDynamicMarkerProvider
    {
        private const string _skullImagePath = "Markers/skull.png";

        // TODO: move to config
        private const string _friendlyCorpseCategory = "Friendly Corpse";
        private const string _friendlyCorpseImagePath = _skullImagePath;
        private static Color _friendlyCorpseColor = Color.Lerp(Color.blue, Color.white, 0.5f);

        private const string _killedCorpseCategory = "Killed Corpse";
        private const string _killedCorpseImagePath = _skullImagePath;

        private const string _killedBossCorpseCategory = "Killed Boss Corpse";
        private const string _killedBossCorpseImagePath = _skullImagePath;

        private const string _bossCorpseCategory = "Boss Corpse";
        private const string _bossCorpseImagePath = _skullImagePath;

        private const string _friendlyKilledCorpseCategory = "Friendly Killed Corpse";
        private const string _friendlyKilledCorpseImagePath = _skullImagePath;
        private static Color _friendlyKilledCorpseColor = Color.Lerp(Color.Lerp(Color.blue, Color.white, 0.5f), Color.red, 0.5f);

        private const string _friendlyKilledBossCorpseCategory = "Friendly Killed Boss Corpse";
        private const string _friendlyKilledBossCorpseImagePath = _skullImagePath;

        private const string _otherCorpseCategory = "Other Corpse";
        private const string _otherCorpseImagePath = _skullImagePath;
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
        private Dictionary<Player, MapMarker> _corpseMarkers = [];
        private static readonly HashSet<string> _searchedCorpseProfileIds = [];

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
            _searchedCorpseProfileIds.Clear();
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
            if (player.HasCorpse() && !IsMarkedSearched(player))
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
                if (!player.HasCorpse() || _corpseMarkers.ContainsKey(player) || IsMarkedSearched(player))
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
                .Where(p => p.HasCorpse() && !IsMarkedSearched(p));

            foreach (var corpse in corpses)
            {
                TryAddMarker(corpse);
            }
        }

        public bool TryGetCorpseLootMarkerState(CompoundItem lootItem, out bool isMarkedSearched, out string reason)
        {
            isMarkedSearched = false;

            if (!TryGetCorpsePlayerByLootItem(lootItem, out var player, out reason))
            {
                return false;
            }

            isMarkedSearched = IsMarkedSearched(player);
            reason = $"matched {DescribeCorpsePlayer(player)}; isMarkedSearched={isMarkedSearched}";
            return true;
        }

        public bool TryMarkCorpseSearchedByLootItem(CompoundItem lootItem, out string reason)
        {
            if (!TryGetCorpsePlayerByLootItem(lootItem, out var corpsePlayer, out reason))
            {
                return false;
            }

            MarkCorpseSearched(corpsePlayer);
            reason = $"marked {DescribeCorpsePlayer(corpsePlayer)}";
            return true;
        }


        public bool TryToggleCorpseSearchedByLootItem(CompoundItem lootItem, out bool isMarkedSearched, out string reason)
        {
            isMarkedSearched = false;
            if (!TryGetCorpsePlayerByLootItem(lootItem, out var corpsePlayer, out reason))
            {
                return false;
            }

            if (IsMarkedSearched(corpsePlayer))
            {
                _searchedCorpseProfileIds.Remove(corpsePlayer.ProfileId);
                TryAddMarker(corpsePlayer);
                reason = $"shown {DescribeCorpsePlayer(corpsePlayer)}";
                return true;
            }

            MarkCorpseSearched(corpsePlayer);
            isMarkedSearched = true;
            reason = $"marked {DescribeCorpsePlayer(corpsePlayer)}";
            return true;
        }
        public bool IsCorpseLootRootItem(string rootItemId)
        {
            return TryGetCorpsePlayerByRootItemId(rootItemId, out _, out _);
        }

        public bool IsCorpseMarkedSearchedByRootItemId(string rootItemId)
        {
            return TryGetCorpsePlayerByRootItemId(rootItemId, out var player, out _) && IsMarkedSearched(player);
        }

        public bool TryMarkCorpseSearchedByRootItemId(string rootItemId)
        {
            return TryGetCorpsePlayerByRootItemId(rootItemId, out var corpsePlayer, out _)
                && TryMarkCorpseSearched(corpsePlayer);
        }

        private void TryAddMarker(Player player)
        {
            if (_lastMapView is null || _corpseMarkers.ContainsKey(player) || IsMarkedSearched(player))
            {
                return;
            }

            var intelLevel = GameUtils.GetIntelLevel();

            if (Settings.ShowCorpseIntelLevel.Value > intelLevel) return;

            // set category and color
            var category = _otherCorpseCategory;
            var imagePath = _otherCorpseImagePath;
            var color = Settings.KilledOtherColor.Value;

            if (player.IsGroupedWithMainPlayer())
            {
                category = _friendlyCorpseCategory;
                imagePath = _friendlyCorpseImagePath;
                color = _friendlyCorpseColor;
            }
            else if (player.IsTrackedBoss() && player.DidMainPlayerKill())
            {
                category = _killedBossCorpseCategory;
                imagePath = _killedBossCorpseImagePath;
                color = Settings.KilledBossColor.Value;
            }
            else if (player.DidMainPlayerKill())
            {
                category = _killedCorpseCategory;
                imagePath = _killedCorpseImagePath;
                color = Settings.KilledCorpseColor.Value;
            }
            else if (player.IsTrackedBoss() && player.DidTeammateKill())
            {
                category = _friendlyKilledBossCorpseCategory;
                imagePath = _friendlyKilledBossCorpseImagePath;
                color = Settings.KilledBossColor.Value;
            }
            else if (player.DidTeammateKill())
            {
                category = _friendlyKilledCorpseCategory;
                imagePath = _friendlyKilledCorpseImagePath;
                color = _friendlyKilledCorpseColor;
            }
            else if (player.IsTrackedBoss())
            {
                category = _bossCorpseCategory;
                imagePath = _bossCorpseImagePath;
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
                Position = MathUtils.ConvertToMapPosition(GetCorpsePosition(player))
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

        private void MarkCorpseSearched(Player player)
        {
            TryMarkCorpseSearched(player);
        }

        private bool TryMarkCorpseSearched(Player player)
        {
            if (player is null || string.IsNullOrEmpty(player.ProfileId))
            {
                return false;
            }

            _searchedCorpseProfileIds.Add(player.ProfileId);
            TryRemoveMarker(player);
            Plugin.Log.LogInfo($"Marked corpse {player.Profile.GetCorrectedNickname()} as searched");
            return true;
        }

        private static bool IsMarkedSearched(Player player)
        {
            return player is not null
                && !string.IsNullOrEmpty(player.ProfileId)
                && _searchedCorpseProfileIds.Contains(player.ProfileId);
        }

        private static bool TryGetCorpsePlayerByRootItemId(string rootItemId, out Player corpsePlayer, out string reason)
        {
            corpsePlayer = null;
            reason = "not scanned";

            if (string.IsNullOrEmpty(rootItemId) || !GameUtils.IsInRaid())
            {
                reason = $"rootItemId empty or not in raid; rootItemId={rootItemId ?? "null"}; isInRaid={GameUtils.IsInRaid()}";
                return false;
            }

            var gameWorld = Singleton<GameWorld>.Instance;
            if (gameWorld is null)
            {
                reason = "GameWorld is null";
                return false;
            }

            var scannedCorpses = 0;
            var samples = new List<string>();

            foreach (var player in gameWorld.AllPlayersEverExisted)
            {
                if (player is null || !player.HasCorpse())
                {
                    continue;
                }

                var corpse = player.GetCorpse();
                if (corpse is null)
                {
                    continue;
                }

                scannedCorpses++;
                AddCorpseSample(samples, player, corpse);

                if (string.Equals(corpse.ItemId, rootItemId, StringComparison.Ordinal)
                    || string.Equals(corpse.Item?.Id, rootItemId, StringComparison.Ordinal))
                {
                    corpsePlayer = player;
                    reason = $"matched by rootItemId={rootItemId}; {DescribeCorpsePlayer(player)}";
                    return true;
                }
            }

            reason = $"no matching corpse by rootItemId={rootItemId}; scannedCorpses={scannedCorpses}; sample={FormatSamples(samples)}";
            return false;
        }

        private static bool TryGetCorpsePlayerByLootItem(CompoundItem lootItem, out Player corpsePlayer, out string reason)
        {
            corpsePlayer = null;

            if (lootItem is null)
            {
                reason = "lootItem is null";
                return false;
            }

            if (!GameUtils.IsInRaid())
            {
                reason = $"not in raid; lootItem={DescribeLootItem(lootItem)}";
                return false;
            }

            var gameWorld = Singleton<GameWorld>.Instance;
            if (gameWorld is null)
            {
                reason = $"GameWorld is null; lootItem={DescribeLootItem(lootItem)}";
                return false;
            }

            var lootOwner = lootItem.Owner;
            var scannedCorpses = 0;
            var samples = new List<string>();

            foreach (var player in gameWorld.AllPlayersEverExisted)
            {
                if (player is null || !player.HasCorpse())
                {
                    continue;
                }

                var corpse = player.GetCorpse();
                if (corpse is null)
                {
                    continue;
                }

                scannedCorpses++;
                AddCorpseSample(samples, player, corpse);

                var matchedByReference = ReferenceEquals(corpse.Item, lootItem);
                var matchedByItemId = string.Equals(corpse.ItemId, lootItem.Id, StringComparison.Ordinal)
                    || string.Equals(corpse.Item?.Id, lootItem.Id, StringComparison.Ordinal);
                var matchedByOwner = lootOwner is not null
                    && corpse.ItemOwner is not null
                    && (ReferenceEquals(corpse.ItemOwner, lootOwner)
                        || string.Equals(corpse.ItemOwner.RootItem?.Id, lootOwner.RootItem?.Id, StringComparison.Ordinal));

                if (matchedByReference || matchedByItemId || matchedByOwner)
                {
                    corpsePlayer = player;
                    reason = $"matchedByReference={matchedByReference}; matchedByItemId={matchedByItemId}; matchedByOwner={matchedByOwner}; lootItem={DescribeLootItem(lootItem)}; lootOwner={DescribeOwner(lootOwner)}; {DescribeCorpsePlayer(player)}";
                    return true;
                }
            }

            reason = $"no matching corpse for lootItem={DescribeLootItem(lootItem)}; lootOwner={DescribeOwner(lootOwner)}; scannedCorpses={scannedCorpses}; sample={FormatSamples(samples)}";
            return false;
        }

        private static void AddCorpseSample(List<string> samples, Player player, EFT.Interactive.Corpse corpse)
        {
            if (samples.Count >= 5)
            {
                return;
            }

            samples.Add($"{player.ProfileId ?? "noProfile"}:{player.Profile?.GetCorrectedNickname() ?? "noName"}:corpseItemId={corpse.ItemId ?? "null"}:itemId={corpse.Item?.Id ?? "null"}:ownerRoot={corpse.ItemOwner?.RootItem?.Id ?? "null"}:marked={IsMarkedSearched(player)}");
        }

        private static string FormatSamples(List<string> samples)
        {
            return samples.Count == 0 ? "none" : string.Join(" | ", samples);
        }

        private static string DescribeCorpsePlayer(Player player)
        {
            return player is null
                ? "player=null"
                : $"playerProfile={player.ProfileId ?? "null"}; nickname={player.Profile?.GetCorrectedNickname() ?? "null"}";
        }

        private static string DescribeLootItem(CompoundItem lootItem)
        {
            return lootItem is null
                ? "null"
                : $"{lootItem.GetType().Name}:id={lootItem.Id ?? "null"}:template={lootItem.TemplateId}:ownerRoot={DescribeRootItemId(lootItem.Owner)}";
        }

        private static string DescribeOwner(IItemOwner owner)
        {
            return owner is null
                ? "null"
                : $"{owner.GetType().Name}:root={DescribeRootItemId(owner)}:container={owner.ContainerName ?? "null"}:type={owner.OwnerType}";
        }

        private static string DescribeRootItemId(IItemOwner owner)
        {
            return owner?.RootItem is null ? "null" : owner.RootItem.Id.ToString();
        }

        private static Vector3 GetCorpsePosition(Player player)
        {
            var corpse = player.GetCorpse();
            return corpse?.TrackableTransform?.position ?? ((IPlayer)player).Position;
        }

        private bool ShouldShowCategory(string category)
        {
            switch (category)
            {
                case _friendlyCorpseCategory:
                    return _showFriendlyCorpses;
                case _killedCorpseCategory:
                case _killedBossCorpseCategory:
                    return _showKilledCorpses;
                case _friendlyKilledCorpseCategory:
                case _friendlyKilledBossCorpseCategory:
                    return _showFriendlyKilledCorpses;
                case _bossCorpseCategory:
                    return _showBossCorpses;
                case _otherCorpseCategory:
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

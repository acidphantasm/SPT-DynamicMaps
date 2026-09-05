using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Comfort.Common;
using DynamicMaps.Config;
using DynamicMaps.Data;
using DynamicMaps.UI.Components;
using DynamicMaps.Utils;
using EFT;
using EFT.Interactive;
using EFT.UI;
using EFT.UI.DragAndDrop;
using UnityEngine;

namespace DynamicMaps.DynamicMarkers
{
    public class LootMarkerProvider : IDynamicMarkerProvider
    {
        // How often the map is checked for picked-up / new loot while in raid
        private const float RefreshIntervalSeconds = 30f;

        private MapView _lastMapView;
        private Dictionary<LootItem, MapMarker> _lootMarkers = [];
        private Coroutine _refreshCoroutine;

        public void OnShowInRaid(MapView map)
        {
            _lastMapView = map;

            // Sync markers with the world state immediately on open,
            // then keep syncing periodically for the rest of the raid
            RefreshMarkers();
            StartRefreshLoop();
        }

        public void OnHideInRaid(MapView map)
        {
            // Do nothing, the refresh loop keeps running in the background
            // so that the markers are up to date when the map is opened again
        }

        public void OnRaidEnd(MapView map)
        {
            StopRefreshLoop();
            TryRemoveMarkers();
        }

        public void OnMapChanged(MapView map, MapDef mapDef)
        {
            _lastMapView = map;

            // transition markers from last map to this one
            foreach (var item in _lootMarkers.Keys.ToList())
            {
                TryRemoveMarker(item);
                TryAddMarker(item);
            }
        }

        public void OnDisable(MapView map)
        {
            OnRaidEnd(map);
        }

        public void RefreshMarkers()
        {
            if (!GameUtils.IsInRaid()) return;
            if (_lastMapView is null) return;

            var gameWorld = Singleton<GameWorld>.Instance;
            if (gameWorld is null) return;

            var lootList = gameWorld.LootList;
            if (lootList is null) return;

            var wishListItems = new HashSet<MongoID>(GameUtils.GetWishListItems());

            // loot items that currently exist in the world
            var worldLootItems = new HashSet<LootItem>();
            foreach (var item in lootList)
            {
                if (item is LootItem lootItem)
                {
                    worldLootItems.Add(lootItem);
                }
            }

            // remove markers of items that no longer exist in the world
            // (picked up, destroyed, etc.)
            foreach (var item in _lootMarkers.Keys.ToList())
            {
                if (!worldLootItems.Contains(item))
                {
                    TryRemoveMarker(item);
                }
            }

            // add markers for new wishlist items that have appeared in the world
            foreach (var lootItem in worldLootItems)
            {
                if (wishListItems.Contains(new MongoID(lootItem.TemplateId)))
                {
                    TryAddMarker(lootItem);
                }
            }
        }

        private void StartRefreshLoop()
        {
            if (_refreshCoroutine is not null) return;

            var commonUi = Singleton<CommonUI>.Instance;
            if (commonUi is null) return;

            _refreshCoroutine = commonUi.StartCoroutine(RefreshLoop());
        }

        private void StopRefreshLoop()
        {
            if (_refreshCoroutine is null) return;

            var commonUi = Singleton<CommonUI>.Instance;
            if (commonUi is not null)
            {
                commonUi.StopCoroutine(_refreshCoroutine);
            }

            _refreshCoroutine = null;
        }

        private IEnumerator RefreshLoop()
        {
            var wait = new WaitForSeconds(RefreshIntervalSeconds);

            while (true)
            {
                yield return wait;

                // raid has ended, this is a safety net in case OnRaidEnd was not delivered
                if (!GameUtils.IsInRaid())
                {
                    _refreshCoroutine = null;
                    yield break;
                }

                try
                {
                    RefreshMarkers();
                }
                catch (Exception e)
                {
                    Plugin.Log.LogError("LootMarkerProvider refresh loop failed");
                    Plugin.Log.LogError($"  Exception given was: {e.Message}");
                    Plugin.Log.LogError($"  {e.StackTrace}");
                }
            }
        }

        private void TryAddMarker(LootItem item)
        {
            if (item == null || item.Item == null)
                return;
            
            if (_lootMarkers.ContainsKey(item)) return;
            if (Settings.ShowWishListItemsIntelLevel.Value > GameUtils.GetIntelLevel()) return;
            
            var staticIcons = EFTHardSettings.Instance.StaticIcons;
            var itemType = ItemViewFactory.GetItemType(item.Item.GetType());
            var itemSprite = staticIcons.ItemTypeSprites.GetValueOrDefault(itemType);

            // Fallback для предметов, для которых EFT не предоставляет ItemType sprite
            if (itemSprite == null)
            {
                itemSprite = TextureUtils.GetOrLoadCachedSprite(
                    "Markers/star.png"
                );

                if (itemSprite == null)
                {
                    Plugin.Log.LogError(
                        $"[DynamicMaps] Missing loot sprite and star fallback. " +
                        $"Marker will not be created. " +
                        $"TemplateId={item.Item.TemplateId}, " +
                        $"ItemType={itemType}"
                    );

                    return;
                }

                Plugin.Log.LogWarning(
                    $"[DynamicMaps] Missing loot sprite. " +
                    $"Using star fallback. " +
                    $"TemplateId={item.Item.TemplateId}, " +
                    $"ItemType={itemType}"
                );
            }

            var transform = item.transform;
            if (transform == null)
                return;
            
            var markerDef = new MapMarkerDef
            {
                Category = "Loot",
                Color = Settings.LootItemColor.Value,
                Sprite = itemSprite,
                Position = MathUtils.ConvertToMapPosition(transform),
                Text = item.Item.TemplateId.LocalizedName()
            };
            
            var marker = _lastMapView.AddMapMarker(markerDef);
            _lootMarkers[item] = marker;
        }

        private void TryRemoveMarkers()
        {
            foreach (var item in _lootMarkers.Keys.ToList())
            {
                TryRemoveMarker(item);
            }
        }

        private void TryRemoveMarker(LootItem item)
        {
            if (!_lootMarkers.ContainsKey(item)) return;
            
            _lootMarkers[item].ContainingMapView.RemoveMapMarker(_lootMarkers[item]);
            _lootMarkers.Remove(item);
        }
        
        public void OnShowOutOfRaid(MapView map)
        {
            // Do nothing
        }

        public void OnHideOutOfRaid(MapView map)
        {
            // Do nothing
        }
    }
}

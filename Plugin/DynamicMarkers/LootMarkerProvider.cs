using System.Collections.Generic;
using System.Linq;
using Comfort.Common;
using DynamicMaps.Config;
using DynamicMaps.Data;
using DynamicMaps.Patches;
using DynamicMaps.UI.Components;
using DynamicMaps.Utils;
using EFT;
using EFT.Interactive;
using EFT.UI.DragAndDrop;

namespace DynamicMaps.DynamicMarkers
{
    public class LootMarkerProvider : IDynamicMarkerProvider
    {
        private MapView _lastMapView;
        private readonly Dictionary<LootItem, MapMarker> _lootMarkers = [];
        private WishlistManager _wishlistManager;
        private bool _lootEventsSubscribed;
        
        public void OnShowInRaid(MapView map)
        {
            _lastMapView = map;

            var wishlistManager = GameUtils.GetPlayerProfile()?.WishlistManager;
            if (!ReferenceEquals(_wishlistManager, wishlistManager))
            {
                UnsubscribeFromWishlist();
                
                _wishlistManager = wishlistManager;
                if (_wishlistManager != null)
                {
                    _wishlistManager.OnAddInWishlist += OnWishlistChanged;
                    _wishlistManager.OnRemoveFromWishlist += OnWishlistChanged;
                }
            }

            if (!_lootEventsSubscribed)
            {
                LootItemInitPatch.OnRegisterLoot += OnRegisterLoot;
                GameWorldDestroyLootPatch.OnDestroyLoot += OnDestroyLoot;
                _lootEventsSubscribed = true;
            }

            RefreshMarkers();
        }

        public void OnHideInRaid(MapView map)
        {
        }

        public void OnRaidEnd(MapView map)
        {
            UnsubscribeFromWishlist();
            if (_lootEventsSubscribed)
            {
                LootItemInitPatch.OnRegisterLoot -= OnRegisterLoot;
                GameWorldDestroyLootPatch.OnDestroyLoot -= OnDestroyLoot;
                _lootEventsSubscribed = false;
            }
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
            if (!GameUtils.IsInRaid() || _lastMapView == null) 
                return;

            var gameWorld = Singleton<GameWorld>.Instance;
            if (gameWorld == null) 
                return;

            var wishListItems = new HashSet<MongoID>(GameUtils.GetWishListItems());
            var availableLoot = gameWorld.LootList.OfType<LootItem>().ToHashSet();

            foreach (var item in _lootMarkers.Keys.ToList())
            {
                if (!availableLoot.Contains(item) || !wishListItems.Contains(new MongoID(item.TemplateId)))
                {
                    TryRemoveMarker(item);
                }
            }

            foreach (var item in availableLoot)
            {
                if (item != null && wishListItems.Contains(new MongoID(item.TemplateId)))
                {
                    TryAddMarker(item);
                }
            }
        }

        private void OnWishlistChanged(MongoID itemId, EWishlistGroup group)
        {
            RefreshMarkers();
        }

        private void OnRegisterLoot(LootItem item)
        {
            if (_lastMapView == null || item?.Item == null || _wishlistManager == null)
                return;

            if (_wishlistManager.GetWishlist().ContainsKey(new MongoID(item.TemplateId)))
            {
                TryAddMarker(item);
            }
        }

        private void OnDestroyLoot(LootItem item)
        {
            TryRemoveMarker(item);
        }

        private void UnsubscribeFromWishlist()
        {
            if (_wishlistManager == null) 
                return;

            _wishlistManager.OnAddInWishlist -= OnWishlistChanged;
            _wishlistManager.OnRemoveFromWishlist -= OnWishlistChanged;
            
            _wishlistManager = null;
        }
        
        private void TryAddMarker(LootItem item)
        {
            if (item == null || item.Item == null)
                return;
            
            if (_lootMarkers.ContainsKey(item)) 
                return;
            
            var intelLevel = GameUtils.GetIntelLevel();
            if (Settings.ShowWishListItemsIntelLevel.Value > intelLevel) 
                return;
            
            var staticIcons = EFTHardSettings.Instance.StaticIcons;
            var itemType = ItemViewFactory.GetItemType(item.Item.GetType());
            var itemSprite = staticIcons.ItemTypeSprites.GetValueOrDefault(itemType);

            if (itemSprite == null)
            {
                return;
            }

            var transform = item.TrackableTransform;
            if (transform == null)
                return;
            
            var marker = _lastMapView.AddTransformMarker(transform, item.Item.TemplateId.LocalizedName(),
                                                        "Loot", Settings.LootItemColor.Value, itemSprite);
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

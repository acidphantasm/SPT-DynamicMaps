using System.Collections.Generic;
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
    using MapMarker = UI.Components.MapMarker;

    public class LooseLootMarkerProvider : IDynamicMarkerProvider
    {
        private MapView _map;
        private readonly Dictionary<LootItem, MapMarker> _markers = [];
        private WishlistManager _wishlistManager;
        private bool _subscribed;

        public void OnShowInRaid(MapView map)
        {
            _map = map;
            var manager = GameUtils.GetPlayerProfile()?.WishlistManager;
            
            if (!ReferenceEquals(_wishlistManager, manager))
            {
                Unsubscribe();
                _wishlistManager = manager;
                
                if (manager != null)
                {
                    manager.OnAddInWishlist += OnWishlistChanged;
                    manager.OnRemoveFromWishlist += OnWishlistChanged;
                }
            }
            
            if (!_subscribed)
            {
                LootItemInitPatch.OnRegisterLoot += OnRegisterLoot;
                GameWorldDestroyLootPatch.OnDestroyLoot += OnDestroyLoot;
                _subscribed = true;
            }
            
            RefreshMarkers();
        }

        public void OnHideInRaid(MapView map)
        {
            
        }

        public void OnRaidEnd(MapView map)
        {
            Unsubscribe();
            
            if (_subscribed)
            {
                LootItemInitPatch.OnRegisterLoot -= OnRegisterLoot;
                GameWorldDestroyLootPatch.OnDestroyLoot -= OnDestroyLoot;
                _subscribed = false;
            }
            
            foreach (var marker in _markers.Values)
            {
                marker.ContainingMapView.RemoveMapMarker(marker);
            }
            
            _markers.Clear();
            _map = null;
        }

        public void OnMapChanged(MapView map, MapDef mapDef)
        {
            _map = map;
            _markers.Clear();
            RefreshMarkers();
        }

        public void OnDisable(MapView map)
        {
            OnRaidEnd(map);
        }

        public void RefreshMarkers()
        {
            if (!GameUtils.IsInRaid() || _map == null || _wishlistManager == null)
                return;

            var world = Singleton<GameWorld>.Instance;
            if (world == null)
                return;

            var available = new HashSet<LootItem>();
            var wishlist = _wishlistManager.GetWishlist();
            
            foreach (var loot in world.LootList)
            {
                if (loot is LootItem item && item.Item != null && wishlist.ContainsKey(new MongoID(item.TemplateId)))
                {
                    available.Add(item);
                    TryAddMarker(item);
                }
            }
            
            foreach (var item in new List<LootItem>(_markers.Keys))
            {
                if (!available.Contains(item))
                {
                    RemoveMarker(item);
                }
            }
        }

        private void OnWishlistChanged(MongoID itemId, EWishlistGroup group)
        {
            RefreshMarkers();
        }

        private void OnRegisterLoot(LootItem item)
        {
            if (_map != null && item?.Item != null && _wishlistManager != null && _wishlistManager.GetWishlist().ContainsKey(new MongoID(item.TemplateId)))
            {
                TryAddMarker(item);
            }
        }

        private void OnDestroyLoot(LootItem item)
        {
            RemoveMarker(item);
        }

        private void TryAddMarker(LootItem item)
        {
            if (item?.Item == null || _markers.ContainsKey(item) || item.TrackableTransform == null || Settings.ShowWishListItemsIntelLevel.Value > GameUtils.GetIntelLevel())
                return;

            var icons = EFTHardSettings.Instance.StaticIcons;
            var type = ItemViewFactory.GetItemType(item.Item.GetType());
            
            var sprite = icons.ItemTypeSprites.GetValueOrDefault(type);
            
            if (sprite == null && _wishlistManager.GetWishlist().TryGetValue(item.TemplateId, out var group))
            {
                sprite = icons.WishlistSprites.GetValueOrDefault(group);
            }
            
            if (sprite == null)
            {
                sprite = icons.WishlistSprites.GetValueOrDefault(EWishlistGroup.Other);
            }
            
            if (sprite == null)
            {
                sprite = icons.BarterSign;
            }
            
            if (sprite != null)
            {
                var map = _map;
                var marker = map.AddTransformMarker(item.TrackableTransform, item.Item.TemplateId.LocalizedName(), "Loot", Settings.LootItemColor.Value, sprite);
                
                marker.Settle(item.RigidBody, settled =>
                {
                    if (!_markers.TryGetValue(item, out var current) || current != settled)
                        return;

                    _markers[item] = map.ReplaceWithStaticMarker(settled, new MapMarkerDef
                    {
                        Text = item.Item.TemplateId.LocalizedName(),
                        Category = "Loot",
                        Color = Settings.LootItemColor.Value,
                        Sprite = sprite
                    });
                });
                
                _markers[item] = marker;
            }
        }

        private void RemoveMarker(LootItem item)
        {
            if (item != null && _markers.TryGetValue(item, out var marker))
            {
                marker.ContainingMapView.RemoveMapMarker(marker);
                _markers.Remove(item);
            }
        }

        private void Unsubscribe()
        {
            if (_wishlistManager == null)
                return;
            
            _wishlistManager.OnAddInWishlist -= OnWishlistChanged;
            _wishlistManager.OnRemoveFromWishlist -= OnWishlistChanged;
            _wishlistManager = null;
        }

        public void OnShowOutOfRaid(MapView map)
        {
            
        }
        
        public void OnHideOutOfRaid(MapView map)
        {
            
        }
    }
}

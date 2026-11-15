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
using EFT.InventoryLogic;
using EFT.UI.DragAndDrop;
using UnityEngine;

namespace DynamicMaps.DynamicMarkers
{
    using MapMarker=UI.Components.MapMarker;

    public class LootMarkerProvider : IDynamicMarkerProvider
    {
        private MapView _lastMapView;
        private readonly Dictionary<LootItem, MapMarker> _lootMarkers = [];
        private readonly Dictionary<Item, (ItemController Owner, MapMarker Marker)> _containerMarkers = [];
        private readonly HashSet<ItemController> _containerOwners = [];
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
            
            ClearContainerMarkers();
            foreach (var owner in _containerOwners)
            {
                owner.AddItemEvent -= OnContainerItemAdded;
                owner.RemoveItemEvent -= OnContainerItemRemoved;
            }
            _containerOwners.Clear();
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
            
            ClearContainerMarkers();
            RefreshContainerMarkers();
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

            RefreshContainerMarkers(gameWorld, wishListItems);
        }

        private void RefreshContainerMarkers()
        {
            if (!GameUtils.IsInRaid() || _lastMapView == null)
                return;

            var gameWorld = Singleton<GameWorld>.Instance;
            if (gameWorld == null)
                return;

            RefreshContainerMarkers(gameWorld, new HashSet<MongoID>(GameUtils.GetWishListItems()));
        }

        private void RefreshContainerMarkers(GameWorld gameWorld, HashSet<MongoID> wishListItems)
        {
            var availableOwners = new Dictionary<ItemController, Transform>();
            if (Settings.ShowWishListItemsInContainers.Value)
            {
                foreach (var loot in gameWorld.LootList)
                {
                    ItemController owner = null;
                    Transform transform = null;
                    if (loot is LootableContainer container)
                    {
                        owner = container.ItemOwner;
                        transform = container.TrackableTransform;
                    }
                    else if (loot is LootItem lootItem)
                    {
                        owner = lootItem.ItemOwner;
                        transform = lootItem.TrackableTransform;
                    }

                    if (owner?.RootItem != null && transform != null)
                    {
                        availableOwners[owner] = transform;
                    }
                }
            }

            foreach (var owner in _containerOwners.ToList())
            {
                if (availableOwners.ContainsKey(owner))
                    continue;

                owner.AddItemEvent -= OnContainerItemAdded;
                owner.RemoveItemEvent -= OnContainerItemRemoved;
                _containerOwners.Remove(owner);
            }

            foreach (var pair in availableOwners)
            {
                if (_containerOwners.Contains(pair.Key))
                    continue;

                pair.Key.AddItemEvent += OnContainerItemAdded;
                pair.Key.RemoveItemEvent += OnContainerItemRemoved;
                _containerOwners.Add(pair.Key);
            }

            var matchingItems = new Dictionary<Item, (ItemController Owner, Transform Transform)>();
            foreach (var pair in availableOwners)
            {
                foreach (var item in pair.Key.RootItem.GetAllItems())
                {
                    if (item != pair.Key.RootItem && wishListItems.Contains(new MongoID(item.TemplateId)))
                    {
                        matchingItems[item] = (pair.Key, pair.Value);
                    }
                }
            }

            foreach (var pair in _containerMarkers.ToList())
            {
                if (!matchingItems.TryGetValue(pair.Key, out var match) || !ReferenceEquals(pair.Value.Owner, match.Owner))
                {
                    pair.Value.Marker.ContainingMapView.RemoveMapMarker(pair.Value.Marker);
                    _containerMarkers.Remove(pair.Key);
                }
                else
                {
                    pair.Value.Marker.Color = Settings.ContainerLootItemColor.Value;
                }
            }

            foreach (var pair in matchingItems)
            {
                if (_containerMarkers.ContainsKey(pair.Key))
                    continue;

                var marker = CreateMarker(pair.Key, pair.Value.Transform, Settings.ContainerLootItemColor.Value);
                if (marker != null)
                {
                    _containerMarkers[pair.Key] = (pair.Value.Owner, marker);
                }
            }
        }

        private void OnContainerItemAdded(AddItemEventArgs args)
        {
            if (args.Status == CommandStatus.Succeed)
                RefreshContainerMarkers();
        }

        private void OnContainerItemRemoved(RemoveItemEventArgs args)
        {
            if (args.Status == CommandStatus.Succeed)
                RefreshContainerMarkers();
        }

        private void ClearContainerMarkers()
        {
            foreach (var pair in _containerMarkers.Values)
            {
                pair.Marker.ContainingMapView.RemoveMapMarker(pair.Marker);
            }
            _containerMarkers.Clear();
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
            if (Settings.ShowWishListItemsInContainers.Value)
                RefreshContainerMarkers();
        }

        private void OnDestroyLoot(LootItem item)
        {
            TryRemoveMarker(item);
            if (item?.ItemOwner != null && _containerOwners.Remove(item.ItemOwner))
            {
                item.ItemOwner.AddItemEvent -= OnContainerItemAdded;
                item.ItemOwner.RemoveItemEvent -= OnContainerItemRemoved;
                foreach (var pair in _containerMarkers.Where(pair => ReferenceEquals(pair.Value.Owner, item.ItemOwner)).ToList())
                {
                    pair.Value.Marker.ContainingMapView.RemoveMapMarker(pair.Value.Marker);
                    _containerMarkers.Remove(pair.Key);
                }
            }
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
            
            var transform = item.TrackableTransform;
            var marker = CreateMarker(item.Item, transform, Settings.LootItemColor.Value);
            if (marker != null)
            {
                _lootMarkers[item] = marker;
            }
        }

        private MapMarker CreateMarker(Item item, Transform transform, Color color)
        {
            if (item == null || transform == null || Settings.ShowWishListItemsIntelLevel.Value > GameUtils.GetIntelLevel())
                return null;

            var staticIcons = EFTHardSettings.Instance.StaticIcons;
            var itemType = ItemViewFactory.GetItemType(item.GetType());
            var itemSprite = staticIcons.ItemTypeSprites.GetValueOrDefault(itemType);
            if (itemSprite == null)
                return null;

            return _lastMapView.AddTransformMarker(transform, item.TemplateId.LocalizedName(), "Loot", color, itemSprite);
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

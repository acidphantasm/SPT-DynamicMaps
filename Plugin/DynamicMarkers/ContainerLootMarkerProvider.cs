using System.Collections.Generic;
using Comfort.Common;
using DynamicMaps.Config;
using DynamicMaps.Data;
using DynamicMaps.Patches;
using DynamicMaps.UI.Components;
using DynamicMaps.Utils;
using EFT;
using EFT.Interactive;
using EFT.InventoryLogic;
using UnityEngine;

namespace DynamicMaps.DynamicMarkers
{
    using MapMarker = UI.Components.MapMarker;

    public class ContainerLootMarkerProvider : IDynamicMarkerProvider
    {
        private static readonly Vector2 MarkerSize = new Vector2(30, 30);
        private const string MarkerPath = "Markers/container.png";

        private sealed class ContainerState
        {
            internal ItemController Owner;
            internal Transform Transform;
            internal MapMarker Marker;
            internal LootItem DroppedItem;
            internal bool Settled;
            internal readonly Dictionary<string, string> Items = [];
        }

        private MapView _map;
        private WishlistManager _wishlistManager;
        private readonly Dictionary<string, ContainerState> _containers = [];
        private bool _lootEventsSubscribed;

        public void OnShowInRaid(MapView map)
        {
            _map = map;
            var manager = GameUtils.GetPlayerProfile()?.WishlistManager;
            
            if (!ReferenceEquals(_wishlistManager, manager))
            {
                UnsubscribeFromWishlist();
                _wishlistManager = manager;
                if (manager != null)
                {
                    manager.OnAddInWishlist += OnWishlistChanged;
                    manager.OnRemoveFromWishlist += OnWishlistChanged;
                }
            }

            if (!_lootEventsSubscribed)
            {
                LootItemInitPatch.OnRegisterLoot += OnRegisterLoot;
                GameWorldDestroyLootPatch.OnDestroyLoot += OnDestroyLoot;
                PlayerInventoryThrowItemPatch.OnThrowItem += OnThrowItem;
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
                PlayerInventoryThrowItemPatch.OnThrowItem -= OnThrowItem;
                _lootEventsSubscribed = false;
            }
            
            foreach (var state in _containers.Values)
            {
                state.Owner.AddItemEvent -= OnItemAdded;
                state.Owner.RemoveItemEvent -= OnItemRemoved;
                RemoveMarker(state);
            }
            
            _containers.Clear();
            _map = null;
        }

        public void OnMapChanged(MapView map, MapDef mapDef)
        {
            _map = map;
            
            foreach (var state in _containers.Values)
            {
                state.Marker = null;
            }
            
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

            var available = new HashSet<string>();
            
            foreach (var loot in world.LootList)
            {
                ItemController owner;
                Transform transform;
                LootItem droppedItem = null;
                
                if (loot is LootableContainer container)
                {
                    owner = container.ItemOwner;
                    transform = container.TrackableTransform;
                }
                else if (loot is LootItem item && IsDroppedContainer(item))
                {
                    owner = item.ItemOwner;
                    transform = item.TrackableTransform;
                    droppedItem = item;
                }
                else
                    continue;

                if (owner?.RootItem == null || transform == null)
                    continue;

                available.Add(owner.ID);
                
                var state = TrackContainer(owner, transform, droppedItem);
                Recount(state);
            }

            foreach (var id in new List<string>(_containers.Keys))
            {
                if (available.Contains(id))
                    continue;
                
                var state = _containers[id];
                
                state.Owner.AddItemEvent -= OnItemAdded;
                state.Owner.RemoveItemEvent -= OnItemRemoved;
                
                RemoveMarker(state);
                
                _containers.Remove(id);
            }
        }

        private static bool IsDroppedContainer(LootItem item)
        {
            return item.Item is Backpack or Vest or MobContainer;
        }

        private ContainerState TrackContainer(ItemController owner, Transform transform, LootItem droppedItem)
        {
            if (_containers.TryGetValue(owner.ID, out var state))
                return state;

            state = new ContainerState
            {
                Owner = owner,
                Transform = transform,
                DroppedItem = droppedItem,
                Settled = droppedItem == null
            };
            
            _containers.Add(owner.ID, state);
            
            owner.AddItemEvent += OnItemAdded;
            owner.RemoveItemEvent += OnItemRemoved;
            
            return state;
        }

        private void OnRegisterLoot(LootItem item)
        {
            if (_map == null || _wishlistManager == null || item?.ItemOwner?.RootItem == null || item.TrackableTransform == null || !IsDroppedContainer(item))
                return;

            Recount(TrackContainer(item.ItemOwner, item.TrackableTransform, item));
        }

        private void OnDestroyLoot(LootItem item)
        {
            if (item?.ItemOwner == null || !_containers.TryGetValue(item.ItemOwner.ID, out var state)
                || state.DroppedItem != item)
                return;

            state.Owner.AddItemEvent -= OnItemAdded;
            state.Owner.RemoveItemEvent -= OnItemRemoved;
            
            RemoveMarker(state);
            _containers.Remove(item.ItemOwner.ID);
        }

        private void OnThrowItem(int itemNetId, Item item)
        {
            if (item is not Backpack)
                return;

            foreach (var state in _containers.Values)
            {
                if (state.DroppedItem != null && state.DroppedItem.GetNetId() == itemNetId)
                {
                    UpdateMarker(state);
                    return;
                }
            }
        }

        private void OnWishlistChanged(MongoID itemId, EWishlistGroup group)
        {
            foreach (var state in _containers.Values)
            {
                Recount(state);
            }
        }

        private void Recount(ContainerState state)
        {
            state.Items.Clear();
            var wishlist = _wishlistManager.GetWishlist();
            
            foreach (var item in state.Owner.RootItem.GetAllItems())
            {
                if (!ReferenceEquals(item, state.Owner.RootItem) && wishlist.ContainsKey(new MongoID(item.TemplateId)))
                {
                    state.Items[item.Id] = item.TemplateId;
                }
            }
            
            UpdateMarker(state);
        }

        private void OnItemAdded(AddItemEventArgs args)
        {
            if (args.Status != CommandStatus.Succeed || args.Item == null || !_containers.TryGetValue(args.OwnerId, out var state))
                return;

            var wishlist = _wishlistManager.GetWishlist();
            
            foreach (var item in args.Item.GetAllItems())
            {
                if (wishlist.ContainsKey(new MongoID(item.TemplateId)))
                {
                    state.Items[item.Id] = item.TemplateId;
                }
            }
            
            UpdateMarker(state);
        }

        private void OnItemRemoved(RemoveItemEventArgs args)
        {
            if (args.Status != CommandStatus.Succeed || args.Item == null || !_containers.TryGetValue(args.OwnerId, out var state))
                return;

            foreach (var item in args.Item.GetAllItems())
            {
                state.Items.Remove(item.Id);
            }
            
            UpdateMarker(state);
        }

        private void UpdateMarker(ContainerState state)
        {
            if (state.Items.Count == 0 || Settings.ShowWishListItemsIntelLevel.Value > GameUtils.GetIntelLevel()
                || state.DroppedItem?.Item is Backpack && Settings.ShowDroppedBackpackInRaid.Value
                && PlayerInventoryThrowItemPatch.ThrownItems.TryGetValue(state.DroppedItem.GetNetId(), out var thrownItem)
                && thrownItem.Id == state.DroppedItem.Item.Id)
            {
                RemoveMarker(state);
                return;
            }
            
            if (state.Marker != null)
            {
                state.Marker.Color = Settings.ContainerLootItemColor.Value;
                return;
            }

            var droppedItem = state.DroppedItem;
            if (state.Settled || droppedItem == null)
            {
                state.Marker = _map.AddMapMarker(new MapMarkerDef
                {
                    Text = "Wishlisted items",
                    Category = "Loot",
                    Color = Settings.ContainerLootItemColor.Value,
                    ImagePath = MarkerPath,
                    Position = MathUtils.ConvertToMapPosition(state.Transform)
                });
                return;
            }

            var map = _map;
            var marker = map.AddTransformMarker(state.Transform, "Wishlisted items", "Loot", Settings.ContainerLootItemColor.Value, MarkerPath, MarkerSize);
            
            marker.Settle(droppedItem.RigidBody, settled =>
            {
                if (state.Marker != settled)
                    return;

                state.Settled = true;
                state.Marker = map.ReplaceWithStaticMarker(settled, new MapMarkerDef
                {
                    Text = "Wishlisted items",
                    Category = "Loot",
                    Color = Settings.ContainerLootItemColor.Value,
                    ImagePath = MarkerPath
                });
            });
            
            state.Marker = marker;
        }

        private static void RemoveMarker(ContainerState state)
        {
            if (state.Marker == null)
                return;
            
            state.Marker.ContainingMapView.RemoveMapMarker(state.Marker);
            state.Marker = null;
        }

        private void UnsubscribeFromWishlist()
        {
            if (_wishlistManager == null)
                return;
            
            _wishlistManager.OnAddInWishlist -= OnWishlistChanged;
            _wishlistManager.OnRemoveFromWishlist -= OnWishlistChanged;
            _wishlistManager = null;
        }

        public void OnShowOutOfRaid(MapView map) { }
        public void OnHideOutOfRaid(MapView map) { }
    }
}

using System;
using System.Collections.Generic;
using System.IO;
using DynamicMaps.Data;
using DynamicMaps.UI.Components;
using DynamicMaps.Utils;
using EFT.InventoryLogic;
using UnityEngine;

namespace DynamicMaps.DynamicMarkers
{
    using MapMarker=UI.Components.MapMarker;

    public class LockedDoorMarkerMutator : IDynamicMarkerProvider
    {
        private static readonly string DoorWithKeyPath = Path.Combine(Plugin.Path, "Markers/door_with_key.png");
        private static readonly string DoorWithLockPath = Path.Combine(Plugin.Path, "Markers/door_with_lock.png");
        
        private readonly Dictionary<string, List<MapMarker>> _doorMarkersByKey = new(StringComparer.Ordinal);
        private readonly Dictionary<string, int> _keyCounts = new(StringComparer.Ordinal);
        private readonly HashSet<Item> _trackedKeys = [];
        private InventoryController _inventoryController;
        private InventoryEquipment _equipment;

        public void OnShowInRaid(MapView map)
        {
            StopTracking();

            var player = GameUtils.GetMainPlayer();
            if (player == null)
                return;

            _equipment = player.Inventory.Equipment;
            _inventoryController = player.InventoryController;

            foreach (var marker in map.GetMapMarkersByCategory("LockedDoor"))
            {
                if (string.IsNullOrWhiteSpace(marker.AssociatedItemId))
                    continue;

                if (!_doorMarkersByKey.TryGetValue(marker.AssociatedItemId, out var markers))
                {
                    markers = [];
                    _doorMarkersByKey[marker.AssociatedItemId] = markers;
                }
                
                markers.Add(marker);
            }

            if (_doorMarkersByKey.Count == 0)
                return;

            foreach (var item in _equipment.GetAllItems())
            {
                TrackKey(item);
            }

            foreach (var keyId in _doorMarkersByKey.Keys)
            {
                UpdateDoors(keyId);
            }

            if (_inventoryController == null)
                return;

            _inventoryController.AddItemEvent += OnItemAdded;
            _inventoryController.RemoveItemEvent += OnItemRemoved;
        }

        public void OnShowOutOfRaid(MapView map)
        {
            var profile = GameUtils.GetPlayerProfile();
            var markers = new List<MapMarker>();
            
            foreach (var marker in map.GetMapMarkersByCategory("LockedDoor"))
            {
                if (!string.IsNullOrWhiteSpace(marker.AssociatedItemId))
                {
                    markers.Add(marker);
                }
            }
            
            if (markers.Count == 0)
                return;

            var stashItems = new HashSet<string>(StringComparer.Ordinal);
            var equipmentItems = new HashSet<string>(StringComparer.Ordinal);

            foreach (var item in profile.Inventory.Stash.GetAllItems())
            {
                stashItems.Add(item.StringTemplateId);
            }
            
            foreach (var item in profile.Inventory.Equipment.GetAllItems())
            {
                equipmentItems.Add(item.StringTemplateId);
            }

            foreach (var marker in markers)
            {
                var keyInStash = stashItems.Contains(marker.AssociatedItemId);
                var keyInEquipment = equipmentItems.Contains(marker.AssociatedItemId);

                // change icon
                marker.SetImagePath(keyInStash || keyInEquipment ? DoorWithKeyPath : DoorWithLockPath);

                // change color
                marker.Color = keyInEquipment
                    ? Color.green
                    : keyInStash
                        ? Color.yellow
                        : Color.red;
            }
        }

        public void OnMapChanged(MapView map, MapDef mapDef)
        {
            if (GameUtils.IsInRaid())
            {
                OnShowInRaid(map);
            }
            else
            {
                OnShowOutOfRaid(map);
            }
        }

        public void OnDisable(MapView map)
        {
            StopTracking();
            var markers = map.GetMapMarkersByCategory("LockedDoor");

            // replace all markers with yellow and with lock
            foreach (var marker in markers)
            {
                if (string.IsNullOrWhiteSpace(marker.AssociatedItemId))
                {
                    continue;
                }

                marker.SetImagePath(DoorWithLockPath);
                marker.Color = Color.yellow;
            }
        }

        public void OnRaidEnd(MapView map)
        {
            StopTracking();
        }

        public void OnHideInRaid(MapView map)
        {
            StopTracking();
        }

        public void OnHideOutOfRaid(MapView map)
        {
            // do nothing
        }

        private void OnItemAdded(AddItemEventArgs args)
        {
            if (args.Status != CommandStatus.Succeed || !args.To.IsChildOf(_equipment))
                return;

            UpdateMovedItems(args.Item, true);
        }

        private void OnItemRemoved(RemoveItemEventArgs args)
        {
            if (args.Status != CommandStatus.Succeed || !args.From.IsChildOf(_equipment))
                return;

            UpdateMovedItems(args.Item, false);
        }

        private void UpdateMovedItems(Item item, bool added)
        {
            if (item is ContainerCollection)
            {
                foreach (var child in item.GetAllItems())
                {
                    UpdateKey(child, added);
                }
            }
            else
            {
                UpdateKey(item, added);
            }
        }

        private void UpdateKey(Item item, bool added)
        {
            if (item == null || !_doorMarkersByKey.ContainsKey(item.StringTemplateId))
                return;

            if (added)
            {
                if (TrackKey(item) && _keyCounts[item.StringTemplateId] == 1)
                    UpdateDoors(item.StringTemplateId);
            }
            else if (_trackedKeys.Remove(item))
            {
                var count = --_keyCounts[item.StringTemplateId];
                if (count == 0)
                    UpdateDoors(item.StringTemplateId);
            }
        }

        private bool TrackKey(Item item)
        {
            if (item == null || !_doorMarkersByKey.ContainsKey(item.StringTemplateId) || !_trackedKeys.Add(item))
                return false;

            _keyCounts.TryGetValue(item.StringTemplateId, out var count);
            _keyCounts[item.StringTemplateId] = count + 1;
            return true;
        }

        private void UpdateDoors(string keyId)
        {
            var hasKey = _keyCounts.TryGetValue(keyId, out var count) && count > 0;
            foreach (var marker in _doorMarkersByKey[keyId])
            {
                marker.SetImagePath(hasKey ? DoorWithKeyPath : DoorWithLockPath);
                marker.Color = hasKey ? Color.green : Color.red;
            }
        }

        private void StopTracking()
        {
            if (_inventoryController != null)
            {
                _inventoryController.AddItemEvent -= OnItemAdded;
                _inventoryController.RemoveItemEvent -= OnItemRemoved;
                _inventoryController = null;
            }

            _equipment = null;
            _doorMarkersByKey.Clear();
            _keyCounts.Clear();
            _trackedKeys.Clear();
        }
    }
}

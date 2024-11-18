using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Comfort.Common;
using DynamicMaps.Config;
using DynamicMaps.Data;
using DynamicMaps.UI.Components;
using DynamicMaps.Utils;
using EFT;
using EFT.Interactive;
using EFT.UI.DragAndDrop;
using HarmonyLib;
using UnityEngine;

namespace DynamicMaps.DynamicMarkers
{
    public class LootMarkerProvider : IDynamicMarkerProvider
    {
        private MapView _lastMapView;
        private Dictionary<LootItem, MapMarker> _lootMarkers = [];
        
        public void OnShowInRaid(MapView map)
        {
            _lastMapView = map;
            
            var lootList = Singleton<GameWorld>.Instance.LootList;
            
            foreach (var item in lootList)
            {
                if (item is not LootItem loot) continue;
                
                if (GameUtils.GetWishListItems().Contains(loot.TemplateId))
                {
                    TryAddMarker(loot);
                }
            }
        }

        public void OnHideInRaid(MapView map)
        {
            // Do nothing
        }

        public void OnRaidEnd(MapView map)
        {
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

            foreach (var item in _lootMarkers.Keys.ToList())
            {
                TryRemoveMarker(item);
                TryAddMarker(item);
            }
        }
        
        private void TryAddMarker(LootItem item)
        {
            if (_lootMarkers.ContainsKey(item)) return;
            if (Settings.ShowWishListItemsIntelLevel.Value > GameUtils.GetIntelLevel()) return;
            
            var staticIcons = EFTHardSettings.Instance.StaticIcons;
            var itemType = ItemViewFactory.GetItemType(item.Item.GetType());
            var itemSprite = staticIcons.ItemTypeSprites.GetValueOrDefault(itemType);

            var markerDef = new MapMarkerDef
            {
                Category = "Loot",
                Color = Settings.LootItemColor.Value,
                Sprite = itemSprite,
                Position = MathUtils.ConvertToMapPosition(item.transform),
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
using System.Collections.Generic;
using System.Linq;
using DynamicMaps.Config;
using DynamicMaps.Data;
using DynamicMaps.Patches;
using DynamicMaps.UI;
using DynamicMaps.UI.Components;
using DynamicMaps.Utils;
using EFT.SynchronizableObjects;
using UnityEngine;

namespace DynamicMaps.DynamicMarkers
{
    public class AirdropMarkerProvider : IDynamicMarkerProvider
    {
        private MapView _lastMapView;
        private Dictionary<AirdropSynchronizableObject, MapMarker> _airdropMarkers = [];

        // TODO: move to config
        private const string _airdropName = "Airdrop";
        private const string _airdropCategory = "Airdrop";
        private const string _airdropImagePath = "Markers/airdrop.png";
        private static Vector2 _airdropPivot = new Vector2(0.5f, 0.25f);
        //

        public void OnShowInRaid(MapView map)
        {
            _lastMapView = map;

            // add all existing airdrops
            foreach (var airdrop in AirdropBoxOnBoxLandPatch.Airdrops)
            {
                TryAddMarker(airdrop);
            }

            // subscribe to new airdrops while map is open
            AirdropBoxOnBoxLandPatch.OnAirdropLanded += TryAddMarker;
        }

        public void OnHideInRaid(MapView map)
        {
            // unsubscribe to new airdrops, since we're hiding
            AirdropBoxOnBoxLandPatch.OnAirdropLanded -= TryAddMarker;
        }

        public void OnRaidEnd(MapView map)
        {
            TryRemoveMarkers();
        }

        public void OnMapChanged(MapView map, MapDef mapDef)
        {
            _lastMapView = map;

            // transition markers from last map to this one
            foreach (var airdrop in _airdropMarkers.Keys.ToList())
            {
                TryRemoveMarker(airdrop);
                TryAddMarker(airdrop);
            }
        }

        public void OnDisable(MapView map)
        {
            OnRaidEnd(map);
        }

        public void RefreshMarkers()
        {
            if (!GameUtils.IsInRaid()) return;

            foreach (var drop in _airdropMarkers.ToArray())
            {
                TryRemoveMarker(drop.Key);
            }

            foreach (var drop in AirdropBoxOnBoxLandPatch.Airdrops)
            {
                TryAddMarker(drop);
            }
        }

        private void TryAddMarker(AirdropSynchronizableObject airdrop)
        {
            if (_airdropMarkers.ContainsKey(airdrop))
            {
                return;
            }

            var intelLevel = GameUtils.GetIntelLevel();

            if (ModdedMapScreen._config.ShowAirdropIntelLevel > intelLevel) return;

            var markerDef = new MapMarkerDef
            {
                Category = _airdropCategory,
                Color = Settings.AirdropColor.Value,
                ImagePath = _airdropImagePath,
                Position = MathUtils.ConvertToMapPosition(airdrop.transform),
                Pivot = _airdropPivot,
                Text = _airdropName
            };

            var marker = _lastMapView.AddMapMarker(markerDef);
            _airdropMarkers[airdrop] = marker;
        }

        private void TryRemoveMarkers()
        {
            foreach (var airdrop in _airdropMarkers.Keys.ToList())
            {
                TryRemoveMarker(airdrop);
            }
        }

        private void TryRemoveMarker(AirdropSynchronizableObject airdrop)
        {
            if (!_airdropMarkers.ContainsKey(airdrop))
            {
                return;
            }

            _airdropMarkers[airdrop].ContainingMapView.RemoveMapMarker(_airdropMarkers[airdrop]);
            _airdropMarkers.Remove(airdrop);
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

using DynamicMaps.Config;
using DynamicMaps.Data;
using DynamicMaps.UI.Components;
using DynamicMaps.Utils;
using UnityEngine;

namespace DynamicMaps.DynamicMarkers
{
    public class PlayerMarkerProvider : IDynamicMarkerProvider
    {
        // TODO: move to config
        private const string _playerCategory = "Main Player";
        private const string _playerImagePath = "Markers/arrow.png";
        //

        private PlayerMapMarker _playerMarker;

        public void OnShowInRaid(MapView map)
        {
            TryAddMarker(map);
        }

        public void OnRaidEnd(MapView map)
        {
            TryRemoveMarker();
        }

        public void OnMapChanged(MapView map, MapDef mapDef)
        {
            TryRemoveMarker();

            if (GameUtils.IsInRaid())
            {
                TryAddMarker(map);
            }
        }

        public void OnDisable(MapView map)
        {
            TryRemoveMarker();
        }

        private void TryAddMarker(MapView map)
        {
            if (_playerMarker is not null)
            {
                return;
            }

            var player = GameUtils.GetMainPlayer();
            if (player is null || player.IsHeadlessClient())
            {
                return;
            }

            var color = Settings.PlayerColor.Value;
            
            // try adding the marker
            _playerMarker = map.AddPlayerMarker(player, _playerCategory, color, _playerImagePath);
        }

        private void TryRemoveMarker()
        {
            if (_playerMarker is null)
            {
                return;
            }

            _playerMarker.ContainingMapView.RemoveMapMarker(_playerMarker);
            _playerMarker = null;
        }

        public void OnHideInRaid(MapView map)
        {
            // do nothing
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

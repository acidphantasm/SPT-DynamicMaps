using System.Collections.Generic;
using DynamicMaps.Data;
using DynamicMaps.UI.Components;
using DynamicMaps.Utils;
using DynamicMaps.DynamicMarkers;
using UnityEngine;
using Comfort.Common;
using EFT;
using DynamicMaps.Config;

namespace DynamicMaps.ExternalModSupport.SamSWATHeliCrash
{
    public class HeliCrashMarkerProvider : IDynamicMarkerProvider
    {
        private MapView _lastMapView;
        private List<MapMarker> _HeliCrashMarkers = new List<MapMarker>();

        // TODO: move to config
        private const string _markerName = "Crashed Helicopter";
        private const string _markerCategory = "Airdrop";
        private const string _markerImagePath = "Markers/helicopter.png";
        private static Vector2 _markerPivot = new Vector2(0.5f, 0.25f);
        private static Color _markerColor = Color.Lerp(Color.red, Color.white, 0.333f);
        //

        public void OnShowInRaid(MapView map)
        {
            _lastMapView = map;
            // There should be only 1 crashed Heli on the Map. Therefore stop refreshing and adding markers when 1 is allready on the map
            if (_HeliCrashMarkers.Count > 0)
            {
                return;
            }
            TryAddMarker();
            
        }

        public void OnHideInRaid(MapView map)
        {
            // Do Nothing
        }

        public void OnRaidEnd(MapView map)
        {
            TryRemoveMarker();
        }

        public void OnMapChanged(MapView map, MapDef mapDef)
        {
            // Do Nothing
        }

        public void OnDisable(MapView map)
        {
            OnRaidEnd(map);
        }

        private void TryAddMarker()
        {
            var gameWorld = Singleton<GameWorld>.Instance;
            var HeliCrashWorldItem = gameWorld.FindItemWithWorldData(Settings.HeliCrashItemID.Value);
            var (itemData, WorldData) = HeliCrashWorldItem.Value;

            if (itemData == null){
                return;
            }

            var markerDef = new MapMarkerDef
            {
                Category = _markerCategory,
                Color = _markerColor,
                ImagePath = _markerImagePath,
                Position = MathUtils.ConvertToMapPosition(WorldData.Transform),
                Pivot = _markerPivot,
                Text = _markerName
            };

            var marker = _lastMapView.AddMapMarker(markerDef);
            _HeliCrashMarkers.Add(marker);
        }

        private void TryRemoveMarker()
        {
            if (_HeliCrashMarkers.Count == 0)
            {
                return;
            }

            _HeliCrashMarkers[0].ContainingMapView.RemoveMapMarker(_HeliCrashMarkers[0]);
            _HeliCrashMarkers.Remove(_HeliCrashMarkers[0]);
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

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
        private readonly List<MapMarker> _heliCrashMarkers = [];

        // TODO: move to config
        private const string MarkerName = "Crashed Helicopter";
        private const string MarkerCategory = "Airdrop";
        private const string MarkerImagePath = "Markers/helicopter.png";
        private static readonly Vector2 MarkerPivot = new Vector2(0.5f, 0.25f);
        private static readonly Color MarkerColor = Color.Lerp(Color.red, Color.white, 0.333f);
        //

        public void OnShowInRaid(MapView map)
        {
            _lastMapView = map;
            // There should be only 1 crashed Heli on the Map. Therefore stop refreshing and adding markers when 1 is allready on the map
            if (_heliCrashMarkers.Count > 0)
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
            var heliCrashWorldItem = gameWorld.FindItemWithWorldData("6223349b3136504a544d1608");
            var (itemData, worldData) = heliCrashWorldItem.Value;

            if (itemData == null){
                return;
            }

            var markerDef = new MapMarkerDef
            {
                Category = MarkerCategory,
                Color = MarkerColor,
                ImagePath = MarkerImagePath,
                Position = MathUtils.ConvertToMapPosition(worldData.Transform),
                Pivot = MarkerPivot,
                Text = MarkerName
            };

            var marker = _lastMapView.AddMapMarker(markerDef);
            _heliCrashMarkers.Add(marker);
        }

        private void TryRemoveMarker()
        {
            if (_heliCrashMarkers.Count == 0)
            {
                return;
            }

            _heliCrashMarkers[0].ContainingMapView.RemoveMapMarker(_heliCrashMarkers[0]);
            _heliCrashMarkers.Remove(_heliCrashMarkers[0]);
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

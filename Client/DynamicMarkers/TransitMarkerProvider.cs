using System.Collections.Generic;
using System.Linq;
using DynamicMaps.Config;
using DynamicMaps.Data;
using DynamicMaps.UI.Components;
using DynamicMaps.Utils;
using EFT.Interactive;

namespace DynamicMaps.DynamicMarkers;

public class TransitMarkerProvider : IDynamicMarkerProvider
{
    private const string TransitCategory = "Transit";
    private const string TransitImagePath = "Markers/transit.png";

    private Dictionary<TransitPoint, MapMarker> _transitMarkers = [];
    
    public void OnShowInRaid(MapView map)
    {
        // get valid transits only on first time that this is run in a raid
        if (_transitMarkers.Count == 0)
        {
            AddTransitMarkers(map);
        }
    }

    public void OnHideInRaid(MapView map)
    {
        // Do Nothing
    }

    public void OnRaidEnd(MapView map)
    {
        TryRemoveMarkers();
    }

    public void OnMapChanged(MapView map, MapDef mapDef)
    {
        foreach (var point in _transitMarkers.Keys.ToList())
        {
            TryRemoveMarker(point);
            TryAddMarker(map, point);
        }
    }

    public void OnDisable(MapView map)
    {
        TryRemoveMarkers();
    }

    public void RefreshMarkers(MapView map)
    {
        foreach (var transit in _transitMarkers.Keys.ToList())
        {
            TryRemoveMarker(transit);
            TryAddMarker(map, transit);
        }
    }
    
    private void AddTransitMarkers(MapView map)
    {
        foreach (var point in LocationScene.GetAllObjects<TransitPoint>())
        {
            TryAddMarker(map, point);
        }
    }

    private void TryAddMarker(MapView map, TransitPoint point)
    {
        if (_transitMarkers.ContainsKey(point))
        {
            return;
        }

        var markerDef = new MapMarkerDef
        {
            Category = TransitCategory,
            ImagePath = TransitImagePath,
            Text = point.parameters.description.BSGLocalized(),
            Position = MathUtils.ConvertToMapPosition(point.transform),
            Color = Settings.TransPointColor.Value
        };

        var marker = map.AddMapMarker(markerDef);
        _transitMarkers[point] = marker;
    }

    private void TryRemoveMarkers()
    {
        foreach (var transit in _transitMarkers.Keys.ToList())
        {
            TryRemoveMarker(transit);
        }
    }

    private void TryRemoveMarker(TransitPoint transit)
    {
        if (!_transitMarkers.ContainsKey(transit))
        {
            return;
        }
        
        _transitMarkers[transit].ContainingMapView.RemoveMapMarker(_transitMarkers[transit]);
        _transitMarkers.Remove(transit);
    }
    
    public void OnShowOutOfRaid(MapView map)
    {
        // Do Nothing
    }

    public void OnHideOutOfRaid(MapView map)
    {
        // Do Nothing
    }
}
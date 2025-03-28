using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using Comfort.Common;
using DynamicMaps.Config;
using DynamicMaps.Data;
using DynamicMaps.UI.Components;
using DynamicMaps.Utils;
using EFT;
using EFT.Interactive;
using EFT.Interactive.SecretExfiltrations;

namespace DynamicMaps.DynamicMarkers;

public class SecretMarkerProvider : IDynamicMarkerProvider
{
    private const string SecretCategory = "Secret";
    private const string SecretImagePath = "Markers/exit.png";


    private bool _showExtractStatusInRaid = true;
    public bool ShowExtractStatusInRaid
    {
        get
        {
            return _showExtractStatusInRaid;
        }

        set
        {
            if (_showExtractStatusInRaid == value)
            {
                return;
            }

            _showExtractStatusInRaid = value;

            // force update all statuses
            foreach (var point in _secretMarkers.Keys)
            {
                UpdateSecretExtractStatus(point, point.Status);
            }
        }
    }

    private Dictionary<SecretExfiltrationPoint, MapMarker> _secretMarkers = [];
    
    public void OnShowInRaid(MapView map)
    {
        // get valid secrets only on first time that this is run in a raid
        if (_secretMarkers.Count == 0)
        {
            AddSecretMarkers(map);
        }

        foreach (var point in _secretMarkers.Keys)
        {
            // update color based on exfil status
            UpdateSecretExtractStatus(point, point.Status);

            // subscribe to status changes while map is shown
            point.OnStatusChanged += UpdateSecretExtractStatus;
        }
    }

    public void OnHideInRaid(MapView map)
    {
        foreach (var extract in _secretMarkers.Keys)
        {
            extract.OnStatusChanged -= UpdateSecretExtractStatus;
        }
    }

    public void OnRaidEnd(MapView map)
    {
        TryRemoveMarkers();
    }

    public void OnMapChanged(MapView map, MapDef mapDef)
    {
        foreach (var point in _secretMarkers.Keys.ToList())
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
        foreach (var secret in _secretMarkers.Keys.ToList())
        {
            TryRemoveMarker(secret);
            TryAddMarker(map, secret);
        }
    }
    
    private void AddSecretMarkers(MapView map)
    {
        var gameWorld = Singleton<GameWorld>.Instance;
        var player = GameUtils.GetMainPlayer();

        IEnumerable<SecretExfiltrationPoint> secrets;
        secrets = gameWorld.ExfiltrationController.SecretExfiltrationPoints;

        // add markers, only this single time
        foreach (var secret in secrets)
        {
            TryAddMarker(map, secret);
        }
    }

    private void TryAddMarker(MapView map, SecretExfiltrationPoint point)
    {
        if (_secretMarkers.ContainsKey(point))
        {
            return;
        }

        var markerDef = new MapMarkerDef
        {
            Category = SecretCategory,
            ImagePath = SecretImagePath,
            Text = point.Settings.Name.BSGLocalized(),
            Position = MathUtils.ConvertToMapPosition(point.transform),
            Color = Settings.SecretPointColor.Value
        };

        var marker = map.AddMapMarker(markerDef);
        _secretMarkers[point] = marker;


        UpdateSecretExtractStatus(point, point.Status);
    }

    private void TryRemoveMarkers()
    {
        foreach (var secret in _secretMarkers.Keys.ToList())
        {
            TryRemoveMarker(secret);
        }
    }

    private void UpdateSecretExtractStatus(ExfiltrationPoint point, EExfiltrationStatus status)
    {
        if (!_secretMarkers.ContainsKey(point as SecretExfiltrationPoint))
        {
            return;
        }

        var marker = _secretMarkers[point as SecretExfiltrationPoint];
        if (!_showExtractStatusInRaid)
        {
            marker.Color = Settings.SecretPointColor.Value;
            return;
        }

        switch (point.Status)
        {
            case EExfiltrationStatus.Hidden:
                marker.Color = Settings.SecretPointColor.Value;
                return;
            case EExfiltrationStatus.UncompleteRequirements:
                marker.Color = Settings.ExtractHasRequirementsColor.Value;
                return;
            case EExfiltrationStatus.Countdown:
                marker.Color = Settings.ExtractOpenColor.Value;
                return;
            default:
                marker.Color = Settings.SecretPointColor.Value;
                break;
        }
    }

    private void TryRemoveMarker(SecretExfiltrationPoint secret)
    {
        if (!_secretMarkers.ContainsKey(secret))
        {
            return;
        }

        secret.OnStatusChanged -= UpdateSecretExtractStatus;

        _secretMarkers[secret].ContainingMapView.RemoveMapMarker(_secretMarkers[secret]);
        _secretMarkers.Remove(secret);
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
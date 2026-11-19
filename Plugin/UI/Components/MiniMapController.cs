using DynamicMaps.Config;
using DynamicMaps.Data;
using DynamicMaps.Utils;
using EFT;
using UnityEngine;

namespace DynamicMaps.UI.Components
{
    internal sealed class MiniMapController
    {
        private readonly MiniMapViewport _viewport = new();
        
        private float _requestedZoom;
        private bool _zoomDirty = true;
        private bool _zoomInputActive;
        private float _zoomSpeed;
        private float _followTimer;
        
        internal MapViewportState State
        {
            get => _viewport.State;
        }

        internal void ReadConfig()
        {
            var zoom = Settings.ZoomMiniMap.Value;
            if (!Mathf.Approximately(_requestedZoom, zoom))
            {
                _requestedZoom = zoom;
                _zoomDirty = true;
            }
            _zoomSpeed = Settings.ZoomMapHotkeySpeed.Value;
        }

        internal void Invalidate(MapDef definition)
        {
            _viewport.Invalidate(definition);
            _zoomDirty = true;
            _followTimer = 0f;
            _zoomInputActive = false;
        }

        internal void Open(MapDef definition, Vector2 size)
        {
            _viewport.Configure(definition, size, _zoomDirty ? _requestedZoom : _viewport.NormalizedZoom);
            _zoomDirty = false;
            _followTimer = 0f;
        }

        internal void Close()
        {
            if (_zoomInputActive)
            {
                CommitZoom();
            }
            _zoomInputActive = false;
        }

        internal void UpdateMap(MapView view, Player player, float deltaTime, bool allowInput)
        {
            if (!_viewport.Initialized)
            {
                return;
            }
            if (_zoomDirty)
            {
                _viewport.SetNormalizedZoom(_requestedZoom);
                _zoomDirty = false;
            }
            
            var zoomDirection = 0f;
            if (allowInput)
            {
                if (Settings.ZoomInMiniMapHotkey.Value.BetterIsPressed())
                {
                    zoomDirection++;
                }
                if (Settings.ZoomOutMiniMapHotkey.Value.BetterIsPressed())
                {
                    zoomDirection--;
                }
            }
            
            if (zoomDirection != 0f)
            {
                _viewport.SetZoom(_viewport.State.Zoom * (1f + zoomDirection * _zoomSpeed * deltaTime));
                _zoomInputActive = true;
            }
            else if (_zoomInputActive)
            {
                CommitZoom();
                _zoomInputActive = false;
            }
            
            if (player == null)
            {
                return;
            }
            
            _followTimer -= deltaTime;
            if (_followTimer <= 0f || zoomDirection != 0f)
            {
                var position = MathUtils.ConvertToMapPosition(((IPlayer)player).Position);
                _viewport.State.Center = position;
                _viewport.State.Level = view.GetLevelAtPosition(position, _viewport.State.Level);
                _followTimer = 0.033f;
            }
        }

        private void CommitZoom()
        {
            _requestedZoom = _viewport.NormalizedZoom;
            Settings.ZoomMiniMap.Value = _requestedZoom;
        }
    }

    internal sealed class MiniMapViewport
    {
        private MapDef Definition { get; set; }
        internal bool Initialized { get; private set; }
        internal MapViewportState State;
        private float ZoomMin { get; set; } = 1f;
        private float ZoomMax { get; set; } = 10f;
        internal float NormalizedZoom { get; private set; }

        internal void Invalidate(MapDef definition)
        {
            Definition = definition;
            Initialized = false;
        }

        internal void Configure(MapDef definition, Vector2 size, float normalizedZoom)
        {
            if (!Initialized || Definition != definition)
            {
                State.Center = MathUtils.GetMidpoint(definition.Bounds.Min, definition.Bounds.Max);
                State.Level = definition.DefaultLevel;
            }
            
            Definition = definition;
            
            var mapSize = MathUtils.GetRotatedRectangle(definition.Bounds.Max - definition.Bounds.Min, definition.CoordinateRotation);
            ZoomMin = Mathf.Min(size.x / mapSize.x, size.y / mapSize.y) / 1.1f;
            ZoomMax = ZoomMin * 10f;
            
            SetNormalizedZoom(normalizedZoom);
            Initialized = true;
        }

        internal void SetNormalizedZoom(float normalizedZoom)
        {
            NormalizedZoom = Mathf.Clamp01(normalizedZoom);
            State.Zoom = Mathf.Lerp(ZoomMin, ZoomMax, NormalizedZoom);
        }

        internal void SetZoom(float zoom)
        {
            State.Zoom = Mathf.Clamp(zoom, ZoomMin, ZoomMax);
            NormalizedZoom = Mathf.InverseLerp(ZoomMin, ZoomMax, State.Zoom);
        }
    }
}

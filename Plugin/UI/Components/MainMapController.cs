using System.Linq;
using DynamicMaps.Config;
using DynamicMaps.Data;
using DynamicMaps.Utils;
using EFT;
using UnityEngine;

namespace DynamicMaps.UI.Components
{
    internal sealed class MainMapController
    {
        private readonly MainMapViewport _viewport = new();
        
        private bool _retainPosition;
        private bool _autoCenter;
        private bool _resetZoom;
        private bool _autoLevel;
        private bool _zoomToMouse;
        
        private bool _hasRetainedPosition;
        
        private bool _zoomDirty = true;
        private bool _zoomInputActive;
        private float _requestedZoom;
        private float _zoomSpeed;
        private float _resetPoint;
        
        private float _scrollTarget;
        private Vector2 _scrollFocus;
        
        private bool _centering;
        private float _centerTime;
        private MapViewportState _centerFrom;
        private MapViewportState _centerTo;

        internal MapViewportState State
        {
            get => _viewport.State;
        }
        private float NormalizedZoom
        {
            get => _zoomDirty || !_viewport.Initialized ? _requestedZoom : _viewport.NormalizedZoom;
        }
        internal bool HasViewportStateFor(MapDef definition) => _viewport.Initialized && _viewport.Definition == definition;
        internal void ReadConfig()
        {
            var configuredZoom = Settings.ZoomMainMap.Value;
            if (!Mathf.Approximately(_requestedZoom, configuredZoom))
            {
                _requestedZoom = configuredZoom;
                _zoomDirty = true;
            }
            
            _retainPosition = Settings.RetainMapPosition.Value;
            _autoCenter = Settings.AutoCenterOnPlayerMarker.Value;
            _resetZoom = Settings.ResetZoomOnCenter.Value;
            _autoLevel = Settings.AutoSelectLevel.Value;
            _zoomToMouse = Settings.ZoomMainMapToMouse.Value;
            _zoomSpeed = Settings.ZoomMapHotkeySpeed.Value;
            _resetPoint = Settings.CenteringZoomResetPoint.Value;
        }

        internal void Invalidate(MapDef definition)
        {
            _viewport.Invalidate(definition);
            _hasRetainedPosition = false;
            _centering = false;
            _scrollTarget = 0f;
            _zoomInputActive = false;
            _zoomDirty = true;
        }

        internal void Open(MapDef definition, Vector2 size, Player player, bool inRaid)
        {
            _viewport.Configure(definition, size, inRaid ? NormalizedZoom : 0f);
            _zoomDirty = !inRaid;
            _scrollTarget = 0f;
            _centering = false;
            if (!inRaid)
            {
                _viewport.State.Center = MathUtils.GetMidpoint(definition.Bounds.Min, definition.Bounds.Max);
                _hasRetainedPosition = false;
                return;
            }
            
            if (!(_retainPosition && _hasRetainedPosition))
            {
                _viewport.State.Center = MathUtils.GetMidpoint(definition.Bounds.Min, definition.Bounds.Max);
                if (_autoCenter && player != null)
                {
                    Center(player, false);
                }
                else if (!_autoCenter && !_resetZoom && !_retainPosition)
                {
                    _viewport.SetNormalizedZoom(0f);
                }
            }
            
            if (_autoLevel && player != null && !(_retainPosition && _hasRetainedPosition))
            {
                _viewport.State.Level = definition.DefaultLevel;
            }
        }

        internal void Resize(MapDef definition, Vector2 size)
        {
            var inRaid = GameUtils.IsInRaid();
            _viewport.Configure(definition, size, inRaid ? NormalizedZoom : 0f);
            _zoomDirty = !inRaid;
            
            if (!inRaid)
            {
                _viewport.State.Center = MathUtils.GetMidpoint(definition.Bounds.Min, definition.Bounds.Max);
            }
        }

        internal void Close()
        {
            _hasRetainedPosition = _retainPosition && GameUtils.IsInRaid() && _viewport.Initialized;
            _scrollTarget = 0f;
            _centering = false;
            
            if (_zoomInputActive)
            {
                CommitZoom();
            }
            _zoomInputActive = false;
        }

        internal void AcceptDrag(MapView view)
        {
            if (!_viewport.Initialized)
            {
                return;
            }
            _viewport.State.Center = _viewport.ClampCenter(view.CaptureViewportState().Center);
            _scrollTarget = 0f;
            _centering = false;
        }

        internal void ChangeLevel(int level)
        {
            _viewport.State.Level = level;
        }

        internal void UpdateMap(MapView view, Player player, float deltaTime, bool peek, bool allowInput, bool dropdownOpen)
        {
            if (!_viewport.Initialized)
            {
                return;
            }
            
            if (_zoomDirty && GameUtils.IsInRaid())
            {
                _viewport.SetNormalizedZoom(_requestedZoom);
                _zoomDirty = false;
                _scrollTarget = 0f;
            }
            
            if (!allowInput)
            {
                return;
            }

            var levelDelta = 0;
            
            if (Settings.ChangeMapLevelUpHotkey.Value.BetterIsDown())
            {
                levelDelta++;
            }
            
            if (Settings.ChangeMapLevelDownHotkey.Value.BetterIsDown())
            {
                levelDelta--;
            }
            
            var wheel = !peek && !dropdownOpen ? Input.GetAxis("Mouse ScrollWheel") : 0f;
            if (wheel != 0f && Input.GetKey(KeyCode.LeftShift))
            {
                levelDelta += wheel > 0f ? 1 : -1;
                wheel = 0f;
            }
            
            if (levelDelta != 0)
            {
                _viewport.ChangeLevelBy(levelDelta);
            }

            if (GameUtils.IsInRaid() && Settings.CenterOnPlayerHotkey.Value.BetterIsDown() && player != null)
            {
                Center(player, Settings.MapTransitionEnabled.Value);
                
                _centerTo.Level = view.GetLevelAtPosition(MathUtils.ConvertToMapPosition(((IPlayer)player).Position), _centerTo.Level);
                if (!_centering)
                {
                    _viewport.State.Level = _centerTo.Level;
                }
            }

            var shift = Vector2.zero;
            if (Settings.MoveMapUpHotkey.Value.BetterIsPressed())
            {
                shift.y++;
            }
            if (Settings.MoveMapDownHotkey.Value.BetterIsPressed())
            {
                shift.y--;
            }
            if (Settings.MoveMapLeftHotkey.Value.BetterIsPressed())
            {
                shift.x--;
            }
            if (Settings.MoveMapRightHotkey.Value.BetterIsPressed())
            {
                shift.x++;
            }
            if (shift != Vector2.zero)
            {
                _scrollTarget = 0f;
                _centering = false;
                var bounds = _viewport.Definition.Bounds;
                var step = Mathf.Min(bounds.Max.x - bounds.Min.x, bounds.Max.y - bounds.Min.y);
                _viewport.State.Center -= MathUtils.GetRotatedVector2(shift, -_viewport.Definition.CoordinateRotation)
                    * (step * Settings.MapMoveHotkeySpeed.Value * deltaTime);
                _viewport.State.Center = _viewport.ClampCenter(_viewport.State.Center);
            }

            var keyboardZoom = 0f;
            if (Settings.ZoomMapInHotkey.Value.BetterIsPressed())
            {
                keyboardZoom++;
            }
            if (Settings.ZoomMapOutHotkey.Value.BetterIsPressed())
            {
                keyboardZoom--;
            }
            if (keyboardZoom != 0f)
            {
                _scrollTarget = 0f;
                _centering = false;
                var focus = _viewport.State.Center;
                if (peek && player != null)
                {
                    focus = MathUtils.ConvertToMapPosition(((IPlayer)player).Position);
                }
                else if (_zoomToMouse)
                {
                    RectTransformUtility.ScreenPointToLocalPointInRectangle(
                        view.RectTransform, Input.mousePosition, null, out var localPoint);
                    focus = localPoint;
                }
                _viewport.ZoomAround(_viewport.State.Zoom * (1f + keyboardZoom * _zoomSpeed * deltaTime), focus);
                _viewport.State.Center = _viewport.ClampCenter(_viewport.State.Center);
                _zoomInputActive = true;
            }
            else if (wheel != 0f)
            {
                _centering = false;
                if (_scrollTarget == 0f)
                {
                    _scrollTarget = _viewport.State.Zoom;
                }
                _scrollTarget = Mathf.Clamp(_scrollTarget * (1f + wheel * 1.75f), _viewport.ZoomMin, _viewport.ZoomMax);
                RectTransformUtility.ScreenPointToLocalPointInRectangle(view.RectTransform, Input.mousePosition, null, out var localPoint);
                _scrollFocus = localPoint;
                _zoomInputActive = true;
            }

            if (_scrollTarget != 0f)
            {
                var nextZoom = Mathf.Lerp(_viewport.State.Zoom, _scrollTarget, 8f * deltaTime);
                if (Mathf.Abs(nextZoom - _scrollTarget) < 0.0001f)
                {
                    nextZoom = _scrollTarget;
                    _scrollTarget = 0f;
                }
                _viewport.ZoomAround(nextZoom, _scrollFocus);
                _viewport.State.Center = _viewport.ClampCenter(_viewport.State.Center);
            }
            if (_centering)
            {
                _centerTime += deltaTime;
                var progress = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(_centerTime / 0.25f));
                _viewport.State = MapViewportState.Lerp(_centerFrom, _centerTo, progress);
                _viewport.SetZoom(_viewport.State.Zoom);
                if (progress >= 1f)
                {
                    _centering = false;
                    _viewport.SetZoom(_centerTo.Zoom);
                    CommitZoom();
                }
            }
            if (_zoomInputActive && keyboardZoom == 0f && _scrollTarget == 0f)
            {
                CommitZoom();
                _zoomInputActive = false;
            }
        }

        internal void SelectPlayerLevel(MapView view, Player player)
        {
            if (_autoLevel && player != null && !(_retainPosition && _hasRetainedPosition))
            {
                _viewport.State.Level = view.GetLevelAtPosition(
                    MathUtils.ConvertToMapPosition(((IPlayer)player).Position), _viewport.State.Level);
            }
        }

        private void Center(Player player, bool animate)
        {
            _centerFrom = _viewport.State;
            _centerTo = _viewport.State;
            _centerTo.Center = MathUtils.ConvertToMapPosition(((IPlayer)player).Position);
            
            if (_resetZoom)
            {
                _centerTo.Zoom = Mathf.Lerp(_viewport.ZoomMin, _viewport.ZoomMax, _resetPoint);
            }
            
            _scrollTarget = 0f;
            _centering = animate;
            _centerTime = 0f;
            
            if (!animate)
            {
                _viewport.State = _centerTo;
                _viewport.SetZoom(_centerTo.Zoom);
                CommitZoom();
            }
        }

        private void CommitZoom()
        {
            if (!GameUtils.IsInRaid())
            {
                return;
            }
            _requestedZoom = _viewport.NormalizedZoom;
            Settings.ZoomMainMap.Value = _requestedZoom;
        }
    }

    internal sealed class MainMapViewport
    {
        internal MapDef Definition { get; private set; }
        internal bool Initialized { get; private set; }
        internal MapViewportState State;
        internal float ZoomMin { get; private set; } = 1f;
        internal float ZoomMax { get; private set; } = 10f;
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

        internal void ZoomAround(float zoom, Vector2 focus)
        {
            var oldZoom = State.Zoom;
            SetZoom(zoom);
            State.Center = focus + (State.Center - focus) * (oldZoom / State.Zoom);
        }

        internal Vector2 ClampCenter(Vector2 center)
        {
            var bounds = Definition.Bounds;
            var size = MathUtils.GetRotatedRectangle(bounds.Max - bounds.Min, Definition.CoordinateRotation);
            var midpoint = MathUtils.GetRotatedVector2(MathUtils.GetMidpoint(bounds.Min, bounds.Max), Definition.CoordinateRotation);
            var rotated = MathUtils.GetRotatedVector2(center, Definition.CoordinateRotation);
            
            rotated.x = Mathf.Clamp(rotated.x, midpoint.x - size.x * 0.5f, midpoint.x + size.x * 0.5f);
            rotated.y = Mathf.Clamp(rotated.y, midpoint.y - size.y * 0.5f, midpoint.y + size.y * 0.5f);
            
            return MathUtils.GetRotatedVector2(rotated, -Definition.CoordinateRotation);
        }

        internal void ChangeLevelBy(int delta)
        {
            var levels = Definition.Layers.Values.Select(layer => layer.Level).Distinct().OrderBy(level => level).ToArray();
            var index = System.Array.IndexOf(levels, State.Level) + delta;
            
            if (index >= 0 && index < levels.Length)
            {
                State.Level = levels[index];
            }
        }
    }
}

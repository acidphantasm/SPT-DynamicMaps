using System;
using System.Collections.Generic;
using System.Linq;
using DG.Tweening;
using DynamicMaps.Data;
using DynamicMaps.Utils;
using EFT;
using UnityEngine;
using UnityEngine.UI;

namespace DynamicMaps.UI.Components
{
    public class MapView : MonoBehaviour
    {
        private static readonly Vector2 MarkerSize = new Vector2(30, 30);

        public event Action<int> OnLevelSelected;

        public RectTransform RectTransform
        {
            get => gameObject.transform as RectTransform;
        }
        public MapDef CurrentMapDef { get; private set; }
        private float CoordinateRotation { get; set; }
        private int SelectedLevel { get; set; }

        private GameObject MapMarkerContainer { get; set; }
        private GameObject MapLabelsContainer { get; set; }
        private GameObject MapLayerContainer { get; set; }

        private float _zoomCurrent = 1f;

        private readonly List<MapMarker> _markers = [];
        private readonly List<MapLayer> _layers = [];
        private readonly List<MapLabel> _labels = [];

        public static MapView Create(GameObject parent, string name)
        {
            var go = UIUtils.CreateUIGameObject(parent, name);
            go.AddComponent<Canvas>();
            go.AddComponent<GraphicRaycaster>();

            var view = go.AddComponent<MapView>();
            return view;
        }

        private void Awake()
        {
            MapLayerContainer = UIUtils.CreateUIGameObject(gameObject, "Layers");
            MapMarkerContainer = UIUtils.CreateUIGameObject(gameObject, "Markers");
            MapLabelsContainer = UIUtils.CreateUIGameObject(gameObject, "Labels");

            // for some reason these don't follow creation order in some cases
            MapLayerContainer.transform.SetAsFirstSibling();
            MapMarkerContainer.transform.SetAsLastSibling();
        }

        private void OnDestroy()
        {
            TextureUtils.ClearMapCache();
        }

        public void AddMapMarker(MapMarker marker)
        {
            if (_markers.Contains(marker))
            {
                return;
            }

            // hook marker position changed event up, so that when markers change position, they get notified
            // about layer status
            marker.OnPositionChanged += UpdateLayerBound;
            UpdateLayerBound(marker);  // call immediately;

            marker.ContainingMapView = this;

            _markers.Add(marker);
        }

        public MapMarker AddMapMarker(MapMarkerDef markerDef)
        {
            var marker = MapMarker.Create(MapMarkerContainer, markerDef, MarkerSize, -CoordinateRotation, 1f/_zoomCurrent);
            AddMapMarker(marker);
            return marker;
        }

        public TransformMapMarker AddTransformMarker(Transform followingTransform, string markerName, string category, Color color,
                                                     string imagePath, Vector2 size)
        {
            var marker = TransformMapMarker.Create(followingTransform, MapMarkerContainer, imagePath, color, markerName, category, size, -CoordinateRotation, 1f/_zoomCurrent);
            AddMapMarker(marker);
            return marker;
        }

        public PlayerMapMarker AddPlayerMarker(IPlayer player, string category, Color color, string imagePath)
        {
            var marker = PlayerMapMarker.Create(player, MapMarkerContainer, imagePath, color, category, MarkerSize, -CoordinateRotation, 1f/_zoomCurrent);
            AddMapMarker(marker);
            return marker;
        }

        public IEnumerable<MapMarker> GetMapMarkersByCategory(string category)
        {
            return _markers.Where(m => m.Category == category);
        }

        public void ChangeMarkerCategoryStatus(string category, bool status)
        {
            foreach (var marker in _markers)
            {
                if (marker.Category != category)
                {
                    continue;
                }

                marker.gameObject.SetActive(status);
            }
        }

        public void ChangeMarkerPartialCategoryStatus(string partial, bool status)
        {
            foreach (var marker in _markers)
            {
                if (!marker.Category.Contains(partial))
                {
                    continue;
                }

                marker.gameObject.SetActive(status);
            }
        }

        public void RemoveMapMarker(MapMarker marker)
        {
            if (!_markers.Contains(marker))
            {
                return;
            }

            _markers.Remove(marker);
            marker.OnPositionChanged -= UpdateLayerBound;
            DOTween.Kill(marker.transform);
            marker.gameObject.SetActive(false);  // destroy not guaranteed to be called immediately
            GameObject.Destroy(marker.gameObject);
        }

        public void AddMapLabel(MapLabelDef labelDef)
        {
            var label = MapLabel.Create(MapLabelsContainer, labelDef, -CoordinateRotation, 1f/_zoomCurrent);

            UpdateLayerBound(label);

            _labels.Add(label);
        }

        public void RemoveMapLabel(MapLabel label)
        {
            if (!_labels.Contains(label))
            {
                return;
            }

            _labels.Remove(label);
            DOTween.Kill(label.transform);
            label.gameObject.SetActive(false);  // destroy not guaranteed to be called immediately
            GameObject.Destroy(label.gameObject);
        }

        public void LoadMap(MapDef mapDef)
        {
            if (mapDef == null || CurrentMapDef == mapDef)
            {
                return;
            }

            if (CurrentMapDef != null)
            {
                UnloadMap();
            }

            CurrentMapDef = mapDef;
            CoordinateRotation = mapDef.CoordinateRotation;

            // set width and height for top level
            var size = mapDef.Bounds.Max - mapDef.Bounds.Min;
            var rotatedSize = MathUtils.GetRotatedRectangle(size, CoordinateRotation);
            RectTransform.sizeDelta = rotatedSize;

            // rotate all of the map content
            RectTransform.localRotation = Quaternion.Euler(0, 0, CoordinateRotation);

            _zoomCurrent = 1f;
            RectTransform.localScale = Vector3.one;
            RectTransform.anchoredPosition = Vector2.zero;

            // load all layers in the order of level
            // BSG has extension method deconstruct for KVP, so have to do this
            foreach (var pair in mapDef.Layers.OrderBy(pair => pair.Value.Level))
            {
                var layerName = pair.Key;
                var layerDef = pair.Value;
                var layer = MapLayer.Create(MapLayerContainer, layerName, layerDef, -CoordinateRotation);
                layer.IsOnDefaultLevel = layerDef.Level == mapDef.DefaultLevel;

                _layers.Add(layer);
            }

            // select layer by the default level
            SelectTopLevel(mapDef.DefaultLevel);

            // load all static map markers
            foreach (var markerDef in mapDef.StaticMarkers)
            {
                AddMapMarker(markerDef);
            }

            // load all static labels
            foreach (var labelDef in mapDef.Labels)
            {
                AddMapLabel(labelDef);
            }
        }

        public void UnloadMap()
        {
            if (CurrentMapDef == null)
            {
                return;
            }

            // remove all markers and reset to empty
            var markersCopy = _markers.ToList();
            foreach (var marker in markersCopy)
            {
                RemoveMapMarker(marker);
            }
            markersCopy.Clear();
            _markers.Clear();

            // remove all markers and reset to empty
            var labelsCopy = _labels.ToList();
            foreach (var label in labelsCopy)
            {
                RemoveMapLabel(label);
            }
            labelsCopy.Clear();
            _labels.Clear();

            // clear layers and reset to empty
            foreach (var layer in _layers)
            {
                GameObject.Destroy(layer.gameObject);
            }
            _layers.Clear();

            TextureUtils.ClearMapCache();
            _zoomCurrent = 1f;
            CurrentMapDef = null;
        }

        public void SelectTopLevel(int level)
        {
            // go through each layer and change top level
            foreach (var layer in _layers)
            {
                layer.OnTopLevelSelected(level);
            }

            SelectedLevel = level;

            UpdateLayerStatus();

            OnLevelSelected?.Invoke(level);
        }

        public void SelectLevelByCoords(Vector3 coords)
        {
            var matchingLayer = FindMatchingLayerByCoordinate(coords);
            if (matchingLayer == null)
            {
                return;
            }

            SelectTopLevel(matchingLayer.Level);
        }

        internal MapViewportState CaptureViewportState()
        {
            var zoom = Mathf.Max(RectTransform.localScale.x, 0.0001f);
            return new MapViewportState
            {
                Center = MathUtils.GetRotatedVector2(-RectTransform.anchoredPosition / zoom, -CoordinateRotation),
                Zoom = zoom,
                Level = SelectedLevel
            };
        }

        internal void ApplyViewportState(MapViewportState state)
        {
            if (CurrentMapDef == null)
            {
                return;
            }

            if (!Mathf.Approximately(_zoomCurrent, state.Zoom))
            {
                _zoomCurrent = state.Zoom;
                RectTransform.localScale = Vector3.one * state.Zoom;
                foreach (var marker in _markers)
                {
                    marker.GetRectTransform().localScale = Vector3.one / state.Zoom;
                }

                foreach (var label in _labels)
                {
                    label.GetRectTransform().localScale = Vector3.one / state.Zoom;
                }
            }
            
            RectTransform.anchoredPosition = -MathUtils.GetRotatedVector2(state.Center, CoordinateRotation) * state.Zoom;
            if (SelectedLevel != state.Level)
            {
                SelectTopLevel(state.Level);
            }
        }

        internal int GetLevelAtPosition(Vector3 position, int fallback)
        {
            return FindMatchingLayerByCoordinate(position)?.Level ?? fallback;
        }

        private MapLayer FindMatchingLayerByCoordinate(Vector3 coordinate)
        {
            // if multiple matching, use the one with the lowest bound volume
            // this might be expensive to compute with lots of layers and bounds
            return _layers.Where(l => l.IsCoordinateInLayer(coordinate))
                          .OrderBy(l => l.GetMatchingBoundVolume(coordinate))
                          .FirstOrDefault();
        }

        private void UpdateLayerBound(ILayerBound bound)
        {
            var layer = FindMatchingLayerByCoordinate(bound.Position);
            
            if (layer is null) return;
            
            bound.HandleNewLayerStatus(layer.Status);
        }

        private void UpdateLayerStatus()
        {
            var theBound = _markers.Cast<ILayerBound>().Concat(_labels);
            foreach (var bound in theBound)
            {
                UpdateLayerBound(bound);
            }
        }
    }

    internal struct MapViewportState
    {
        internal Vector2 Center;
        internal float Zoom;
        internal int Level;

        internal static MapViewportState Lerp(MapViewportState from, MapViewportState to, float progress)
        {
            return new MapViewportState
            {
                Center = Vector2.Lerp(from.Center, to.Center, progress),
                Zoom = Mathf.Lerp(from.Zoom, to.Zoom, progress),
                Level = progress < 1f ? from.Level : to.Level
            };
        }
    }

    internal enum MapDisplayMode
    {
        Hidden,
        Main,
        Peek,
        Mini
    }

}

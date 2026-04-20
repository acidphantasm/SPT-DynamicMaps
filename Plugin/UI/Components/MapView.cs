using System;
using System.Collections.Generic;
using System.Linq;
using DG.Tweening;
using DynamicMaps.Config;
using DynamicMaps.Data;
using DynamicMaps.Utils;
using EFT;
using UnityEngine;
using UnityEngine.UI;

namespace DynamicMaps.UI.Components
{
    public class MapView : MonoBehaviour
    {
        private static Vector2 _markerSize = new Vector2(30, 30);
        private static float _zoomMaxScaler = 10f;  // multiplier against zoomMin
        private static float _zoomMinScaler = 1.1f; // divider against ratio of a provided rect

        public event Action<int> OnLevelSelected;

        public RectTransform RectTransform => gameObject.transform as RectTransform;
        private RectTransform _maskTransform;
        public MapDef CurrentMapDef { get; private set; }
        public float CoordinateRotation { get; private set; }
        public int SelectedLevel { get; private set; }

        public GameObject MapMarkerContainer { get; private set; }
        public GameObject MapLabelsContainer { get; private set; }
        public GameObject MapLayerContainer { get; private set; }

        // Zoom holders
        public float ZoomMin { get; private set; }      // set when map loaded
        public float ZoomMax { get; private set; }      // set when map loaded
        public float ZoomMain { get; private set; }
        public float ZoomMini { get; private set; }
        public bool IsMiniMapActive { get; set; }
        private float ZoomCurrent => IsMiniMapActive ? ZoomMini : ZoomMain;
        
        public Vector2 MainMapPos { get; private set; } = Vector2.zero;
        
        private Vector2 _immediateMapAnchor = Vector2.zero;

        private List<MapMarker> _markers = new List<MapMarker>();
        private List<MapLayer> _layers = new List<MapLayer>();
        private List<MapLabel> _labels = new List<MapLabel>();

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

        private void OnEnable()
        {
            Settings.OnZoomMainMapChanged += HandleZoomMainChanged;
            Settings.OnZoomMiniMapChanged += HandleZoomMiniChanged;
        }

        private void OnDisable()
        {
            Settings.OnZoomMainMapChanged -= HandleZoomMainChanged;
            Settings.OnZoomMiniMapChanged -= HandleZoomMiniChanged;
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
            var marker = MapMarker.Create(MapMarkerContainer, markerDef, _markerSize, -CoordinateRotation, 1f/ZoomCurrent);
            AddMapMarker(marker);
            return marker;
        }

        public TransformMapMarker AddTransformMarker(Transform followingTransform, string name, string category, Color color,
                                                     string imagePath, Vector2 size)
        {
            var marker = TransformMapMarker.Create(followingTransform, MapMarkerContainer, imagePath, color, name, category,
                                                   size, -CoordinateRotation, 1f/ZoomCurrent);
            AddMapMarker(marker);
            return marker;
        }

        public PlayerMapMarker AddPlayerMarker(IPlayer player, string category, Color color, string imagePath)
        {
            var marker = PlayerMapMarker.Create(player, MapMarkerContainer, imagePath, color, category,
                                                _markerSize, -CoordinateRotation, 1f/ZoomCurrent);
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
            var label = MapLabel.Create(MapLabelsContainer, labelDef, -CoordinateRotation, 1f/ZoomCurrent);

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

        public void LoadMap(MapDef mapDef, RectTransform maskTransform)
        {
            _maskTransform = maskTransform;
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

            // set min/max zoom based on parent's rect transform
            SetMinMaxZoom(maskTransform);

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

            _immediateMapAnchor = Vector2.zero;
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

        private float NormalizedToActual(float normalized)
        {
            return Mathf.Lerp(ZoomMin, ZoomMax, normalized);
        }

        private float ActualToNormalized(float actual)
        {
            return Mathf.InverseLerp(ZoomMin, ZoomMax, actual);
        }
        
        public void ClampToMapBounds()
        {
            if (CurrentMapDef == null || _maskTransform == null) return;

            var mapSize = CurrentMapDef.Bounds.Max - CurrentMapDef.Bounds.Min;
            var rotatedSize = MathUtils.GetRotatedRectangle(mapSize, CoordinateRotation);
            var scaledMapSize = rotatedSize * ZoomCurrent;
            var maskSize = _maskTransform.rect.size;

            var boundsCenter = MathUtils.GetMidpoint(CurrentMapDef.Bounds.Min, CurrentMapDef.Bounds.Max);
            var rotatedCenter = MathUtils.GetRotatedVector2(boundsCenter, CoordinateRotation);
            var scaledCenter = rotatedCenter * ZoomCurrent;

            var paddingX = maskSize.x * 0.5f;
            var paddingY = maskSize.y * 0.5f;

            var halfMap = scaledMapSize * 0.5f;
            var halfMask = maskSize * 0.5f;

            var sizeDiff = halfMap - halfMask;

            var rangeX = sizeDiff.x + paddingX;
            var rangeY = sizeDiff.y + paddingY;

            const float epsilon = 0.05f;

            if (Mathf.Abs(rangeX) < epsilon)
                rangeX = 0f;

            if (Mathf.Abs(rangeY) < epsilon)
                rangeY = 0f;

            rangeX = Mathf.Max(0f, rangeX);
            rangeY = Mathf.Max(0f, rangeY);

            var maxX = rangeX;
            var maxY = rangeY;

            var clamped = new Vector2(
                Mathf.Clamp(RectTransform.anchoredPosition.x, -scaledCenter.x - maxX, -scaledCenter.x + maxX),
                Mathf.Clamp(RectTransform.anchoredPosition.y, -scaledCenter.y - maxY, -scaledCenter.y + maxY));

            if (clamped != RectTransform.anchoredPosition)
            {
                RectTransform.anchoredPosition = clamped;
                _immediateMapAnchor = clamped;
                MainMapPos = clamped;
            }
        }
        
        public void SetMinMaxZoom(RectTransform parentTransform)
        {
            // set zoom min and max based on size of map and size of mask
            var mapSize = RectTransform.sizeDelta;
            ZoomMin = Mathf.Min(parentTransform.sizeDelta.x / mapSize.x, parentTransform.sizeDelta.y / mapSize.y) / _zoomMinScaler;
            ZoomMax = _zoomMaxScaler * ZoomMin;

            // this will set everything up for initial zoom - mini comes from the settings on load, main needs to be minimum so the user can change it
            ZoomMain = ZoomMin;
            ZoomMini = NormalizedToActual(Settings.ZoomMiniMap.Value);
            
            ApplyZoom(ZoomCurrent, 0f);

            // shift map to center it
            // FIXME: this doesn't center in the parent
            RectTransform.anchoredPosition = Vector2.zero;

            var midpoint = MathUtils.GetMidpoint(CurrentMapDef.Bounds.Min, CurrentMapDef.Bounds.Max);
            ShiftMapToCoordinate(midpoint, 0, false);
        }

        public void SetMapZoom(float zoomNew, float tweenTime, bool updateMainZoom = true, bool updateMiniZoom = false)
        {
            zoomNew = Mathf.Clamp(zoomNew, ZoomMin, ZoomMax);

            if (updateMainZoom)
            {
                if (Mathf.Approximately(zoomNew, ZoomMain)) 
                    return;
                
                ZoomMain = zoomNew;
                Settings.ZoomMainMap.Value = ActualToNormalized(zoomNew);
            }

            if (updateMiniZoom)
            {
                if (Mathf.Approximately(zoomNew, ZoomMini)) 
                    return;
                
                ZoomMini = zoomNew;
                Settings.ZoomMiniMap.Value = ActualToNormalized(zoomNew);
            }

            ApplyZoom(ZoomCurrent, tweenTime);
        }
        
        public void ApplyMainMapZoom()
        {
            ApplyZoom(ZoomMain, 0f);
        }
        
        public void ApplyMiniMapZoom()
        {
            ApplyZoom(ZoomMini, 0f);
        }
        
        private void ApplyZoom(float zoom, float tweenTime)
        {
            zoom = Mathf.Clamp(zoom, ZoomMin, ZoomMax);
    
            RectTransform.DOScale(zoom * Vector3.one, tweenTime);

            foreach (var marker in _markers)
                marker.GetRectTransform().DOScale(1 / zoom * Vector3.one, tweenTime);
            foreach (var label in _labels)
                label.GetRectTransform().DOScale(1 / zoom * Vector3.one, tweenTime);
        }
        
        private void HandleZoomMainChanged(float normalized)
        {
            var actual = NormalizedToActual(normalized);
            ZoomMain = actual;

            if (!IsMiniMapActive)
            {
                ApplyZoom(actual, 0);
            }
        }

        private void HandleZoomMiniChanged(float normalized)
        {
            var actual = NormalizedToActual(normalized);
            ZoomMini = actual;
            
            if (IsMiniMapActive)
            {
                ApplyZoom(actual, 0);
            }
        }

        public void IncrementalZoomInto(float zoomDelta, Vector2 rectPoint, float zoomTweenTime)
        {
            CancelPositionTween();
            
            var zoomNew = Mathf.Clamp(ZoomMain + zoomDelta, ZoomMin, ZoomMax);
            var actualDelta = zoomNew - ZoomMain;
            var rotatedPoint = MathUtils.GetRotatedVector2(rectPoint, CoordinateRotation);

            // have to shift first, so that the tween is started in the shift first
            ShiftMap(-rotatedPoint * actualDelta, zoomTweenTime, false);
            SetMapZoom(zoomNew, zoomTweenTime);
        }
        
        public void IncrementalZoomIntoMiniMap(float zoomDelta, Vector2 rectPoint, float zoomTweenTime)
        {
            CancelPositionTween();
            
            var zoomNew = Mathf.Clamp(ZoomMini + zoomDelta, ZoomMin, ZoomMax);
            var actualDelta = zoomNew - ZoomMini;
            var rotatedPoint = MathUtils.GetRotatedVector2(rectPoint, CoordinateRotation);

            // have to shift first, so that the tween is started in the shift first
            ShiftMap(-rotatedPoint * actualDelta, zoomTweenTime, true);
            SetMapZoom(zoomNew, zoomTweenTime, false, true);
        }
        
        public void CancelPositionTween()
        {
            DOTween.Kill(RectTransform, true);
            _immediateMapAnchor = RectTransform.anchoredPosition;
        }

        public void ShiftMap(Vector2 shift, float tweenTime, bool isMini)
        {
            if (shift == Vector2.zero)
            {
                return;
            }

            // check if tweening to update _immediateMapAnchor, since the scroll rect might have moved the anchor
            if (!DOTween.IsTweening(RectTransform, true) || tweenTime == 0)
            {
                _immediateMapAnchor = RectTransform.anchoredPosition;
            }

            _immediateMapAnchor += shift;

            if (!isMini)
            {
                MainMapPos = _immediateMapAnchor;
            }
            
            RectTransform.DOAnchorPos(_immediateMapAnchor, tweenTime);
        }

        public void SetMapPos(Vector2 pos, float tweenTime)
        {
            MainMapPos = pos;
            RectTransform.DOAnchorPos(pos, tweenTime);
        }
        
        public void ShiftMapToCoordinate(Vector2 coord, float tweenTime, bool isMini)
        {
            var rotatedCoord = MathUtils.GetRotatedVector2(coord, CoordinateRotation);
            var currentCenter = RectTransform.anchoredPosition / ZoomCurrent;
            ShiftMap((-rotatedCoord - currentCenter) * ZoomCurrent, tweenTime, isMini);
        }

        public void ShiftMapToPlayer(Vector2 coord, float tweenTime, bool isMini)
        {
            var rotatedCoord = MathUtils.GetRotatedVector2(coord, CoordinateRotation);
            var currentCenter = RectTransform.anchoredPosition / ZoomMain;
            ShiftMap((-rotatedCoord - currentCenter) * ZoomMain, tweenTime, isMini);
        }

        public void ScaledShiftMap(Vector2 shiftIncrements, float incrementScale, bool isMini)
        {
            var smallestDimension = Mathf.Min(CurrentMapDef.Bounds.Max.x - CurrentMapDef.Bounds.Min.x,
                                              CurrentMapDef.Bounds.Max.y - CurrentMapDef.Bounds.Min.y);
            
            var incrementSize = smallestDimension * ZoomCurrent * incrementScale;
            ShiftMap(shiftIncrements * incrementSize, 0, isMini);
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
}

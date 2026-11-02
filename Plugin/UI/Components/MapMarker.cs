using System;
using System.Collections.Generic;
using DynamicMaps.Config;
using DynamicMaps.Data;
using DynamicMaps.Utils;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace DynamicMaps.UI.Components
{
    public class MapMarker : MonoBehaviour, ILayerBound, IPointerEnterHandler, IPointerExitHandler
    {
        // TODO: this seems... not great?
        public static Dictionary<string, Dictionary<LayerStatus, float>> CategoryImageAlphaLayerStatus { get; protected set; }
            = new Dictionary<string, Dictionary<LayerStatus, float>>
            {
                {"Extract", new Dictionary<LayerStatus, float> {
                    {LayerStatus.Hidden, 0.50f},
                    {LayerStatus.Underneath, 0.75f},
                    {LayerStatus.OnTop, 1.0f},
                    {LayerStatus.FullReveal, 1.0f},
                }},
                {"Secret", new Dictionary<LayerStatus, float> {
                    {LayerStatus.Hidden, 0.50f},
                    {LayerStatus.Underneath, 0.75f},
                    {LayerStatus.OnTop, 1.0f},
                    {LayerStatus.FullReveal, 1.0f},
                }},
                {"Transit", new Dictionary<LayerStatus, float> {
                    {LayerStatus.Hidden, 0.50f},
                    {LayerStatus.Underneath, 0.75f},
                    {LayerStatus.OnTop, 1.0f},
                    {LayerStatus.FullReveal, 1.0f},
                }},
                {"Quest", new Dictionary<LayerStatus, float> {
                    {LayerStatus.Hidden, 0.50f},
                    {LayerStatus.Underneath, 0.75f},
                    {LayerStatus.OnTop, 1.0f},
                    {LayerStatus.FullReveal, 1.0f},
                }},
                {"Airdrop", new Dictionary<LayerStatus, float> {
                    {LayerStatus.Hidden, 0.50f},
                    {LayerStatus.Underneath, 0.75f},
                    {LayerStatus.OnTop, 1.0f},
                    {LayerStatus.FullReveal, 1.0f},
                }},
            };
        public static Dictionary<string, Dictionary<LayerStatus, float>> CategoryLabelAlphaLayerStatus { get; protected set; }
            = new Dictionary<string, Dictionary<LayerStatus, float>>
            {
                {"Extract", new Dictionary<LayerStatus, float> {
                    {LayerStatus.Hidden, 0.0f},
                    {LayerStatus.Underneath, 0.0f},
                    {LayerStatus.OnTop, 1.0f},
                    {LayerStatus.FullReveal, 1.0f},
                }},
                {"Secret", new Dictionary<LayerStatus, float> {
                    {LayerStatus.Hidden, 0.0f},
                    {LayerStatus.Underneath, 0.0f},
                    {LayerStatus.OnTop, 1.0f},
                    {LayerStatus.FullReveal, 1.0f},
                }},
                {"Transit", new Dictionary<LayerStatus, float> {
                    {LayerStatus.Hidden, 0.0f},
                    {LayerStatus.Underneath, 0.0f},
                    {LayerStatus.OnTop, 1.0f},
                    {LayerStatus.FullReveal, 1.0f},
                }},
                {"Quest", new Dictionary<LayerStatus, float> {
                    {LayerStatus.Hidden, 0.0f},
                    {LayerStatus.Underneath, 0.0f},
                    {LayerStatus.OnTop, 1.0f},
                    {LayerStatus.FullReveal, 1.0f},
                }},
            };

        private static readonly Vector2 LabelSizeMultiplier = new Vector2(2.5f, 2f);
        private static readonly float MarkerMinFontSize = 9f;
        private static readonly float MarkerMaxFontSize = 13f;

        public event Action<ILayerBound> OnPositionChanged;

        private string Text { get; set; }
        public string Category { get; private set; }
        public MapView ContainingMapView { get; set; }

        private Image Image { get; set; }
        private TextMeshProUGUI Label { get; set; }
        private RectTransform RectTransform
        {
            get => gameObject.transform as RectTransform;
        }

        public string AssociatedItemId { get; private set; } = "";
        public bool IsDynamic { get; protected set; }
        private bool ShowInRaid { get; set; } = true;

        private Vector3 _position;
        public Vector3 Position
        {
            get => _position;
            set => Move(value);
        }

        private float _rotation;
        public float Rotation
        {
            get => _rotation;
            set => SetRotation(value);
        }

        private Color _color = Color.white;
        public Color Color
        {
            get => _color;

            set
            {
                _color = value;
                Image.color = value;
                Label.color = value;
            }
        }

        private Vector2 _size = new Vector2(30f, 30f);
        public Vector2 Size
        {
            get
            {
                return _size;
            }

            set
            {
                _size = value;
                RectTransform.sizeDelta = _size;
                Image.GetRectTransform().sizeDelta = _size;
                Label.GetRectTransform().sizeDelta = _size * LabelSizeMultiplier;
            }
        }

        protected Dictionary<LayerStatus, float> ImageAlphaLayerStatus { get; set; } = new()
        {
            {LayerStatus.Hidden, 0.0f},
            {LayerStatus.Underneath, 0.0f},
            {LayerStatus.OnTop, 1f},
            {LayerStatus.FullReveal, 1f},
        };
        
        protected Dictionary<LayerStatus, float> LabelAlphaLayerStatus { get; set; } = new()
        {
            {LayerStatus.Hidden, 0.0f},
            {LayerStatus.Underneath, 0.0f},
            {LayerStatus.OnTop, 0.0f},
            {LayerStatus.FullReveal, 1f},
        };

        private string _imagePath;
        private float _initialRotation;
        private bool _hasSetOutline;
        private bool _isInFullReveal;

        public static MapMarker Create(GameObject parent, MapMarkerDef def, Vector2 size, float degreesRotation, float scale)
        {
            var mapMarker = Create<MapMarker>(parent, def.Text, def.Category, def.ImagePath, def.Color, def.Position, size,
                                              def.Pivot, degreesRotation, scale, def.ShowInRaid, def.Sprite);
            mapMarker.AssociatedItemId = def.AssociatedItemId;

            return mapMarker;
        }

        public static T Create<T>(GameObject parent, string text, string category, string imageRelativePath, Color color,
                                  Vector3 position, Vector2 size, Vector2 pivot, float degreesRotation, float scale,
                                  bool showInRaid = true, Sprite sprite = null)
                            where T : MapMarker
        {
            var go = UIUtils.CreateUIGameObject(parent, $"MapMarker {text}");

            // this is to receive mouse events
            var fakeImage = go.AddComponent<Image>();
            fakeImage.color = Color.clear;
            fakeImage.raycastTarget = true;

            var rectTransform = go.GetRectTransform();
            rectTransform.anchoredPosition = position;
            rectTransform.sizeDelta = size;
            rectTransform.localScale = scale * Vector2.one;
            rectTransform.localRotation = Quaternion.Euler(0, 0, degreesRotation);
            rectTransform.pivot = pivot;

            var marker = go.AddComponent<T>();
            marker.Text = text;
            marker.Category = category;
            marker.Position = position;
            marker._initialRotation = degreesRotation;
            marker.ShowInRaid = showInRaid;

            // image
            var imageGo = UIUtils.CreateUIGameObject(go, "image");
            imageGo.AddComponent<CanvasRenderer>();
            imageGo.GetRectTransform().sizeDelta = size;
            imageGo.GetRectTransform().pivot = new Vector2(0.5f, 0.5f);
            marker.Image = imageGo.AddComponent<Image>();
            marker.Image.raycastTarget = false;
            if (sprite is null)
            {
                marker.SetImagePath(imageRelativePath);
                Settings.MarkerPack.SettingChanged += marker.OnMarkerPackChanged;
            }
            else
            {
                marker.Image.sprite = sprite;
            }
            marker.Image.type = Image.Type.Simple;

            // var outline = imageGO.AddComponent<Outline>();
            // outline.effectColor = Color.black;
            // outline.effectDistance = Vector2.one;

            // label
            var labelGo = UIUtils.CreateUIGameObject(go, "label");
            labelGo.AddComponent<CanvasRenderer>();
            labelGo.GetRectTransform().anchorMin = new Vector2(0.5f, 0f);
            labelGo.GetRectTransform().anchorMax = new Vector2(0.5f, 0f);
            labelGo.GetRectTransform().pivot = new Vector2(0.5f, 1f);
            labelGo.GetRectTransform().sizeDelta = size * LabelSizeMultiplier;
            marker.Label = labelGo.AddComponent<TextMeshProUGUI>();
            marker.Label.alignment = TextAlignmentOptions.Top;
            marker.Label.enableWordWrapping = true;
            marker.Label.enableAutoSizing = true;
            marker.Label.fontSizeMin = MarkerMinFontSize;
            marker.Label.fontSizeMax = MarkerMaxFontSize;
            marker.Label.text = marker.Text;

            marker._hasSetOutline = UIUtils.TrySetTMPOutline(marker.Label);

            marker.Color = color;
            marker._size = size;

            marker.Label.gameObject.SetActive(false);

            return marker;
        }

        protected virtual void OnEnable()
        {
            TrySetOutlineAndResize();
        }

        protected virtual void OnDestroy()
        {
            if (Settings.MarkerPack != null)
                Settings.MarkerPack.SettingChanged -= OnMarkerPackChanged;
            
            OnPositionChanged = null;
        }

        public void SetImagePath(string path)
        {
            Image.sprite = TextureUtils.GetOrLoadCachedSprite(path);
            _imagePath = path;
        }

        private void OnMarkerPackChanged(object sender, EventArgs e)
        {
            if (Image != null && _imagePath != null)
                SetImagePath(_imagePath);
        }

        public void Move(Vector3 newPosition, bool callback = true)
        {
            RectTransform.anchoredPosition = newPosition; // vector3 to vector2 discards z
            _position = newPosition;

            if (callback)
            {
                OnPositionChanged?.Invoke(this);
            }
        }

        public void SetRotation(float degreesRotation)
        {
            _rotation = degreesRotation;
            Image.gameObject.GetRectTransform().localRotation = Quaternion.Euler(0, 0, degreesRotation - _initialRotation);
        }

        public void MoveAndRotate(Vector3 newPosition, float rotation, bool callback = true)
        {
            Move(newPosition, callback);
            SetRotation(rotation);
        }

        public void HandleNewLayerStatus(LayerStatus status)
        {
            if (!ShowInRaid && GameUtils.IsInRaid())
            {
                gameObject.SetActive(false);
                return;
            }

            if (_isInFullReveal)
            {
                status = LayerStatus.FullReveal;
            }

            var imageAlpha = ImageAlphaLayerStatus[status];
            var labelAlpha = LabelAlphaLayerStatus[status];

            if (CategoryImageAlphaLayerStatus.ContainsKey(Category))
            {
                imageAlpha = CategoryImageAlphaLayerStatus[Category][status];
            }
            if (CategoryLabelAlphaLayerStatus.ContainsKey(Category))
            {
                labelAlpha = CategoryLabelAlphaLayerStatus[Category][status];
            }

            Image.color = new Color(Image.color.r, Image.color.g, Image.color.b, imageAlpha);
            Label.color = new Color(Label.color.r, Label.color.g, Label.color.b, labelAlpha);

            Image.gameObject.SetActive(imageAlpha > 0f);
            Label.gameObject.SetActive(labelAlpha > 0f);
            gameObject.SetActive(labelAlpha > 0f || imageAlpha > 0f);
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            TrySetOutlineAndResize();

            _isInFullReveal = true;
            transform.SetAsLastSibling();
            HandleNewLayerStatus(LayerStatus.FullReveal);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            _isInFullReveal = false;
            OnPositionChanged?.Invoke(this);
        }

        private void TrySetOutlineAndResize()
        {
            if (_hasSetOutline || Label == null)
            {
                return;
            }

            // try resetting text, since it seems like if outline fails, it doesn't size properly
            Label.enableAutoSizing = true;
            Label.enableWordWrapping = true;
            Label.fontSizeMin = MarkerMinFontSize;
            Label.fontSizeMax = MarkerMaxFontSize;
            Label.alignment = TextAlignmentOptions.Top;
            Label.text = $"{Label.text}";

            _hasSetOutline = UIUtils.TrySetTMPOutline(Label);
        }
    }
}

using System;
using System.Collections.Generic;
using DynamicMaps.API;
using DynamicMaps.Utils;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DynamicMaps.UI.Components
{
    internal class MarkerContextMenu : MonoBehaviour
    {
        private const float EntryHeight = 26f;
        private const float MinWidth = 200f;
        private const float MaxWidth = 360f;
        private const float FontSize = 14f;
        private const float HeaderFontSize = 12f;

        private static readonly Color BackgroundColor = new(0.07f, 0.08f, 0.08f, 0.96f);
        private static readonly Color HeaderColor = new(0.6f, 0.6f, 0.6f, 1f);
        private static readonly Color EntryTextColor = new(0.9f, 0.9f, 0.86f, 1f);
        private static readonly Color EntryNormalColor = new(1f, 1f, 1f, 0f);
        private static readonly Color EntryHighlightColor = new(1f, 1f, 1f, 0.15f);

        private static MarkerContextMenu _instance;

        private readonly List<GameObject> _entries = [];
        private RectTransform _rectTransform;
        private Canvas _rootCanvas;
        private MapMarker _marker;
        private TMP_FontAsset _font;

        internal static void Open(MapMarker marker, Vector2 screenPosition)
        {
            var actions = MapMarkerContextMenu.GetAvailableActions(marker);
            if (actions.Count == 0)
            {
                Close();
                return;
            }

            try
            {
                OpenInternal(marker, actions, screenPosition);
            }
            catch (Exception e)
            {
                Plugin.Log.LogError($"Failed to open marker context menu: {e}");
            }
        }

        private static void OpenInternal(MapMarker marker, List<MapMarkerContextAction> actions, Vector2 screenPosition)
        {
            var parentCanvas = marker.GetComponentInParent<Canvas>();
            var rootCanvas = parentCanvas != null ? parentCanvas.rootCanvas : null;
            if (rootCanvas == null)
                return;

            if (_instance == null || _instance._rootCanvas != rootCanvas)
            {
                if (_instance != null)
                    Destroy(_instance.gameObject);

                _instance = Create(rootCanvas);
            }

            // use the same font as the marker labels
            var markerLabel = marker.GetComponentInChildren<TextMeshProUGUI>(true);
            if (markerLabel != null)
                _instance._font = markerLabel.font;

            _instance.Show(marker, actions, screenPosition);
        }

        internal static void Close()
        {
            if (_instance == null)
                return;

            _instance._marker = null;
            _instance.gameObject.SetActive(false);
        }

        private static MarkerContextMenu Create(Canvas rootCanvas)
        {
            var go = UIUtils.CreateUIGameObject(rootCanvas.gameObject, "DynamicMaps MarkerContextMenu");

            // own canvas so the menu draws on top of everything
            var canvas = go.AddComponent<Canvas>();
            canvas.overrideSorting = true;
            canvas.sortingOrder = 30000;
            go.AddComponent<GraphicRaycaster>();

            var background = go.AddComponent<Image>();
            background.color = BackgroundColor;
            background.raycastTarget = true;

            var layout = go.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(4, 4, 4, 4);
            layout.spacing = 1f;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            var fitter = go.AddComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var menu = go.AddComponent<MarkerContextMenu>();
            menu._rectTransform = go.GetRectTransform();
            menu._rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            menu._rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            menu._rootCanvas = rootCanvas;
            go.SetActive(false);

            return menu;
        }

        private Camera EventCamera =>
            _rootCanvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : _rootCanvas.worldCamera;

        private void Show(MapMarker marker, List<MapMarkerContextAction> actions, Vector2 screenPosition)
        {
            _marker = marker;

            foreach (var entry in _entries)
                Destroy(entry);
            _entries.Clear();

            if (!string.IsNullOrWhiteSpace(marker.Text))
                _entries.Add(CreateHeader(marker.Text));

            foreach (var action in actions)
            {
                string label;
                try
                {
                    label = action.GetLabel(marker);
                }
                catch (Exception e)
                {
                    Plugin.Log.LogError($"Context menu action '{action.Id}' failed to get its label: {e}");
                    continue;
                }

                _entries.Add(CreateEntry(label, action));
            }

            // flip around the cursor near screen edges
            _rectTransform.pivot = new Vector2(
                screenPosition.x > Screen.width * 0.75f ? 1f : 0f,
                screenPosition.y < Screen.height * 0.25f ? 0f : 1f);

            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                _rootCanvas.GetRectTransform(), screenPosition, EventCamera, out var localPosition);
            _rectTransform.anchoredPosition = localPosition;
            _rectTransform.localScale = Vector3.one;

            transform.SetAsLastSibling();
            gameObject.SetActive(true);
            LayoutRebuilder.ForceRebuildLayoutImmediate(_rectTransform);
        }

        private void Update()
        {
            if (_marker == null || !_marker.isActiveAndEnabled)
            {
                Close();
                return;
            }

            if (Input.GetKeyDown(KeyCode.Escape))
            {
                Close();
                return;
            }

            if ((Input.GetMouseButtonDown(0) || Input.GetMouseButtonDown(1) || Input.GetMouseButtonDown(2))
                && !RectTransformUtility.RectangleContainsScreenPoint(_rectTransform, Input.mousePosition, EventCamera))
            {
                Close();
            }
        }

        private void OnDestroy()
        {
            if (_instance == this)
                _instance = null;
        }

        private GameObject CreateHeader(string text)
        {
            var go = UIUtils.CreateUIGameObject(gameObject, "Header");
            var layoutElement = go.AddComponent<LayoutElement>();
            layoutElement.minWidth = MinWidth;
            layoutElement.minHeight = EntryHeight - 4f;

            var label = CreateLabel(go, text, HeaderFontSize, HeaderColor);
            label.fontStyle = FontStyles.Italic;

            return go;
        }

        private GameObject CreateEntry(string text, MapMarkerContextAction action)
        {
            var go = UIUtils.CreateUIGameObject(gameObject, $"Entry {action.Id}");
            var layoutElement = go.AddComponent<LayoutElement>();
            layoutElement.minWidth = MinWidth;
            layoutElement.minHeight = EntryHeight;

            var image = go.AddComponent<Image>();
            image.color = Color.white;
            image.raycastTarget = true;

            var button = go.AddComponent<Button>();
            button.targetGraphic = image;
            var colors = button.colors;
            colors.normalColor = EntryNormalColor;
            colors.highlightedColor = EntryHighlightColor;
            colors.pressedColor = EntryHighlightColor;
            colors.selectedColor = EntryNormalColor;
            colors.fadeDuration = 0.05f;
            button.colors = colors;

            var marker = _marker;
            button.onClick.AddListener(() =>
            {
                Close();

                try
                {
                    action.Execute(marker);
                }
                catch (Exception e)
                {
                    Plugin.Log.LogError($"Context menu action '{action.Id}' failed: {e}");
                }
            });

            CreateLabel(go, text, FontSize, EntryTextColor);

            return go;
        }

        private TextMeshProUGUI CreateLabel(GameObject parent, string text, float fontSize, Color color)
        {
            var go = UIUtils.CreateUIGameObject(parent, "Label");
            var rectTransform = go.GetRectTransform();
            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.offsetMin = new Vector2(8f, 0f);
            rectTransform.offsetMax = new Vector2(-8f, 0f);

            var label = go.AddComponent<TextMeshProUGUI>();
            if (_font != null)
                label.font = _font;
            label.text = text;
            label.fontSize = fontSize;
            label.color = color;
            label.alignment = TextAlignmentOptions.MidlineLeft;
            label.enableWordWrapping = false;
            label.overflowMode = TextOverflowModes.Ellipsis;
            label.raycastTarget = false;

            var layoutElement = parent.GetComponent<LayoutElement>();
            if (layoutElement != null)
            {
                var textWidth = label.GetPreferredValues(text).x;
                layoutElement.preferredWidth = Mathf.Clamp(textWidth + 24f, MinWidth, MaxWidth);
            }

            return label;
        }
    }
}

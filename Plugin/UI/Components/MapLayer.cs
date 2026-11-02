using System;
using System.IO;
using DynamicMaps.Data;
using DynamicMaps.Utils;
using Unity.VectorGraphics;
using UnityEngine;
using UnityEngine.UI;

namespace DynamicMaps.UI.Components
{
    public enum LayerStatus
    {
        Hidden,
        Underneath,
        OnTop,
        FullReveal
    }

    public interface ILayerBound
    {
        Vector3 Position { get; set; }
        void HandleNewLayerStatus(LayerStatus status);
    }

    public class MapLayer : MonoBehaviour
    {
        private static readonly float FadeMultiplierPerLayer = 0.5f;
        private static readonly float DefaultLevelFallbackAlpha = 0.1f;

        private string Name { get; set; }
        private MaskableGraphic Image { get; set; }
        public RectTransform RectTransform => gameObject.transform as RectTransform;

        public int Level => _def.Level;
        public LayerStatus Status { get; private set; }
        public bool IsOnDefaultLevel { get; set; }

        private MapLayerDef _def = new MapLayerDef();

        public static MapLayer Create(GameObject parent, string name, MapLayerDef def, float degreesRotation)
        {
            var sprite = TextureUtils.GetOrLoadMapSprite(def);
            if (sprite == null)
            {
                throw new InvalidDataException($"Unable to load map image: {def.ImagePath}");
            }

            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer));
            go.layer = parent.layer;
            go.transform.SetParent(parent.transform);
            go.ResetRectTransform();

            var rectTransform = go.GetRectTransform();
            var layer = go.AddComponent<MapLayer>();

            // set layer size
            var size = def.ImageBounds.Max - def.ImageBounds.Min;
            var rotatedSize = MathUtils.GetRotatedRectangle(size, degreesRotation);
            rectTransform.sizeDelta = rotatedSize;

            // set layer offset
            var offset = MathUtils.GetMidpoint(def.ImageBounds.Min, def.ImageBounds.Max);
            rectTransform.anchoredPosition = offset;

            // set rotation to combat when we rotate the whole map content
            rectTransform.localRotation = Quaternion.Euler(0, 0, degreesRotation);

            layer.Name = name;
            layer._def = def;

            // load image
            if (Path.GetExtension(def.ImagePath).Equals(".svg", StringComparison.OrdinalIgnoreCase))
            {
                var image = go.AddComponent<SVGImage>();
                image.preserveAspect = false;
                image.sprite = sprite;
                layer.Image = image;
            }
            else
            {
                var image = go.AddComponent<Image>();
                image.preserveAspect = false;
                image.sprite = sprite;
                layer.Image = image;
            }

            layer.Image.raycastTarget = false;

            return layer;
        }

        public bool IsCoordinateInLayer(Vector3 coordinate)
        {
            foreach (var gameBound in _def.GameBounds)
            {
                if (coordinate.x > gameBound.Min.x && coordinate.x < gameBound.Max.x
                 && coordinate.y > gameBound.Min.y && coordinate.y < gameBound.Max.y
                 && coordinate.z > gameBound.Min.z && coordinate.z < gameBound.Max.z)
                {
                    return true;
                }
            }

            return false;
        }

        public float GetMatchingBoundVolume(Vector3 coordinate)
        {
            // a bit scuffed formatting
            // this assumes that a layer wouldn't have multiple overlapping game bounds
            foreach (var gameBound in _def.GameBounds)
            {
                if (coordinate.x > gameBound.Min.x && coordinate.x < gameBound.Max.x
                 && coordinate.y > gameBound.Min.y && coordinate.y < gameBound.Max.y
                 && coordinate.z > gameBound.Min.z && coordinate.z < gameBound.Max.z)
                {
                    return (gameBound.Max.x - gameBound.Min.x) *
                           (gameBound.Max.y - gameBound.Min.y) *
                           (gameBound.Max.z - gameBound.Min.z);
                }
            }

            return float.MinValue;
        }

        public void OnTopLevelSelected(int newLevel)
        {
            Status = LayerStatus.Hidden;
            if (Level == newLevel)
            {
                Status = LayerStatus.OnTop;
            }
            else if (Level < newLevel)
            {
                Status = LayerStatus.Underneath;
            }

            var isActive = true;
            var levelDelta = newLevel - Level;
            var c = Mathf.Clamp01(Mathf.Pow(FadeMultiplierPerLayer, levelDelta));
            var a = 1f;

            if (Status == LayerStatus.Hidden)
            {
                isActive = false;

                if (IsOnDefaultLevel)
                {
                    a = DefaultLevelFallbackAlpha;
                    isActive = true;
                }
            }

            gameObject.SetActive(isActive);
            Image.color = new Color(c, c, c, a);
        }
    }
}

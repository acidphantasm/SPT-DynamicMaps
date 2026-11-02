using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using UnityEngine;

namespace DynamicMaps.Data
{
    public class BoundingRectangle
    {
        public Vector2 Min { get; set; }
        public Vector2 Max { get; set; }
    }

    public class BoundingRectangularSolid
    {
        public Vector3 Min { get; set; }
        public Vector3 Max { get; set; }
    }

    public class MapLayerDef
    {
        [JsonRequired]
        public int Level { get; set; }

        [JsonRequired]
        public string ImagePath { get; set; }
        
        public int TesselationIndex { get; set; }

        [JsonRequired]
        public BoundingRectangle ImageBounds { get; set; }

        [JsonRequired]
        public List<BoundingRectangularSolid> GameBounds { get; set; }
    }

    public class MapMarkerDef
    {
        public string Text { get; set; } = "";

        [JsonRequired]
        public string ImagePath { get; set; }

        [JsonRequired]
        public Vector3 Position { get; set; }

        public Sprite Sprite { get; set; }
        public bool ShowInRaid { get; set; } = true;
        public string Category { get; set; } = "None";
        public Color Color { get; set; } = Color.white;
        public Vector2 Pivot { get; set; } = new Vector2(0.5f, 0.5f);
        public string AssociatedItemId { get; set; } = "";
    }

    public class MapLabelDef
    {
        [JsonRequired]
        public string Text { get; set; }

        [JsonRequired]
        public Vector3 Position { get; set; }

        public float FontSize { get; set; } = 14f;
        public float DegreesRotation { get; set; } = 0f;
        public Color Color { get; set; } = Color.white;
        public string Category { get; set; } = "None";
    }

    public class MapDef
    {
        [JsonRequired]
        public string DisplayName { get; set; }

        [JsonRequired]
        public BoundingRectangle Bounds { get; set; }

        [JsonRequired]
        public Dictionary<string, MapLayerDef> Layers { get; set; } = new Dictionary<string, MapLayerDef>();

        public List<string> MapInternalNames { get; set; } = new List<string>();
        public float CoordinateRotation { get; set; } = 0;
        public List<MapLabelDef> Labels { get; set; } = new List<MapLabelDef>();
        public List<MapMarkerDef> StaticMarkers { get; set; } = new List<MapMarkerDef>();
        public int DefaultLevel { get; set; } = 0;

        [JsonIgnore]
        public string SelectionId { get; set; }

        [JsonIgnore]
        public bool IsAddon { get; set; }

        public string Author { get; set; }
        public string AuthorLink { get; set; }

        public static MapDef LoadFromPath(string absolutePath, string addonName = null)
        {
            try
            {
                var mapDef = JsonConvert.DeserializeObject<MapDef>(File.ReadAllText(absolutePath));
                if (mapDef?.Bounds == null || mapDef.Layers == null || mapDef.Layers.Count == 0 || mapDef.MapInternalNames == null || string.IsNullOrWhiteSpace(mapDef.DisplayName))
                {
                    throw new InvalidDataException("Incomplete map definition");
                }

                mapDef.SelectionId = Path.GetRelativePath(Plugin.Path, absolutePath).Replace('\\', '/');
                mapDef.IsAddon = addonName != null;
                if (mapDef.IsAddon)
                {
                    mapDef.DisplayName = $"{mapDef.DisplayName} ({addonName})";
                }

                foreach (var layer in mapDef.Layers.Values)
                {
                    if (layer == null || string.IsNullOrWhiteSpace(layer.ImagePath) || layer.ImageBounds == null || layer.GameBounds == null)
                    {
                        throw new InvalidDataException("Incomplete map layer");
                    }

                    layer.ImagePath = Path.GetFullPath(Path.Combine(mapDef.IsAddon ? Path.GetDirectoryName(absolutePath) : Plugin.Path, layer.ImagePath));
                    var extension = Path.GetExtension(layer.ImagePath);
                    if (!extension.Equals(".svg", StringComparison.OrdinalIgnoreCase) && !extension.Equals(".png", StringComparison.OrdinalIgnoreCase) || !File.Exists(layer.ImagePath))
                    {
                        throw new InvalidDataException($"Missing or unsupported map image: {layer.ImagePath}");
                    }
                }

                return mapDef;
            }
            catch (Exception e)
            {
                Plugin.Log.LogError($"Loading MapMappingDef failed from json at path: {absolutePath}");
                Plugin.Log.LogError($"Exception given was: {e.Message}");
                Plugin.Log.LogError($"{e.StackTrace}");
            }

            return null;
        }
    }
}

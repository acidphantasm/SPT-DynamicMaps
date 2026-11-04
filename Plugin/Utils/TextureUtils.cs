using System;
using System.Collections.Generic;
using DynamicMaps.Config;
using DynamicMaps.Data;
using System.IO;
using UnityEngine;

namespace DynamicMaps.Utils
{
    public static class TextureUtils
    {
        private static readonly Dictionary<string, Sprite> SpriteCache = new(StringComparer.OrdinalIgnoreCase);
        private static readonly Dictionary<string, Sprite> MapSpriteCache = new(StringComparer.OrdinalIgnoreCase);
        private static readonly Dictionary<string, (Texture2D Texture, Rect ImageRect)> MapCache = new(StringComparer.OrdinalIgnoreCase);

        public static Sprite GetOrLoadMapSprite(MapLayerDef def)
        {
            var extension = Path.GetExtension(def.ImagePath);
            if (extension.Equals(".svg", StringComparison.OrdinalIgnoreCase))
            {
                return SvgUtils.GetOrLoadCachedSprite(def);
            }
            
            if (!extension.Equals(".png", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidDataException($"Unsupported map image: {def.ImagePath}");
            }

            var absolutePath = Path.GetFullPath(Path.Combine(Plugin.Path, def.ImagePath ?? throw new InvalidOperationException()));
            if (MapSpriteCache.TryGetValue(absolutePath, out var sprite))
            {
                return sprite;
            }

            var texture = LoadTexture2DFromPath(absolutePath, out var imageRect);
            if (texture == null)
            {
                throw new InvalidDataException($"Unable to load map image: {absolutePath}");
            }

            sprite = Sprite.Create(texture, imageRect, new Vector2(0.5f, 0.5f), 1f);
            MapSpriteCache[absolutePath] = sprite;
            return sprite;
        }

        public static void ClearMapCache()
        {
            foreach (var sprite in MapSpriteCache.Values)
            {
                UnityEngine.Object.Destroy(sprite);
            }
            
            MapSpriteCache.Clear();

            foreach (var cached in MapCache.Values)
            {
                UnityEngine.Object.Destroy(cached.Texture);
            }
            MapCache.Clear();
        }

        private static Texture2D LoadTexture2DFromPath(string absolutePath, out Rect imageRect)
        {
            imageRect = default;
            if (string.IsNullOrEmpty(absolutePath) || !File.Exists(absolutePath))
            {
                return null;
            }

            if (MapCache.TryGetValue(absolutePath, out var cached))
            {
                imageRect = cached.ImageRect;
                return cached.Texture;
            }

            var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            try
            {
                if (!tex.LoadImage(File.ReadAllBytes(absolutePath)))
                {
                    UnityEngine.Object.Destroy(tex);
                    return null;
                }

                imageRect = new Rect(0, 0, tex.width, tex.height);
                var newWidth = (tex.width + 3) & ~3;
                var newHeight = (tex.height + 3) & ~3;
                if (newWidth != tex.width || newHeight != tex.height)
                {
                    var padded = new Texture2D(newWidth, newHeight, TextureFormat.RGBA32, false);
                    try
                    {
                        padded.SetPixels(0, 0, tex.width, tex.height, tex.GetPixels());
                        padded.Apply();
                    }
                    catch
                    {
                        UnityEngine.Object.Destroy(padded);
                        throw;
                    }
                    UnityEngine.Object.Destroy(tex);
                    tex = padded;
                }

                tex.Compress(true);
                tex.Apply(false, true);
                MapCache[absolutePath] = (tex, imageRect);
                return tex;
            }
            catch
            {
                UnityEngine.Object.Destroy(tex);
                throw;
            }
        }

        private static Texture2D LoadTexture2DFromPath(string absolutePath)
        {
            if (string.IsNullOrEmpty(absolutePath) || !File.Exists(absolutePath))
            {
                return null;
            }

            var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            tex.LoadImage(File.ReadAllBytes(absolutePath));
            return tex;
        }

        public static Sprite GetOrLoadCachedSprite(string path)
        {
            var fileName = Path.GetFileName(path);
            if (!Path.GetExtension(fileName).Equals(".png", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidDataException($"Marker images must be PNG files: {path}");
            }

            var defaultPath = Path.Combine(Plugin.Path, "Markers", fileName);
            var pack = Settings.MarkerPack.Value;
            var cachePath = pack == "Default" ? defaultPath : Path.Combine(Plugin.Path, "Addons", pack, "Markers", fileName);
            if (SpriteCache.TryGetValue(cachePath, out var sprite))
            {
                return sprite;
            }

            var imagePath = File.Exists(cachePath) ? cachePath : defaultPath;
            if (SpriteCache.TryGetValue(imagePath, out sprite))
            {
                return SpriteCache[cachePath] = sprite;
            }

            var texture = LoadTexture2DFromPath(imagePath);
            if (texture == null)
            {
                throw new InvalidDataException($"Unable to load marker image: {imagePath}");
            }

            sprite = Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height), new Vector2(texture.width / 2, texture.height / 2));
            SpriteCache[imagePath] = sprite;
            SpriteCache[cachePath] = sprite;
            return sprite;
        }
    }
}

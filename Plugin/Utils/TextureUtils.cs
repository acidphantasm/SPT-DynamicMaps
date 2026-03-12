using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace DynamicMaps.Utils
{
    public static class TextureUtils
    {
        private static Dictionary<string, Sprite> _spriteCache = new();
        private static Dictionary<string, Texture2D> _mapCache = new();

        public static Texture2D LoadTexture2DFromPath(string absolutePath)
        {
            if (string.IsNullOrEmpty(absolutePath) || !File.Exists(absolutePath))
                return null;

            if (_mapCache.TryGetValue(absolutePath, out var cachedTex))
                return cachedTex;

            var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            tex.LoadImage(File.ReadAllBytes(absolutePath));

            var newWidth = (tex.width + 3) & ~3;
            var newHeight = (tex.height + 3) & ~3;

            if (newWidth != tex.width || newHeight != tex.height)
            {
                var padded = new Texture2D(newWidth, newHeight, TextureFormat.RGBA32, false);

                padded.SetPixels(0, 0, tex.width, tex.height, tex.GetPixels());
                padded.Apply();

                Object.Destroy(tex);
                tex = padded;
            }

            tex.Compress(true);
            tex.Apply(false, true);

            _mapCache[absolutePath] = tex;
            return tex;
        }

        public static Sprite GetOrLoadCachedSprite(string path)
        {
            if (_spriteCache.TryGetValue(path, out var sprite))
                return sprite;

            var absolutePath = Path.Combine(Plugin.Path, path);
            var texture = LoadTexture2DFromPath(absolutePath);
            _spriteCache[path] = Sprite.Create(texture,
                                               new Rect(0f, 0f, texture.width, texture.height),
                                               new Vector2(texture.width / 2, texture.height / 2));

            return _spriteCache[path];
        }
    }
}

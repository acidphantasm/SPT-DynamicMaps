using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace DynamicMaps.Utils
{
    public static class TextureUtils
    {
        private static Dictionary<string, Sprite> _spriteCache = new Dictionary<string, Sprite>();

        public static Texture2D LoadTexture2DFromPath(string absolutePath)
        {
            if (!File.Exists(absolutePath))
            {
                return null;
            }

            var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            tex.LoadImage(File.ReadAllBytes(absolutePath));

            int orgWidth = tex.width;
            int orgHeight = tex.height;

            int expectedFourMultipleWidth = (orgWidth + 3) & ~3;
            int expectedFourMultipleHeight = (orgHeight + 3) & ~3;

            Texture2D texToCompress = tex;

            if (expectedFourMultipleWidth != orgWidth || expectedFourMultipleHeight != orgHeight)
            {
                var padded = new Texture2D(expectedFourMultipleWidth, expectedFourMultipleHeight, TextureFormat.RGBA32, mipChain: false, linear: false);

                var clear = new Color32[expectedFourMultipleWidth * expectedFourMultipleHeight];
                for (int i = 0; i < clear.Length; i++) clear[i] = new Color32(0, 0, 0, 0);
                padded.SetPixels32(clear);

                var pixels = tex.GetPixels32();
                padded.SetPixels32(0, 0, orgWidth, orgHeight, pixels);
                padded.Apply(false, false);

                Object.Destroy(tex);
                texToCompress = padded;
            }

            texToCompress.Compress(true);
            texToCompress.Apply(false, true);

            return texToCompress;
        }

        public static Sprite GetOrLoadCachedSprite(string path)
        {
            if (_spriteCache.ContainsKey(path))
            {
                return _spriteCache[path];
            }

            var absolutePath = Path.Combine(Plugin.Path, path);
            var texture = LoadTexture2DFromPath(absolutePath);
            _spriteCache[path] = Sprite.Create(texture,
                                               new Rect(0f, 0f, texture.width, texture.height),
                                               new Vector2(texture.width / 2, texture.height / 2));

            return _spriteCache[path];
        }
    }
}

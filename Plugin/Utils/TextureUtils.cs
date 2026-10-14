using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace DynamicMaps.Utils
{
    public static class TextureUtils
    {
        private static Dictionary<string, Sprite> _spriteCache = new();

        private static Texture2D LoadTexture2DFromPath(string absolutePath)
        {
            if (string.IsNullOrEmpty(absolutePath) || !File.Exists(absolutePath))
                return null;

            var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            tex.LoadImage(File.ReadAllBytes(absolutePath));
            tex.name = Path.GetFileNameWithoutExtension(absolutePath);

            // under il2cpp Unity can't see the managed cache holding this, so without the flag
            // Resources.UnloadUnusedAssets (run on every scene change) frees it and icons turn into squares
            tex.hideFlags |= HideFlags.DontUnloadUnusedAsset;
            return tex;
        }

        public static Sprite GetOrLoadCachedSprite(string path)
        {
            // Unity's == also catches sprites that were destroyed behind our back
            if (_spriteCache.TryGetValue(path, out var sprite) && sprite != null)
                return sprite;

            var absolutePath = Path.Combine(Plugin.Path, path);
            var texture = LoadTexture2DFromPath(absolutePath);
            if (texture == null)
            {
                Plugin.Logger.LogWarning($"Marker image is missing: {absolutePath}");
                return null;
            }

            sprite = Sprite.Create(texture,
                                   new Rect(0f, 0f, texture.width, texture.height),
                                   new Vector2(texture.width / 2, texture.height / 2));
            sprite.name = texture.name;
            sprite.hideFlags |= HideFlags.DontUnloadUnusedAsset;

            return _spriteCache[path] = sprite;
        }
    }
}

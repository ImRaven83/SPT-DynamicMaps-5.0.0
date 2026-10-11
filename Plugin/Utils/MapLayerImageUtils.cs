using System.Collections.Generic;
using System.IO;
using System.Linq;
using DynamicMaps.Data;
using UnityEngine;

namespace DynamicMaps.Utils;

/// <summary>
/// Unity.VectorGraphics can't be loaded into the il2cpp game, so map layers are shipped as PNGs that are
/// pre-rendered from the SVG sources (see tools/render_map_layers.py)
/// </summary>
public static class MapLayerImageUtils
{
    private static readonly Dictionary<string, Sprite> MapCache = [];

    /// <summary>
    /// Map defs still point at the .svg source, the rendered image sits next to it with a .png extension
    /// </summary>
    public static string GetRenderedImagePath(string imagePath)
    {
        return Path.ChangeExtension(imagePath, ".png");
    }

    public static Sprite GetOrLoadCachedSprite(MapLayerDef def)
    {
        var path = GetRenderedImagePath(def.ImagePath);
        if (MapCache.TryGetValue(path, out var sprite) && sprite != null)
        {
            return sprite;
        }

        var absolutePath = Path.Combine(Plugin.Path, path);
        if (!File.Exists(absolutePath))
        {
            Plugin.Logger.LogError($"Map layer image is missing: {absolutePath}");
            return null;
        }

        // mipmaps keep the map readable when zoomed out (e.g. on the mini-map)
        var texture = new Texture2D(2, 2, TextureFormat.RGBA32, true);
        if (!texture.LoadImage(File.ReadAllBytes(absolutePath), true))
        {
            Plugin.Logger.LogError($"Failed to decode map layer image: {absolutePath}");
            return null;
        }

        texture.name = Path.GetFileNameWithoutExtension(path);
        texture.wrapMode = TextureWrapMode.Clamp;
        texture.filterMode = FilterMode.Trilinear;
        texture.anisoLevel = 4;
        texture.hideFlags |= HideFlags.DontUnloadUnusedAsset;

        // block compress (layers are rendered with dimensions divisible by 4) and drop the CPU copy,
        // a 4k layer goes from ~85MB to ~21MB of VRAM with mips
        if (texture.width % 4 == 0 && texture.height % 4 == 0)
        {
            texture.Compress(false);
        }
        texture.Apply(false, true);

        sprite = Sprite.Create(texture,
                               new Rect(0f, 0f, texture.width, texture.height),
                               new Vector2(0.5f, 0.5f),
                               100f,
                               0,
                               SpriteMeshType.FullRect);
        sprite.name = texture.name;
        sprite.hideFlags |= HideFlags.DontUnloadUnusedAsset;

        Plugin.Logger.LogInfo($"Loaded map layer image {path} ({texture.width}x{texture.height}, {texture.format})");
        return MapCache[path] = sprite;
    }

    /// <summary>
    /// Destroys cached layer images that aren't used by the given layers, so only the shown map stays in memory
    /// </summary>
    public static void ReleaseAllExcept(IEnumerable<MapLayerDef> keep)
    {
        var keepPaths = new HashSet<string>(keep.Select(def => GetRenderedImagePath(def.ImagePath)));
        foreach (var path in MapCache.Keys.ToList())
        {
            if (keepPaths.Contains(path))
            {
                continue;
            }

            var sprite = MapCache[path];
            MapCache.Remove(path);

            if (sprite == null)
            {
                continue;
            }

            var texture = sprite.texture;
            Object.Destroy(sprite);
            if (texture != null)
            {
                Object.Destroy(texture);
            }
        }
    }
}

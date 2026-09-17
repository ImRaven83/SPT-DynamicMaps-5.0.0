using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;
using DynamicMaps.Data;
using Svg;
using UnityEngine;

namespace DynamicMaps.Utils;

// Unity.VectorGraphics isn't part of the base game and has no IL2CPP interop build (Il2CppInterop only
// generates stubs for assemblies the live game actually ships), so it can no longer tessellate SVGs to a
// mesh. Maps are rasterized instead, in pure managed code via the Svg NuGet package (verified against the
// real package: SvgDocument.FromSvg<SvgDocument> + Draw(w, h) -> System.Drawing.Bitmap), then handed to
// Unity as a plain PNG-backed Sprite the same way TextureUtils.cs already loads textures.
public static class SvgUtils
{
    private static readonly Dictionary<(string, int), Sprite> MapCache = [];
    private static readonly Regex ViewBoxRegex = new(@"<svg[^>]*\sviewBox=""([^""]+)""", RegexOptions.Compiled);

    // Replaces the old adaptive mesh-vertex-budget tessellation levels. A rasterized texture has no vertex
    // budget, just a max dimension, so TesselationIndex (still supplied per-layer by the map jsonc configs)
    // is now read as a resolution tier instead: higher index -> lower pixel density -> smaller texture.
    private static readonly float[] PixelsPerSvgUnit = [8f, 4f, 2f, 1f];
    private const int MaxTextureDimension = 8192;

    private static Sprite LoadSvgFromPath(MapLayerDef def, string absolutePath)
    {
        var svgData = File.ReadAllText(absolutePath);

        var viewBoxRect = GetViewbox(svgData);
        if (viewBoxRect is null)
            return null;

        var qualityIndex = Mathf.Clamp(def.TesselationIndex, 0, PixelsPerSvgUnit.Length - 1);
        var pixelsPerUnit = PixelsPerSvgUnit[qualityIndex];

        var rasterWidth = viewBoxRect.Value.width * pixelsPerUnit;
        var rasterHeight = viewBoxRect.Value.height * pixelsPerUnit;
        var maxDimension = Mathf.Max(rasterWidth, rasterHeight);
        if (maxDimension > MaxTextureDimension)
        {
            var scale = MaxTextureDimension / maxDimension;
            rasterWidth *= scale;
            rasterHeight *= scale;
            pixelsPerUnit *= scale;
        }

        var svgDocument = SvgDocument.FromSvg<SvgDocument>(svgData);
        using var bitmap = svgDocument.Draw(Mathf.Max(1, Mathf.RoundToInt(rasterWidth)), Mathf.Max(1, Mathf.RoundToInt(rasterHeight)));
        using var stream = new MemoryStream();
        bitmap.Save(stream, ImageFormat.Png);

        var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
        texture.LoadImage(stream.ToArray());

        return Sprite.Create(
            texture,
            new Rect(0f, 0f, texture.width, texture.height),
            new Vector2(0.5f, 0.5f),
            pixelsPerUnit);
    }

    public static Sprite GetOrLoadCachedSprite(MapLayerDef def)
    {
        var key = (def.ImagePath, def.TesselationIndex);
        if (MapCache.TryGetValue(key, out var sprite))
            return sprite;

        var absolutePath = Path.Combine(Plugin.Path, def.ImagePath);
        return MapCache[key] = LoadSvgFromPath(def, absolutePath);
    }

    private static Rect? GetViewbox(string svgText)
    {
        var match = ViewBoxRegex.Match(svgText);
        if (!match.Success)
            return null;

        var parts = match.Groups[1].Value.Split([' ', '\t', '\r', '\n', ','], StringSplitOptions.RemoveEmptyEntries);

        if (parts.Length != 4)
            return null;

        if (!float.TryParse(parts[0], NumberStyles.Float, CultureInfo.InvariantCulture, out var x)) return null;
        if (!float.TryParse(parts[1], NumberStyles.Float, CultureInfo.InvariantCulture, out var y)) return null;
        if (!float.TryParse(parts[2], NumberStyles.Float, CultureInfo.InvariantCulture, out var w)) return null;
        if (!float.TryParse(parts[3], NumberStyles.Float, CultureInfo.InvariantCulture, out var h)) return null;

        if (w <= 0f || h <= 0f)
            return null;

        return new Rect(x, y, w, h);
    }
}

using System;
using DynamicMaps;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;
using DynamicMaps.Data;
using Unity.VectorGraphics;
using UnityEngine;

namespace DynamicMaps.Utils;
public static class SvgUtils
{
    private static readonly Dictionary<(string, int), Sprite> MapCache = [];
    private static readonly Regex ViewBoxRegex = new(@"<svg[^>]*\sviewBox=""([^""]+)""", RegexOptions.Compiled);
    private static readonly VectorUtils.TessellationOptions[] TesselationIndex =
    [
        new() { StepDistance = 1.5f,  MaxCordDeviation = 0.2f,  MaxTanAngleDeviation = 0.2f,  SamplingStepSize = 0.04f },
        new() { StepDistance = 2f,  MaxCordDeviation = 0.3f,  MaxTanAngleDeviation = 0.25f, SamplingStepSize = 0.05f },
        new() { StepDistance = 4f,  MaxCordDeviation = 0.4f,  MaxTanAngleDeviation = 0.3f,  SamplingStepSize = 0.06f },
        new() { StepDistance = 6f,  MaxCordDeviation = 0.5f,  MaxTanAngleDeviation = 0.4f,  SamplingStepSize = 0.07f },
        new() { StepDistance = 8f,  MaxCordDeviation = 0.6f,  MaxTanAngleDeviation = 0.5f,  SamplingStepSize = 0.08f },
    ];

    private static Sprite LoadSvgFromPath(MapLayerDef def, string absolutePath)
    {
        var svgData = File.ReadAllText(absolutePath);
        
        var viewBoxRect = GetViewbox(svgData);
        if (viewBoxRect is null)
            return null;

        using var reader = new StringReader(svgData);
        var sceneInfo = SVGParser.ImportSVG(
            reader,
            ViewportOptions.OnlyApplyRootViewBox,
            dpi: 0,
            pixelsPerUnit: 1f,
            windowWidth: 0,
            windowHeight: 0);

        var startIndex = Mathf.Clamp(def.TesselationIndex, 0, TesselationIndex.Length - 1);
        for (var i = startIndex; i < TesselationIndex.Length; i++)
        {
            var geometry = VectorUtils.TessellateScene(sceneInfo.Scene, TesselationIndex[i], sceneInfo.NodeOpacity);

            if (OverBudgetVertices(geometry))
            {
                Plugin.Log.LogWarning($"Preset {i} over budget for {absolutePath}, trying next.");
                continue;
            }

            var sprite = VectorUtils.BuildSprite(
                geometry,
                viewBoxRect.Value,
                svgPixelsPerUnit: 1f,
                alignment: VectorUtils.Alignment.Center,
                customPivot: Vector2.zero,
                gradientResolution: 32,
                flipYAxis: true);

            return sprite;
        }

        return null;
    }
    
    public static Sprite GetOrLoadCachedSprite(MapLayerDef def)
    {
        var key = (def.ImagePath, def.TesselationIndex);
        if (MapCache.TryGetValue(key, out var sprite))
            return sprite;

        var absolutePath = Path.Combine(Plugin.Path, def.ImagePath);
        return MapCache[key] = LoadSvgFromPath(def, absolutePath);
    }
    
    private static bool OverBudgetVertices(List<VectorUtils.Geometry> geometry)
    {
        var verts = 0;
        foreach (var g in geometry)
        {
            verts += g.Vertices?.Length ?? 0;
            if (verts > 65500) return true;
        }

        return false;
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
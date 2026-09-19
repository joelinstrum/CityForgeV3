using System;
using System.Collections.Generic;
using UnityEngine;

namespace CityForgeV3.World
{
    // The grass child keeps the existing ground/material contract. Only the
    // perimeter is new, using the clipped hedge plot's mesh and leaf contract.
    internal sealed class HedgeBorderedGrassPatch : MonoBehaviour
    {
        private static readonly Dictionary<string, Mesh> HedgeMeshes = new();
        private NaturalGrassGardenPatch _grass;
        private MeshRenderer _hedge;
        private MaterialPropertyBlock _properties;
        private float _opacity;

        internal static bool TryDimensions(string propId, out float width,
            out float depth)
        {
            width = depth = 0f;
            if (string.Equals(propId,
                    LotWorldController.HedgedGrassShortPropId,
                    StringComparison.OrdinalIgnoreCase))
            {
                width = 4f; depth = 2f;
            }
            else if (string.Equals(propId,
                    LotWorldController.HedgedGrassLongPropId,
                    StringComparison.OrdinalIgnoreCase))
            {
                width = 6f; depth = 3f;
            }
            else if (string.Equals(propId,
                    LotWorldController.HedgedGrassSquarePropId,
                    StringComparison.OrdinalIgnoreCase))
                width = depth = 4f;
            else if (string.Equals(propId,
                    LotWorldController.HedgedGrassLargeSquarePropId,
                    StringComparison.OrdinalIgnoreCase))
                width = depth = 6f;
            else if (IsCircle(propId))
                width = depth = 4f;
            return width > 0f;
        }

        internal static bool IsCircle(string propId) =>
            string.Equals(propId, LotWorldController.HedgedGrassCirclePropId,
                StringComparison.OrdinalIgnoreCase);

        internal static Transform Create(string name, float width, float depth,
            bool circular, float opacity, SeasonPreset season,
            TimeOfDayPreset timeOfDay, Vector3 sunDirection)
        {
            if (GeorgianClippedHedgeGarden.LeafMaterial(false) == null)
                return null;
            var root = new GameObject(name).transform;
            var patch = root.gameObject.AddComponent<HedgeBorderedGrassPatch>();
            patch._opacity = opacity;
            var grass = NaturalGrassGardenPatch.Create("Natural Grass center",
                width, depth, opacity, season, timeOfDay, sunDirection,
                circular);
            grass.SetParent(root, false);
            patch._grass = grass.GetComponent<NaturalGrassGardenPatch>();
            patch.BuildHedge(width, depth, circular);
            patch.SetAppearance(season, timeOfDay, sunDirection);
            return root;
        }

        private void BuildHedge(float width, float depth, bool circular)
        {
            var key = width + "x" + depth + (circular ? "-round" : "");
            if (!HedgeMeshes.TryGetValue(key, out var mesh) || mesh == null)
            {
                var builder = new GeorgianClippedHedgeGarden.HedgeMeshBuilder();
                var border = width == depth ? 0.40f : 0.38f;
                if (circular)
                    builder.AddRing(width * 0.5f - 0.03f, border, 0.54f);
                else
                {
                    var edgeX = width * 0.5f - border * 0.5f - 0.03f;
                    var edgeZ = depth * 0.5f - border * 0.5f - 0.03f;
                    builder.AddBox(0f, edgeZ, width - 0.06f, border, 0.54f);
                    builder.AddBox(0f, -edgeZ, width - 0.06f, border, 0.54f);
                    builder.AddBox(-edgeX, 0f, border,
                        depth - border * 2f - 0.06f, 0.54f);
                    builder.AddBox(edgeX, 0f, border,
                        depth - border * 2f - 0.06f, 0.54f);
                }
                mesh = builder.Finish();
                mesh.name = "Natural Grass hedge border " + key;
                HedgeMeshes[key] = mesh;
            }
            var perimeter = new GameObject("Clipped hedge perimeter");
            perimeter.transform.SetParent(transform, false);
            perimeter.AddComponent<MeshFilter>().sharedMesh = mesh;
            _hedge = perimeter.AddComponent<MeshRenderer>();
        }

        internal void SetOpacity(float opacity, SeasonPreset season,
            TimeOfDayPreset timeOfDay, Vector3 sunDirection)
        {
            _opacity = opacity;
            _grass?.SetOpacity(opacity, season, timeOfDay, sunDirection);
            SetHedgeAppearance(season);
        }

        internal void SetAppearance(SeasonPreset season,
            TimeOfDayPreset timeOfDay, Vector3 sunDirection)
        {
            _grass?.SetAppearance(season, timeOfDay, sunDirection);
            SetHedgeAppearance(season);
        }

        private void SetHedgeAppearance(SeasonPreset season)
        {
            if (_hedge == null) return;
            _hedge.sharedMaterial = GeorgianClippedHedgeGarden.LeafMaterial(
                _opacity < 0.99f);
            _properties ??= new MaterialPropertyBlock();
            _properties.SetColor("_Color",
                GeorgianClippedHedgeGarden.LeafColorForSeason(
                    season, _opacity));
            _hedge.SetPropertyBlock(_properties);
        }
    }
}

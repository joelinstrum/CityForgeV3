using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace CityForgeV3.World
{
    public sealed partial class LotWorldController
    {
        private const int DecalRenderQueue = 3003;
        private static readonly string[] GrassDecalTextureIds =
        {
            "leaves-01", "leaves-02", "leaves-03"
        };
        private static readonly string[] StreetDecalTextureIds =
        {
            "brick-street-01", "brick-street-02", "street-splat-01"
        };
        private static readonly string[] DirtDecalTextureIds =
        {
            "dirt-01", "dirt-02"
        };

        private Transform _decalRoot;
        private Transform _decalPreview;
        public bool LastDecalPlacementBlockedByBuilding { get; private set; }
        public bool LastDecalPlacementRequiresStreet { get; private set; }
        public int DecalCount => _session?.Data?.Decals?.Count ?? 0;
        private readonly Stack<string> _decalEraseUndo = new();

        private void BuildDecalRoot()
        {
            _decalRoot = new GameObject("Placed Decals").transform;
            _decalRoot.SetParent(transform, false);
            _decalPreview = BuildDecalQuad(_decalRoot, "Decal Paintbrush Preview",
                null, 1.25f, 1f, 0, new Color(0.18f, 0.95f, 0.78f, 0.42f));
            _decalPreview.gameObject.SetActive(false);
        }

        public void SetDecalPlacementPreview(bool active)
        {
            if (_decalPreview != null) _decalPreview.gameObject.SetActive(active);
        }

        public bool UpdateDecalPreviewFromPanel(Vector2 panelPosition, Vector2 panelSize)
        {
            if (_decalPreview == null ||
                !TryLotPointFromPanel(panelPosition, panelSize, out var point) ||
                !PointInsideLot(point))
            {
                SetDecalPlacementPreview(false);
                return false;
            }
            _decalPreview.gameObject.SetActive(true);
            _decalPreview.localPosition = new Vector3(point.x,
                SampleTerrainHeight(point.x, point.z) + 0.098f, point.z);
            return true;
        }

        public bool PlaceRandomDecalFromPanel(string categoryId,
            Vector2 panelPosition, Vector2 panelSize)
        {
            LastDecalPlacementBlockedByBuilding = false;
            LastDecalPlacementRequiresStreet = false;
            if ((categoryId != "grass" && categoryId != "street" &&
                 categoryId != "dirt") ||
                !TryLotPointFromPanel(panelPosition, panelSize, out var point) ||
                !PointInsideLot(point)) return false;
            if (PointOccupiedByBuilding(new Vector2(point.x, point.z)))
            {
                LastDecalPlacementBlockedByBuilding = true;
                return false;
            }

            if (categoryId == "street" &&
                !PointCoveredByStreet(new Vector2(point.x, point.z)))
            {
                LastDecalPlacementRequiresStreet = true;
                return false;
            }

            _session.Data.Decals ??= new List<PlacedDecal>();
            var textureIds = categoryId switch
            {
                "street" => StreetDecalTextureIds,
                "dirt" => DirtDecalTextureIds,
                _ => GrassDecalTextureIds
            };
            var textureIndex = UnityEngine.Random.Range(0, textureIds.Length);
            _session.Data.Decals.Add(new PlacedDecal
            {
                InstanceId = Guid.NewGuid().ToString("N"),
                CategoryId = categoryId,
                TextureId = textureIds[textureIndex],
                PositionX = point.x,
                PositionZ = point.z,
                RotationQuarterTurns = UnityEngine.Random.Range(0, 4),
                SizeMeters = categoryId == "street" ? 4f : 6f,
                AspectRatio = categoryId == "street" &&
                    textureIds[textureIndex].StartsWith("brick-street",
                        StringComparison.Ordinal)
                        ? 1.5f : 1f
            });
            _decalEraseUndo.Clear();
            RebuildDecalPresentations();
            NotifyStateChanged();
            return true;
        }

        public bool UndoLastDecal()
        {
            if (_decalEraseUndo.Count > 0)
            {
                var erasedInstanceId = _decalEraseUndo.Pop();
                var erased = _session.Data.Decals.Find(decal =>
                    decal.InstanceId == erasedInstanceId);
                if (erased?.EraseMarks?.Count > 0)
                {
                    erased.EraseMarks.RemoveAt(erased.EraseMarks.Count - 1);
                    RebuildDecalPresentations();
                    NotifyStateChanged();
                    return true;
                }
            }
            if ((_session?.Data?.Decals?.Count ?? 0) == 0) return false;
            _session.Data.Decals.RemoveAt(_session.Data.Decals.Count - 1);
            RebuildDecalPresentations();
            NotifyStateChanged();
            return true;
        }

        public bool EraseDecalFromPanel(Vector2 panelPosition, Vector2 panelSize)
        {
            if (!TryLotPointFromPanel(panelPosition, panelSize, out var point))
                return false;
            var groundPoint = new Vector2(point.x, point.z);
            for (var index = (_session.Data.Decals?.Count ?? 0) - 1;
                 index >= 0; index--)
            {
                var decal = _session.Data.Decals[index];
                var width = decal.SizeMeters * Mathf.Max(0.1f, decal.AspectRatio);
                var height = decal.SizeMeters;
                var offset = groundPoint - new Vector2(decal.PositionX, decal.PositionZ);
                var radians = -decal.RotationQuarterTurns * 90f * Mathf.Deg2Rad;
                var local = new Vector2(
                    offset.x * Mathf.Cos(radians) - offset.y * Mathf.Sin(radians),
                    offset.x * Mathf.Sin(radians) + offset.y * Mathf.Cos(radians));
                var u = local.x / width + 0.5f;
                var v = local.y / height + 0.5f;
                if (u < 0f || u > 1f || v < 0f || v > 1f) continue;
                decal.EraseMarks ??= new List<DecalEraseMark>();
                if (decal.EraseMarks.Count >= 32) decal.EraseMarks.RemoveAt(0);
                const float brushRadiusMeters = 0.7f;
                decal.EraseMarks.Add(new DecalEraseMark
                {
                    U = u,
                    V = v,
                    RadiusU = brushRadiusMeters / width,
                    RadiusV = brushRadiusMeters / height
                });
                _decalEraseUndo.Push(decal.InstanceId);
                RebuildDecalPresentations();
                NotifyStateChanged();
                return true;
            }
            return false;
        }

        private bool PointInsideLot(Vector3 point) =>
            point.x >= -LotWidthMeters * 0.5f && point.x <= LotWidthMeters * 0.5f &&
            point.z >= -LotDepthMeters * 0.5f && point.z <= LotDepthMeters * 0.5f;

        private bool PointOccupiedByBuilding(Vector2 point)
        {
            foreach (var placed in _session.Data.Buildings ?? new List<PlacedBuilding>())
            {
                var entry = BuildingCatalog.Find(placed.BuildingId);
                var package = HybridBuildingPackageRegistry.Load(entry.PackageResourcePath);
                if (BuildingFootprintContains(point,
                        new Vector2(placed.CellX, placed.CellZ),
                        package.WidthMeters, package.DepthMeters,
                        placed.RotationQuarterTurns)) return true;
            }
            foreach (var root in _experimentalBuilding3DVisibleRoots)
            {
                if (root == null) continue;
                var bounds = CombinedRendererBounds(root, out var hasBounds);
                if (!hasBounds) continue;
                var world = transform.TransformPoint(new Vector3(point.x, 0f, point.y));
                if (world.x >= bounds.min.x && world.x <= bounds.max.x &&
                    world.z >= bounds.min.z && world.z <= bounds.max.z) return true;
            }
            return false;
        }

        private bool PointCoveredByStreet(Vector2 point)
        {
            foreach (var placed in _session.Data.RoadPieces ??
                     new List<PlacedRoadPiece>())
            {
                var package = RoadPiecePackageCatalog.Resolve(placed.PackageId);
                if (package?.AllowsVehicles != true) continue;
                var center = RoadArtworkCenter(placed, package);
                var radians = -placed.RotationQuarterTurns * 90f * Mathf.Deg2Rad;
                var offset = point - center;
                var local = new Vector2(
                    offset.x * Mathf.Cos(radians) - offset.y * Mathf.Sin(radians),
                    offset.x * Mathf.Sin(radians) + offset.y * Mathf.Cos(radians));
                if (Mathf.Abs(local.x) <= package.RoadWidthMeters * 0.5f &&
                    Mathf.Abs(local.y) <= package.ArtworkLengthMeters * 0.5f)
                    return true;
            }
            return false;
        }

        private void RebuildDecalPresentations()
        {
            if (_decalRoot == null) return;
            for (var index = _decalRoot.childCount - 1; index >= 0; index--)
            {
                var child = _decalRoot.GetChild(index);
                if (child == _decalPreview) continue;
                if (Application.isPlaying) Destroy(child.gameObject);
                else DestroyImmediate(child.gameObject);
            }
            foreach (var placed in _session.Data.Decals ?? new List<PlacedDecal>())
            {
                var folder = placed.CategoryId switch
                {
                    "street" => "Street",
                    "dirt" => "Dirt",
                    _ => "Grass"
                };
                var texture = Resources.Load<Texture2D>(
                    $"CityForgeV3/Decals/{folder}/{placed.TextureId}");
                var quad = BuildDecalQuad(_decalRoot, $"Decal — {placed.TextureId}",
                    texture, placed.SizeMeters, placed.AspectRatio,
                    placed.RotationQuarterTurns, Color.white);
                ConformDecalToTerrain(quad, placed);
                var material = quad.GetComponent<MeshRenderer>().sharedMaterial;
                var marks = new Vector4[Mathf.Min(32,
                    placed.EraseMarks?.Count ?? 0)];
                for (var markIndex = 0; markIndex < marks.Length; markIndex++)
                {
                    var mark = placed.EraseMarks[markIndex];
                    marks[markIndex] = new Vector4(mark.U, mark.V,
                        mark.RadiusU, mark.RadiusV);
                }
                material.SetInt("_EraseMarkCount", marks.Length);
                if (marks.Length > 0) material.SetVectorArray("_EraseMarks", marks);
            }
        }

        private void RefreshDecalTerrainConformance()
        {
            if (_decalRoot == null) return;
            var placedIndex = 0;
            var decals = _session.Data.Decals ?? new List<PlacedDecal>();
            for (var childIndex = 0; childIndex < _decalRoot.childCount &&
                 placedIndex < decals.Count; childIndex++)
            {
                var child = _decalRoot.GetChild(childIndex);
                if (child == _decalPreview ||
                    !child.name.StartsWith("Decal — ", StringComparison.Ordinal))
                    continue;
                ConformDecalToTerrain(child, decals[placedIndex++]);
            }
        }

        private void ConformDecalToTerrain(Transform decal, PlacedDecal placed)
        {
            const float surfaceOffset = 0.035f;
            const float maximumVertexSpacing = 0.5f;
            var widthMeters = placed.SizeMeters *
                Mathf.Max(0.1f, placed.AspectRatio);
            var depthMeters = placed.SizeMeters;
            var columns = Mathf.Max(1,
                Mathf.CeilToInt(widthMeters / maximumVertexSpacing));
            var rows = Mathf.Max(1,
                Mathf.CeilToInt(depthMeters / maximumVertexSpacing));
            var vertices = new Vector3[(columns + 1) * (rows + 1)];
            var uv = new Vector2[vertices.Length];
            var radians = placed.RotationQuarterTurns * 90f * Mathf.Deg2Rad;
            var cosine = Mathf.Cos(radians);
            var sine = Mathf.Sin(radians);
            for (var row = 0; row <= rows; row++)
            for (var column = 0; column <= columns; column++)
            {
                var u = column / (float)columns;
                var v = row / (float)rows;
                var localX = (u - 0.5f) * widthMeters;
                var localZ = (v - 0.5f) * depthMeters;
                var rotatedX = localX * cosine + localZ * sine;
                var rotatedZ = -localX * sine + localZ * cosine;
                var x = placed.PositionX + rotatedX;
                var z = placed.PositionZ + rotatedZ;
                var index = row * (columns + 1) + column;
                vertices[index] = new Vector3(rotatedX,
                    SampleTerrainHeight(x, z) + surfaceOffset, rotatedZ);
                uv[index] = new Vector2(u, v);
            }
            var triangles = new int[columns * rows * 6];
            var triangle = 0;
            for (var row = 0; row < rows; row++)
            for (var column = 0; column < columns; column++)
            {
                var index = row * (columns + 1) + column;
                triangles[triangle++] = index;
                triangles[triangle++] = index + columns + 1;
                triangles[triangle++] = index + 1;
                triangles[triangle++] = index + 1;
                triangles[triangle++] = index + columns + 1;
                triangles[triangle++] = index + columns + 2;
            }
            var mesh = decal.GetComponent<MeshFilter>().sharedMesh;
            if (mesh == null || mesh.name != "Terrain-Conforming Decal")
            {
                mesh = new Mesh { name = "Terrain-Conforming Decal" };
                decal.GetComponent<MeshFilter>().sharedMesh = mesh;
            }
            mesh.Clear();
            mesh.vertices = vertices;
            mesh.triangles = triangles;
            mesh.uv = uv;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            decal.localPosition = new Vector3(placed.PositionX, 0f,
                placed.PositionZ);
            decal.localRotation = Quaternion.identity;
            decal.localScale = Vector3.one;
        }

        private static Transform BuildDecalQuad(Transform parent, string name,
            Texture2D texture, float sizeMeters, float aspectRatio,
            int quarterTurns, Color tint)
        {
            var quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            quad.name = name;
            quad.transform.SetParent(parent, false);
            quad.transform.localRotation = Quaternion.Euler(90f,
                quarterTurns * 90f, 0f);
            quad.transform.localScale = new Vector3(
                sizeMeters * Mathf.Max(0.1f, aspectRatio), sizeMeters, 1f);
            quad.GetComponent<Collider>().enabled = false;
            var shader = Shader.Find("CityForgeV3/SoftGroundDecal") ??
                Shader.Find("Unlit/Transparent") ??
                Shader.Find("Sprites/Default");
            var material = new Material(shader)
            {
                color = tint,
                mainTexture = texture,
                renderQueue = DecalRenderQueue
            };
            material.SetInt("_ZWrite", 0);
            var renderer = quad.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            return quad.transform;
        }
    }
}

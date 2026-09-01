using System;
using System.Collections.Generic;
using UnityEngine;

namespace CityForgeV3.World
{
    public sealed partial class LotWorldController
    {
        private Transform _waterRoot;
        private Transform _waterPreviewRoot;
        private Transform _waterSelectionRoot;
        private LineRenderer _waterBoundaryGuide;
        private readonly List<Vector2> _waterPlacementPoints = new();
        private Vector2 _waterPreviewPoint;
        private bool _waterPreviewHasPoint;
        private Material _swampWaterMaterial;
        private int _waterVertexDragIndex = -1;

        public bool WaterPlacementActive { get; private set; }
        public bool WaterVertexDragActive => _waterVertexDragIndex >= 0;
        public int SelectedWaterAreaIndex { get; private set; } = -1;
        public bool HasSelectedWaterArea => SelectedWaterAreaIndex >= 0 &&
            SelectedWaterAreaIndex < (_session.Data.WaterAreas?.Count ?? 0);
        public int WaterPlacementPointCount => _waterPlacementPoints.Count;
        public int WaterAreaCount => _session.Data.WaterAreas?.Count ?? 0;

        private void BuildWaterRoot()
        {
            _waterRoot = new GameObject("Placed Water Areas").transform;
            _waterRoot.SetParent(transform, false);
            _waterPreviewRoot = new GameObject("Water Area Preview").transform;
            _waterPreviewRoot.SetParent(transform, false);
            _waterSelectionRoot = new GameObject(
                "Selected Water Boundary").transform;
            _waterSelectionRoot.SetParent(transform, false);

            var guide = new GameObject("Water Boundary Guide");
            guide.transform.SetParent(transform, false);
            _waterBoundaryGuide = guide.AddComponent<LineRenderer>();
            _waterBoundaryGuide.useWorldSpace = false;
            _waterBoundaryGuide.loop = false;
            _waterBoundaryGuide.startWidth = 0.18f;
            _waterBoundaryGuide.endWidth = 0.18f;
            _waterBoundaryGuide.sharedMaterial = LotSurfaceMaterial(
                new Color(0.18f, 0.92f, 0.82f, 0.96f), 2021);
            guide.SetActive(false);
        }

        public void BeginSwampWaterPlacement()
        {
            DeselectWaterArea();
            WaterPlacementActive = true;
            _waterPlacementPoints.Clear();
            _waterPreviewHasPoint = false;
            ClearWaterPreviewMesh();
            RefreshWaterBoundaryGuide();
        }

        public bool AddSwampBoundaryPointFromPanel(Vector2 panelPosition,
            Vector2 panelSize)
        {
            if (!WaterPlacementActive ||
                !TryLotPointFromPanel(panelPosition, panelSize, out var point))
                return false;
            var boundaryPoint = ClampWaterPoint(new Vector2(point.x, point.z));
            if (_waterPlacementPoints.Count > 0 && Vector2.Distance(
                    _waterPlacementPoints[^1], boundaryPoint) < 0.25f)
                return false;
            _waterPlacementPoints.Add(boundaryPoint);
            _waterPreviewPoint = boundaryPoint;
            _waterPreviewHasPoint = true;
            RefreshWaterPlacementPreview();
            return true;
        }

        public bool UpdateSwampBoundaryPreviewFromPanel(Vector2 panelPosition,
            Vector2 panelSize)
        {
            if (!WaterPlacementActive ||
                !TryLotPointFromPanel(panelPosition, panelSize, out var point))
                return false;
            _waterPreviewPoint = ClampWaterPoint(new Vector2(point.x, point.z));
            _waterPreviewHasPoint = true;
            RefreshWaterPlacementPreview();
            return true;
        }

        public bool FinishSwampWaterPlacement()
        {
            if (!WaterPlacementActive || _waterPlacementPoints.Count < 3 ||
                Mathf.Abs(SignedPolygonArea(_waterPlacementPoints)) < 0.5f)
                return false;
            _session.Data.WaterAreas ??= new List<PlacedWaterArea>();
            var water = new PlacedWaterArea
            {
                InstanceId = Guid.NewGuid().ToString("N"),
                WaterId = "swamp-water"
            };
            foreach (var point in _waterPlacementPoints)
                water.Boundary.Add(new WaterBoundaryPoint(point.x, point.y));
            _session.Data.WaterAreas.Add(water);
            CancelWaterPlacement();
            RebuildWaterPresentations();
            NotifyStateChanged();
            return true;
        }

        public void CancelWaterPlacement()
        {
            WaterPlacementActive = false;
            _waterPlacementPoints.Clear();
            _waterPreviewHasPoint = false;
            ClearWaterPreviewMesh();
            if (_waterBoundaryGuide != null)
                _waterBoundaryGuide.gameObject.SetActive(false);
        }

        public bool BeginWaterAreaManipulationFromPanel(Vector2 panelPosition,
            Vector2 panelSize)
        {
            if (WaterPlacementActive ||
                !TryLotPointFromPanel(panelPosition, panelSize, out var point))
                return false;
            var local = new Vector2(point.x, point.z);
            var selected = -1;
            for (var index = (_session.Data.WaterAreas?.Count ?? 0) - 1;
                 index >= 0; index--)
            {
                var boundary = WaterBoundary(_session.Data.WaterAreas[index]);
                if (!PointInWaterPolygon(local, boundary)) continue;
                selected = index;
                break;
            }
            if (selected < 0)
            {
                DeselectWaterArea();
                return false;
            }
            SelectedWaterAreaIndex = selected;
            _waterVertexDragIndex = NearestWaterVertexIndex(
                _session.Data.WaterAreas[selected], local, 1.25f);
            RefreshWaterSelection();
            return true;
        }

        public bool DragSelectedWaterVertexFromPanel(Vector2 panelPosition,
            Vector2 panelSize)
        {
            if (!HasSelectedWaterArea || _waterVertexDragIndex < 0 ||
                !TryLotPointFromPanel(panelPosition, panelSize, out var point))
                return false;
            var water = _session.Data.WaterAreas[SelectedWaterAreaIndex];
            if (_waterVertexDragIndex >= water.Boundary.Count) return false;
            var target = ClampWaterPoint(new Vector2(point.x, point.z));
            water.Boundary[_waterVertexDragIndex].X = target.x;
            water.Boundary[_waterVertexDragIndex].Z = target.y;
            RebuildWaterPresentations();
            RefreshWaterSelection();
            return true;
        }

        public void EndWaterVertexDrag()
        {
            if (_waterVertexDragIndex < 0) return;
            _waterVertexDragIndex = -1;
            NotifyStateChanged();
        }

        public void DeselectWaterArea()
        {
            SelectedWaterAreaIndex = -1;
            _waterVertexDragIndex = -1;
            if (_waterSelectionRoot != null)
                ClearWaterChildren(_waterSelectionRoot);
        }

        public bool DeleteSelectedWaterArea()
        {
            if (!HasSelectedWaterArea) return false;
            _session.Data.WaterAreas.RemoveAt(SelectedWaterAreaIndex);
            DeselectWaterArea();
            RebuildWaterPresentations();
            NotifyStateChanged();
            return true;
        }

        public bool AdjustSelectedWaterTextureScale(float multiplier)
        {
            if (!HasSelectedWaterArea) return false;
            var water = _session.Data.WaterAreas[SelectedWaterAreaIndex];
            water.TextureScale = Mathf.Clamp(water.TextureScale * multiplier,
                0.025f, 0.8f);
            RebuildWaterPresentations();
            RefreshWaterSelection();
            NotifyStateChanged();
            return true;
        }

        private Vector2 ClampWaterPoint(Vector2 point) => new(
            Mathf.Clamp(point.x, -LotWidthMeters * 0.5f,
                LotWidthMeters * 0.5f),
            Mathf.Clamp(point.y, -LotDepthMeters * 0.5f,
                LotDepthMeters * 0.5f));

        private void RefreshWaterPlacementPreview()
        {
            RefreshWaterBoundaryGuide();
            var preview = new List<Vector2>(_waterPlacementPoints);
            if (_waterPreviewHasPoint && (preview.Count == 0 ||
                Vector2.Distance(preview[^1], _waterPreviewPoint) > 0.01f))
                preview.Add(_waterPreviewPoint);
            ClearWaterPreviewMesh();
            if (preview.Count < 3) return;
            BuildWaterMeshObject(_waterPreviewRoot, "Swamp Water Preview",
                preview, 0.04f, true);
        }

        private void RefreshWaterBoundaryGuide()
        {
            if (_waterBoundaryGuide == null) return;
            var count = _waterPlacementPoints.Count +
                (_waterPreviewHasPoint ? 1 : 0);
            if (!WaterPlacementActive || count == 0)
            {
                _waterBoundaryGuide.gameObject.SetActive(false);
                return;
            }
            _waterBoundaryGuide.positionCount = count;
            for (var index = 0; index < _waterPlacementPoints.Count; index++)
                _waterBoundaryGuide.SetPosition(index, new Vector3(
                    _waterPlacementPoints[index].x, 0.09f,
                    _waterPlacementPoints[index].y));
            if (_waterPreviewHasPoint)
                _waterBoundaryGuide.SetPosition(count - 1, new Vector3(
                    _waterPreviewPoint.x, 0.09f, _waterPreviewPoint.y));
            _waterBoundaryGuide.gameObject.SetActive(true);
        }

        private void RebuildWaterPresentations()
        {
            if (_waterRoot == null) return;
            ClearWaterChildren(_waterRoot);
            foreach (var water in _session.Data.WaterAreas ??
                     new List<PlacedWaterArea>())
            {
                if (water?.Boundary == null || water.Boundary.Count < 3)
                    continue;
                var points = new List<Vector2>(water.Boundary.Count);
                foreach (var point in water.Boundary)
                    points.Add(new Vector2(point.X, point.Z));
                BuildWaterMeshObject(_waterRoot,
                    $"Swamp Water — {water.InstanceId}", points,
                    water.HeightMeters, false, water.TextureScale,
                    water.TextureRotationDegrees);
            }
        }

        private void RefreshWaterSelection()
        {
            if (_waterSelectionRoot == null) return;
            ClearWaterChildren(_waterSelectionRoot);
            if (!HasSelectedWaterArea) return;
            var water = _session.Data.WaterAreas[SelectedWaterAreaIndex];
            var points = WaterBoundary(water);
            if (points.Count < 3) return;
            var material = LotSurfaceMaterial(
                new Color(0.12f, 1f, 0.82f, 0.96f), 2022);
            var outlineObject = new GameObject("Water Boundary Outline");
            outlineObject.transform.SetParent(_waterSelectionRoot, false);
            var outline = outlineObject.AddComponent<LineRenderer>();
            outline.useWorldSpace = false;
            outline.loop = true;
            outline.startWidth = 0.14f;
            outline.endWidth = 0.14f;
            outline.positionCount = points.Count;
            outline.sharedMaterial = material;
            for (var index = 0; index < points.Count; index++)
            {
                outline.SetPosition(index, new Vector3(
                    points[index].x, water.HeightMeters + 0.06f,
                    points[index].y));
                var handle = GameObject.CreatePrimitive(PrimitiveType.Cube);
                handle.name = $"Water Vertex {index + 1}";
                handle.transform.SetParent(_waterSelectionRoot, false);
                handle.transform.localPosition = new Vector3(points[index].x,
                    water.HeightMeters + 0.1f, points[index].y);
                handle.transform.localScale = new Vector3(0.42f, 0.12f, 0.42f);
                handle.GetComponent<Collider>().enabled = false;
                handle.GetComponent<Renderer>().sharedMaterial = material;
            }
        }

        private static List<Vector2> WaterBoundary(PlacedWaterArea water)
        {
            var result = new List<Vector2>();
            if (water?.Boundary == null) return result;
            foreach (var point in water.Boundary)
                result.Add(new Vector2(point.X, point.Z));
            return result;
        }

        private static int NearestWaterVertexIndex(PlacedWaterArea water,
            Vector2 point, float maximumDistance)
        {
            var nearest = -1;
            var nearestDistance = maximumDistance * maximumDistance;
            for (var index = 0; index < (water?.Boundary?.Count ?? 0); index++)
            {
                var candidate = new Vector2(
                    water.Boundary[index].X, water.Boundary[index].Z);
                var distance = Vector2.SqrMagnitude(candidate - point);
                if (distance > nearestDistance) continue;
                nearestDistance = distance;
                nearest = index;
            }
            return nearest;
        }

        public static bool PointInWaterPolygon(Vector2 point,
            IReadOnlyList<Vector2> polygon)
        {
            if (polygon == null || polygon.Count < 3) return false;
            var inside = false;
            for (int current = 0, previous = polygon.Count - 1;
                 current < polygon.Count; previous = current++)
            {
                var a = polygon[current];
                var b = polygon[previous];
                if ((a.y > point.y) == (b.y > point.y)) continue;
                var crossingX = (b.x - a.x) * (point.y - a.y) /
                    (b.y - a.y) + a.x;
                if (point.x < crossingX) inside = !inside;
            }
            return inside;
        }

        private void BuildWaterMeshObject(Transform parent, string name,
            IReadOnlyList<Vector2> points, float height, bool preview,
            float textureScale = 0.12f, float textureRotationDegrees = 0f)
        {
            var triangles = TriangulateWaterPolygon(points);
            if (triangles.Count < 3) return;
            var vertices = new Vector3[points.Count];
            var uv = new Vector2[points.Count];
            var radians = textureRotationDegrees * Mathf.Deg2Rad;
            var right = new Vector2(Mathf.Cos(radians), -Mathf.Sin(radians));
            var forward = new Vector2(Mathf.Sin(radians), Mathf.Cos(radians));
            for (var index = 0; index < points.Count; index++)
            {
                vertices[index] = new Vector3(points[index].x, height,
                    points[index].y);
                uv[index] = new Vector2(Vector2.Dot(points[index], right),
                    Vector2.Dot(points[index], forward)) * textureScale;
            }
            var mesh = new Mesh { name = $"{name} Mesh" };
            mesh.SetVertices(vertices);
            mesh.SetTriangles(triangles, 0);
            mesh.SetUVs(0, uv);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            var item = new GameObject(name);
            item.transform.SetParent(parent, false);
            item.AddComponent<MeshFilter>().sharedMesh = mesh;
            var renderer = item.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = SwampWaterMaterial();
            if (preview)
            {
                var properties = new MaterialPropertyBlock();
                properties.SetColor("_BaseColor",
                    new Color(0.26f, 0.62f, 0.42f, 0.62f));
                properties.SetColor("_Color",
                    new Color(0.26f, 0.62f, 0.42f, 0.62f));
                renderer.SetPropertyBlock(properties);
            }
        }

        private Material SwampWaterMaterial()
        {
            if (_swampWaterMaterial != null) return _swampWaterMaterial;
            var shader = Shader.Find("Universal Render Pipeline/Lit") ??
                Shader.Find("Standard");
            _swampWaterMaterial = new Material(shader)
            {
                name = "CityForge Swamp Water Runtime",
                color = new Color(0.19f, 0.34f, 0.16f, 0.96f),
                renderQueue = 2008
            };
            var albedo = Resources.Load<Texture2D>(
                "CityForgeV3/Water/Swamp/swamp_water_Albedo");
            var normal = Resources.Load<Texture2D>(
                "CityForgeV3/Water/Swamp/swamp_water_Normal");
            if (albedo != null)
            {
                _swampWaterMaterial.mainTexture = albedo;
                _swampWaterMaterial.SetTexture("_BaseMap", albedo);
            }
            if (normal != null)
            {
                _swampWaterMaterial.SetTexture("_BumpMap", normal);
                _swampWaterMaterial.EnableKeyword("_NORMALMAP");
            }
            _swampWaterMaterial.SetFloat("_Smoothness", 0.72f);
            _swampWaterMaterial.SetFloat("_Metallic", 0f);
            return _swampWaterMaterial;
        }

        private void ClearWaterPreviewMesh()
        {
            if (_waterPreviewRoot != null) ClearWaterChildren(_waterPreviewRoot);
        }

        private static void ClearWaterChildren(Transform parent)
        {
            for (var index = parent.childCount - 1; index >= 0; index--)
            {
                var child = parent.GetChild(index).gameObject;
                if (Application.isPlaying) Destroy(child);
                else DestroyImmediate(child);
            }
        }

        public static float SignedPolygonArea(IReadOnlyList<Vector2> points)
        {
            var area = 0f;
            for (var index = 0; index < points.Count; index++)
            {
                var next = points[(index + 1) % points.Count];
                area += points[index].x * next.y - next.x * points[index].y;
            }
            return area * 0.5f;
        }

        public static List<int> TriangulateWaterPolygon(
            IReadOnlyList<Vector2> points)
        {
            var result = new List<int>();
            if (points == null || points.Count < 3) return result;
            var remaining = new List<int>(points.Count);
            var counterClockwise = SignedPolygonArea(points) > 0f;
            if (counterClockwise)
                for (var index = 0; index < points.Count; index++)
                    remaining.Add(index);
            else
                for (var index = points.Count - 1; index >= 0; index--)
                    remaining.Add(index);

            var safety = points.Count * points.Count;
            while (remaining.Count > 2 && safety-- > 0)
            {
                var clipped = false;
                for (var index = 0; index < remaining.Count; index++)
                {
                    var previous = remaining[(index - 1 + remaining.Count) %
                        remaining.Count];
                    var current = remaining[index];
                    var next = remaining[(index + 1) % remaining.Count];
                    if (Cross(points[current] - points[previous],
                            points[next] - points[current]) <= 0.00001f)
                        continue;
                    var containsPoint = false;
                    for (var candidate = 0; candidate < remaining.Count;
                         candidate++)
                    {
                        var pointIndex = remaining[candidate];
                        if (pointIndex == previous || pointIndex == current ||
                            pointIndex == next) continue;
                        if (!PointInWaterTriangle(points[pointIndex],
                                points[previous], points[current], points[next]))
                            continue;
                        containsPoint = true;
                        break;
                    }
                    if (containsPoint) continue;
                    result.Add(previous);
                    result.Add(current);
                    result.Add(next);
                    remaining.RemoveAt(index);
                    clipped = true;
                    break;
                }
                if (!clipped) break;
            }
            return result;
        }

        private static float Cross(Vector2 a, Vector2 b) =>
            a.x * b.y - a.y * b.x;

        private static bool PointInWaterTriangle(Vector2 point, Vector2 a,
            Vector2 b, Vector2 c)
        {
            var ab = Cross(b - a, point - a);
            var bc = Cross(c - b, point - b);
            var ca = Cross(a - c, point - c);
            return ab >= -0.00001f && bc >= -0.00001f &&
                   ca >= -0.00001f;
        }
    }
}

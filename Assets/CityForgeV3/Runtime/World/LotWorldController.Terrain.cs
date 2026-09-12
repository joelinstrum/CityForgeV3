using System.Collections.Generic;
using UnityEngine;

namespace CityForgeV3.World
{
    public sealed partial class LotWorldController
    {
        private const float TerrainVertexSpacing = 1f;
        private const float TerrainMaximumHeight = 12f;
        private const float TerrainMaximumGradePerMeter = 0.12f;
        private readonly Stack<string> _terrainUndo = new();
        private LineRenderer _terrainBrushCursor;
        private bool _terrainStrokeActive;
        private string _terrainStrokeStart = "";
        private Vector3 _lastTerrainStrokePoint;
        private bool _hasLastTerrainStrokePoint;

        public bool CanUndoTerrainSculpt => _terrainUndo.Count > 0;
        public bool TerrainStrokeActive => _terrainStrokeActive;

        private void EnsureTerrainData()
        {
            var width = Mathf.RoundToInt((LotWidthMeters + 4f) / TerrainVertexSpacing) + 1;
            var depth = Mathf.RoundToInt((LotDepthMeters + 4f) / TerrainVertexSpacing) + 1;
            var count = width * depth;
            if (_session.Data.TerrainGridWidth == width &&
                _session.Data.TerrainGridDepth == depth &&
                _session.Data.TerrainHeights?.Count == count) return;
            _session.Data.TerrainGridWidth = width;
            _session.Data.TerrainGridDepth = depth;
            _session.Data.TerrainHeights = new List<float>(new float[count]);
        }

        private void RebuildTerrainMesh()
        {
            if (_groundMeshFilter == null) return;
            EnsureTerrainData();
            var sourceWidth = _session.Data.TerrainGridWidth;
            var sourceDepth = _session.Data.TerrainGridDepth;
            // The Lot Editor intentionally renders a two-metre apron around
            // each edge. In a district, adjacent lots share one continuous
            // world and that apron must not cover neighboring cells.
            var crop = _districtHosted ? 2 : 0;
            var width = sourceWidth - crop * 2;
            var depth = sourceDepth - crop * 2;
            var sourceHalfWidth = (sourceWidth - 1) * TerrainVertexSpacing * 0.5f;
            var sourceHalfDepth = (sourceDepth - 1) * TerrainVertexSpacing * 0.5f;
            var vertices = new Vector3[width * depth];
            var uv = new Vector2[vertices.Length];
            for (var z = 0; z < depth; z++)
            for (var x = 0; x < width; x++)
            {
                var index = z * width + x;
                var sourceX = x + crop;
                var sourceZ = z + crop;
                vertices[index] = new Vector3(
                    sourceX * TerrainVertexSpacing - sourceHalfWidth,
                    _session.Data.TerrainHeights[sourceZ * sourceWidth + sourceX],
                    sourceZ * TerrainVertexSpacing - sourceHalfDepth);
                uv[index] = new Vector2(x / (float)(width - 1), z / (float)(depth - 1));
            }
            var triangles = new int[(width - 1) * (depth - 1) * 6];
            var triangle = 0;
            for (var z = 0; z < depth - 1; z++)
            for (var x = 0; x < width - 1; x++)
            {
                var i = z * width + x;
                triangles[triangle++] = i;
                triangles[triangle++] = i + width;
                triangles[triangle++] = i + 1;
                triangles[triangle++] = i + 1;
                triangles[triangle++] = i + width;
                triangles[triangle++] = i + width + 1;
            }
            var mesh = _groundMeshFilter.sharedMesh;
            if (mesh == null)
            {
                mesh = new Mesh { name = "CityForge Terrain Heightfield" };
                _groundMeshFilter.sharedMesh = mesh;
            }
            mesh.Clear();
            mesh.vertices = vertices;
            mesh.triangles = triangles;
            mesh.uv = uv;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            if (_terrainShadowCasterMeshFilter != null)
                _terrainShadowCasterMeshFilter.sharedMesh = mesh;
            _groundMeshCollider.sharedMesh = null;
            _groundMeshCollider.sharedMesh = mesh;
            RefreshTerrainGridGeometry();
            RefreshDecalTerrainConformance();
        }

        public bool UpdateTerrainBrushFromPanel(Vector2 panelPosition, Vector2 panelSize, float radius)
        {
            if (!TryLotPointFromPanel(panelPosition, panelSize, out var point))
            {
                SetTerrainCursorVisible(false);
                return false;
            }
            EnsureTerrainCursor();
            const int segments = 48;
            _terrainBrushCursor.positionCount = segments + 1;
            for (var index = 0; index <= segments; index++)
            {
                var angle = index / (float)segments * Mathf.PI * 2f;
                var x = point.x + Mathf.Cos(angle) * radius;
                var z = point.z + Mathf.Sin(angle) * radius;
                _terrainBrushCursor.SetPosition(index,
                    transform.TransformPoint(new Vector3(x, SampleTerrainHeight(x, z) + 0.08f, z)));
            }
            SetTerrainCursorVisible(true);
            return true;
        }

        public bool BeginTerrainStrokeFromPanel(Vector2 panelPosition, Vector2 panelSize,
            float radius, float strength, bool raise)
        {
            if (!TryLotPointFromPanel(panelPosition, panelSize, out var point)) return false;
            _terrainStrokeStart = _session.Serialize();
            _terrainStrokeActive = true;
            _hasLastTerrainStrokePoint = false;
            SculptTerrainAt(new Vector2(point.x, point.z), radius, strength, raise);
            return true;
        }

        public bool ContinueTerrainStrokeFromPanel(Vector2 panelPosition, Vector2 panelSize,
            float radius, float strength, bool raise)
        {
            if (!_terrainStrokeActive ||
                !TryLotPointFromPanel(panelPosition, panelSize, out var point)) return false;
            if (_hasLastTerrainStrokePoint &&
                Vector2.Distance(new Vector2(point.x, point.z),
                    new Vector2(_lastTerrainStrokePoint.x, _lastTerrainStrokePoint.z)) < 0.2f)
                return false;
            SculptTerrainAt(new Vector2(point.x, point.z), radius, strength, raise);
            return true;
        }

        public bool EndTerrainStroke()
        {
            if (!_terrainStrokeActive) return false;
            _terrainStrokeActive = false;
            _hasLastTerrainStrokePoint = false;
            if (_terrainStrokeStart == _session.Serialize()) return false;
            _terrainUndo.Push(_terrainStrokeStart);
            while (_terrainUndo.Count > 30)
            {
                var retained = _terrainUndo.ToArray();
                _terrainUndo.Clear();
                for (var index = Mathf.Min(28, retained.Length - 1); index >= 0; index--)
                    _terrainUndo.Push(retained[index]);
            }
            NotifyStateChanged();
            return true;
        }

        public bool UndoTerrainSculpt()
        {
            if (_terrainUndo.Count == 0) return false;
            _session.Restore(_terrainUndo.Pop());
            RebuildTerrainMesh();
            NotifyStateChanged();
            return true;
        }

        public void HideTerrainBrush() => SetTerrainCursorVisible(false);

        public float SampleTerrainHeight(float x, float z)
        {
            EnsureTerrainData();
            var width = _session.Data.TerrainGridWidth;
            var depth = _session.Data.TerrainGridDepth;
            var halfWidth = (width - 1) * TerrainVertexSpacing * 0.5f;
            var halfDepth = (depth - 1) * TerrainVertexSpacing * 0.5f;
            var gx = Mathf.Clamp((x + halfWidth) / TerrainVertexSpacing, 0f, width - 1f);
            var gz = Mathf.Clamp((z + halfDepth) / TerrainVertexSpacing, 0f, depth - 1f);
            var x0 = Mathf.FloorToInt(gx);
            var z0 = Mathf.FloorToInt(gz);
            var x1 = Mathf.Min(x0 + 1, width - 1);
            var z1 = Mathf.Min(z0 + 1, depth - 1);
            var a = Mathf.Lerp(_session.Data.TerrainHeights[z0 * width + x0],
                _session.Data.TerrainHeights[z0 * width + x1], gx - x0);
            var b = Mathf.Lerp(_session.Data.TerrainHeights[z1 * width + x0],
                _session.Data.TerrainHeights[z1 * width + x1], gx - x0);
            var terrainHeight = Mathf.Lerp(a, b, gz - z0);
            if (!_districtHosted || _districtRiverSurfaceSampler == null)
                return terrainHeight;
            var world = transform.TransformPoint(new Vector3(x,
                terrainHeight, z));
            var river = _districtRiverSurfaceSampler(world);
            if (!river.HasValue || !river.Value.InsideChannel)
                return terrainHeight;
            return transform.InverseTransformPoint(new Vector3(world.x,
                river.Value.BedElevation, world.z)).y;
        }

        public bool SculptTerrainAt(Vector2 center, float radius, float strength, bool raise)
        {
            if (radius <= 0f || strength <= 0f) return false;
            EnsureTerrainData();
            var width = _session.Data.TerrainGridWidth;
            var depth = _session.Data.TerrainGridDepth;
            var halfWidth = (width - 1) * TerrainVertexSpacing * 0.5f;
            var halfDepth = (depth - 1) * TerrainVertexSpacing * 0.5f;
            var supportHeight = TerrainBrushSupportHeight(center, radius,
                width, depth, halfWidth, halfDepth);
            var maximumRelativeHeight = radius * TerrainMaximumGradePerMeter;
            for (var z = 0; z < depth; z++)
            for (var x = 0; x < width; x++)
            {
                var worldX = x * TerrainVertexSpacing - halfWidth;
                var worldZ = z * TerrainVertexSpacing - halfDepth;
                if (Mathf.Abs(worldX) > LotWidthMeters * 0.5f ||
                    Mathf.Abs(worldZ) > LotDepthMeters * 0.5f) continue;
                var distance = Vector2.Distance(new Vector2(worldX, worldZ),
                    center);
                if (distance >= radius) continue;
                var falloff = Mathf.SmoothStep(1f, 0f, distance / radius);
                var index = z * width + x;
                var delta = strength * falloff * (raise ? 1f : -1f);
                var minimum = Mathf.Max(-TerrainMaximumHeight,
                    supportHeight - maximumRelativeHeight);
                var maximum = Mathf.Min(TerrainMaximumHeight,
                    supportHeight + maximumRelativeHeight);
                _session.Data.TerrainHeights[index] = Mathf.Clamp(
                    _session.Data.TerrainHeights[index] + delta,
                    minimum, maximum);
            }
            EnforceTerrainMaximumGrade(raise);
            _lastTerrainStrokePoint = new Vector3(center.x,
                SampleTerrainHeight(center.x, center.y), center.y);
            _hasLastTerrainStrokePoint = true;
            RebuildTerrainMesh();
            return true;
        }

        private float TerrainBrushSupportHeight(Vector2 center, float radius,
            int width, int depth, float halfWidth, float halfDepth)
        {
            var sum = 0f;
            var count = 0;
            var ringThickness = TerrainVertexSpacing * 1.5f;
            for (var z = 0; z < depth; z++)
            for (var x = 0; x < width; x++)
            {
                var worldX = x * TerrainVertexSpacing - halfWidth;
                var worldZ = z * TerrainVertexSpacing - halfDepth;
                if (Mathf.Abs(worldX) > LotWidthMeters * 0.5f ||
                    Mathf.Abs(worldZ) > LotDepthMeters * 0.5f) continue;
                var distance = Vector2.Distance(
                    new Vector2(worldX, worldZ), center);
                if (distance < radius || distance > radius + ringThickness) continue;
                sum += _session.Data.TerrainHeights[z * width + x];
                count++;
            }
            return count > 0 ? sum / count : SampleTerrainHeight(center.x, center.y);
        }

        private void EnforceTerrainMaximumGrade(bool raising)
        {
            var width = _session.Data.TerrainGridWidth;
            var depth = _session.Data.TerrainGridDepth;
            var heights = _session.Data.TerrainHeights;
            // Relax both peaks and pits against their immediate neighbors.
            // Repeated passes propagate the grade constraint outward, so a
            // taller landform must occupy a wider footprint instead of
            // stacking into a narrow mound.
            for (var pass = 0; pass < 16; pass++)
            {
                var source = heights.ToArray();
                for (var z = 1; z < depth - 1; z++)
                for (var x = 1; x < width - 1; x++)
                {
                    var index = z * width + x;
                    var minimumNeighbor = Mathf.Min(
                        source[index - 1], source[index + 1],
                        source[index - width], source[index + width]);
                    var maximumNeighbor = Mathf.Max(
                        source[index - 1], source[index + 1],
                        source[index - width], source[index + width]);
                    heights[index] = raising
                        ? Mathf.Min(source[index],
                            minimumNeighbor + TerrainMaximumGradePerMeter)
                        : Mathf.Max(source[index],
                            maximumNeighbor - TerrainMaximumGradePerMeter);
                }
            }
        }

        private void EnsureTerrainCursor()
        {
            if (_terrainBrushCursor != null) return;
            var cursor = new GameObject("Terrain Brush Cursor");
            cursor.transform.SetParent(transform);
            _terrainBrushCursor = cursor.AddComponent<LineRenderer>();
            _terrainBrushCursor.useWorldSpace = true;
            _terrainBrushCursor.loop = false;
            _terrainBrushCursor.widthMultiplier = 0.09f;
            _terrainBrushCursor.material = new Material(Shader.Find("Sprites/Default"));
            _terrainBrushCursor.startColor = new Color(1f, 0.76f, 0.18f, 0.95f);
            _terrainBrushCursor.endColor = _terrainBrushCursor.startColor;
            _terrainBrushCursor.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            _terrainBrushCursor.receiveShadows = false;
        }

        private void SetTerrainCursorVisible(bool visible)
        {
            if (_terrainBrushCursor != null)
                _terrainBrushCursor.gameObject.SetActive(visible);
        }
    }
}

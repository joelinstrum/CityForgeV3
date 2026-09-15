using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace CityForgeV3.World
{
    // Presentation-only default terrain dressing. Never adds items to a save or
    // consumes Unity's random state (which also drives flora planting).
    public sealed class DistrictGroundDecals : MonoBehaviour
    {
        private const float Spacing = 8f;
        private const int CellsPerChunk = 16;
        private const float Elevation = .17f; // Above district road artwork (.152).
        private readonly List<Mesh> _meshes = new();
        private readonly List<Material> _materials = new();
        private readonly List<Renderer> _renderers = new();
        private DistrictWorldController _world;
        public int PatchCount { get; private set; }
        public bool PresentationEnabled { get; set; } = true;

        public void Rebuild(DistrictWorldController world, RegionCityTile district,
            float width, float depth)
        {
            Release();
            _world = world;
            var shader = Shader.Find("CityForgeV3/SoftGroundDecal");
            if (shader == null) return;
            for (var i = 1; i <= 3; i++)
            {
                var texture = Resources.Load<Texture2D>(
                    $"CityForgeV3/Decals/Grass/leaves-0{i}");
                if (texture == null) { Release(); return; }
                _materials.Add(new Material(shader)
                {
                    name = $"Default District Leaves {i}",
                    mainTexture = texture,
                    color = Color.white,
                    renderQueue = 3003
                });
            }
            uint seed = 2166136261;
            foreach (var c in district.TileId ?? "") seed = unchecked((seed ^ c) * 16777619);
            var columns = Mathf.CeilToInt(width / Spacing);
            var rows = Mathf.CeilToInt(depth / Spacing);
            for (var cz = 0; cz < rows; cz += CellsPerChunk)
            for (var cx = 0; cx < columns; cx += CellsPerChunk)
            {
                var vertices = new List<Vector3>();
                var uv = new List<Vector2>();
                var triangles = new[] { new List<int>(), new List<int>(), new List<int>() };
                for (var z = cz; z < Mathf.Min(rows, cz + CellsPerChunk); z++)
                for (var x = cx; x < Mathf.Min(columns, cx + CellsPerChunk); x++)
                {
                    var h = Hash(seed ^ unchecked((uint)x * 73856093u) ^ unchecked((uint)z * 19349663u));
                    if (h % 10 == 0) continue;
                    var px = -width * .5f + (x + .5f) * Spacing + (Unit(h) - .5f) * 4f;
                    var pz = -depth * .5f + (z + .5f) * Spacing + (Unit(Hash(h)) - .5f) * 4f;
                    var half = 2.5f + Unit(Hash(h + 1)) * 1.5f;
                    // Keep whole patches inside the district and outside the
                    // carved channel, including its banks. Recomputed on river edits.
                    if (Mathf.Abs(px) + half > width * .5f || Mathf.Abs(pz) + half > depth * .5f) continue;
                    var corners = new[] {
                        new Vector3(px-half, Elevation, pz-half),
                        new Vector3(px+half, Elevation, pz-half),
                        new Vector3(px+half, Elevation, pz+half),
                        new Vector3(px-half, Elevation, pz+half) };
                    bool river = world.SampleRiverSurface(transform.TransformPoint(new Vector3(px, 0f, pz))).HasValue;
                    foreach (var corner in corners)
                        river |= world.SampleRiverSurface(transform.TransformPoint(corner)).HasValue;
                    if (river) continue;
                    for(int k=0;k<corners.Length;k++) corners[k].y += world.TerrainElevation(corners[k].x,corners[k].z);
                    int start = vertices.Count;
                    vertices.AddRange(corners);
                    var turn = (int)(Hash(h + 2) % 4);
                    var texcoords = new[] { Vector2.zero, Vector2.right, Vector2.one, Vector2.up };
                    for (var k = 0; k < 4; k++) uv.Add(texcoords[(k + turn) % 4]);
                    var indices = triangles[h % 3];
                    indices.Add(start); indices.Add(start+2); indices.Add(start+1);
                    indices.Add(start); indices.Add(start+3); indices.Add(start+2);
                    PatchCount++;
                }
                if (vertices.Count == 0) continue;
                var mesh = new Mesh { name = $"District Leaves {cx},{cz}", subMeshCount = 3 };
                mesh.SetVertices(vertices);
                mesh.SetUVs(0, uv);
                for (var i = 0; i < 3; i++) mesh.SetTriangles(triangles[i], i);
                mesh.RecalculateBounds();
                _meshes.Add(mesh);
                var chunk = new GameObject(mesh.name);
                chunk.transform.SetParent(transform, false);
                chunk.AddComponent<MeshFilter>().sharedMesh = mesh;
                var renderer = chunk.AddComponent<MeshRenderer>();
                renderer.sharedMaterials = _materials.ToArray();
                renderer.shadowCastingMode = ShadowCastingMode.Off;
                renderer.receiveShadows = false;
                _renderers.Add(renderer);
            }
            if(district.Hills!=null && district.Hills.HeightMeters>0)
            {
                var hills=new GameObject("Hill Surface Detail");hills.transform.SetParent(transform,false);
                hills.AddComponent<DistrictHillGroundOverlay>().Rebuild(world,district,width,depth);
            }
            LateUpdate();
        }

        private static uint Hash(uint value)
        {
            unchecked { value ^= value >> 16; value *= 0x7feb352du;
                value ^= value >> 15; value *= 0x846ca68bu; return value ^ (value >> 16); }
        }
        private static float Unit(uint value) => (value & 65535) / 65535f;

        private void LateUpdate()
        {
            if (_world == null || _world.WorldCamera == null) return;
            // Subpixel litter fades out at district overview scales. Chunk
            // bounds provide normal frustum culling while inspecting close up.
            var alpha = 1f - Mathf.InverseLerp(80f, 180f, _world.WorldCamera.orthographicSize);
            var tint = LotWorldController.TextureTintForTimeOfDay(_world.TimeOfDay);
            tint.a = alpha;
            foreach (var material in _materials) material.color = tint;
            foreach (var renderer in _renderers)
                renderer.enabled = PresentationEnabled && alpha > .001f;
        }

        private void Release()
        {
            for (var i = transform.childCount - 1; i >= 0; i--)
            {
                var child = transform.GetChild(i).gameObject;
                child.SetActive(false);
                Dispose(child);
            }
            foreach (var mesh in _meshes) Dispose(mesh);
            foreach (var material in _materials) Dispose(material);
            _meshes.Clear(); _materials.Clear(); _renderers.Clear(); PatchCount = 0;
        }
        private static void Dispose(Object value)
        {
            if (Application.isPlaying) Destroy(value); else DestroyImmediate(value);
        }
        private void OnDestroy() => Release();
    }
}

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace CityForgeV3.World
{
    // Rendering only: individual tree objects remain the selection/animation handles.
    // Static geometry is cached by spatial cell and sprite; no work runs each frame.
    public sealed class DistrictFloraBatches : MonoBehaviour
    {
        private const float CellSize = 256f;
        private readonly Dictionary<(Vector2Int, Sprite), List<SpriteRenderer>> cells = new();
        private readonly Dictionary<SpriteRenderer, (Vector2Int, Sprite)> membership = new();
        private readonly Dictionary<(Vector2Int, Sprite), List<GameObject>> outputs = new();
        private readonly Dictionary<Sprite, Geometry> geometry = new();
        private sealed class Geometry
        {
            public Vector2[] Vertices, UV;
            public ushort[] Triangles;
        }
        public void Build(IEnumerable<SpriteRenderer> trees)
        {
            foreach (var tree in trees)
            {
                if (tree == null || tree.sprite == null) continue;
                var p = transform.InverseTransformPoint(tree.transform.position);
                var cell = (new Vector2Int(Mathf.FloorToInt(p.x / CellSize), Mathf.FloorToInt(p.z / CellSize)), tree.sprite);
                if (!cells.TryGetValue(cell, out var list)) cells[cell] = list = new();
                list.Add(tree); membership[tree] = cell;
            }
            Rebuild();
        }
        public void Rebuild()
        {
            foreach (var cell in cells.Keys) RebuildCell(cell);
        }
        readonly HashSet<(Vector2Int, Sprite)> dirtyCells = new();
        int changeDepth;
        public void BeginChanges() => changeDepth++;
        public void EndChanges()
        {
            if (changeDepth == 0 || --changeDepth != 0) return;
            foreach (var cell in dirtyCells) RebuildCell(cell);
            dirtyCells.Clear();
        }
        void QueueCellRebuild((Vector2Int, Sprite) cell)
        {
            if (changeDepth > 0) dirtyCells.Add(cell); else RebuildCell(cell);
        }

        public void Add(SpriteRenderer tree)
        {
            if (tree == null || tree.sprite == null || membership.ContainsKey(tree)) return;
            var p = transform.InverseTransformPoint(tree.transform.position);
            var cell = (new Vector2Int(Mathf.FloorToInt(p.x / CellSize), Mathf.FloorToInt(p.z / CellSize)), tree.sprite);
            if (!cells.TryGetValue(cell, out var list)) cells[cell] = list = new();
            list.Add(tree); membership[tree] = cell;
            QueueCellRebuild(cell);
        }
        public void Remove(SpriteRenderer tree)
        {
            if (tree == null || !membership.Remove(tree, out var cell)) return;
            cells[cell].Remove(tree);
            tree.forceRenderingOff = false;
            var shadow = Shadow(tree);
            if (shadow != null) shadow.forceRenderingOff = false;
            QueueCellRebuild(cell);
        }
        private static MeshRenderer Shadow(SpriteRenderer tree) =>
            tree.transform.Find("District Flora Shadow")?.GetComponent<MeshRenderer>();
        private void RebuildCell((Vector2Int, Sprite) cell)
        {
            if (outputs.TryGetValue(cell, out var old))
                foreach (var item in old) { item.SetActive(false); Dispose(item); }
            outputs[cell] = new();
            var groups = new Dictionary<Sprite, List<SpriteRenderer>>();
            foreach (var tree in cells[cell])
            {
                if (tree == null || tree.sprite == null) continue;
                if (!groups.TryGetValue(tree.sprite, out var list)) groups[tree.sprite] = list = new();
                list.Add(tree);
            }
            foreach (var pair in groups)
            {
                var sprite = pair.Key;
                if (!geometry.TryGetValue(sprite, out var source))
                    geometry[sprite] = source = new Geometry { Vertices = sprite.vertices, UV = sprite.uv, Triangles = sprite.triangles };
                // Preserve back-to-front order within a texture group. Depth still
                // resolves overlaps with other species, lots, and water.
                pair.Value.Sort((a, b) => a.sortingOrder.CompareTo(b.sortingOrder));
                var vertices = new List<Vector3>(); var uv = new List<Vector2>();
                var colors = new List<Color>(); var triangles = new List<int>();
                var shadowVertices = new List<Vector3>(); var shadowUV = new List<Vector2>();
                var shadowColors = new List<Color>(); var shadowTriangles = new List<int>();
                MeshRenderer firstShadow = null;
                foreach (var tree in pair.Value)
                {
                    int offset = vertices.Count;
                    var matrix = transform.worldToLocalMatrix * tree.transform.localToWorldMatrix;
                    foreach (var v in source.Vertices) { vertices.Add(matrix.MultiplyPoint3x4(v)); colors.Add(tree.color); }
                    uv.AddRange(source.UV);
                    foreach (var i in source.Triangles) triangles.Add(offset + i);
                    tree.forceRenderingOff = true;
                    var shadow = Shadow(tree);
                    if (shadow == null) continue;
                    shadow.forceRenderingOff = true;
                    if (!shadow.enabled) continue;
                    firstShadow ??= shadow;
                    var mesh = shadow.GetComponent<MeshFilter>().sharedMesh;
                    offset = shadowVertices.Count;
                    matrix = transform.worldToLocalMatrix * shadow.transform.localToWorldMatrix;
                    foreach (var v in mesh.vertices) shadowVertices.Add(matrix.MultiplyPoint3x4(v));
                    shadowUV.AddRange(mesh.uv); shadowColors.AddRange(mesh.colors);
                    foreach (var i in mesh.triangles) shadowTriangles.Add(offset + i);
                }
                var properties = new MaterialPropertyBlock();
                pair.Value[0].GetPropertyBlock(properties); properties.SetTexture("_MainTex", sprite.texture);
                Create(cell, "Flora batch", pair.Value[0].sharedMaterial, properties, vertices, uv, colors, triangles);
                if (firstShadow != null)
                {
                    firstShadow.GetPropertyBlock(properties);
                    Create(cell, "Flora shadow batch", firstShadow.sharedMaterial, properties,
                        shadowVertices, shadowUV, shadowColors, shadowTriangles);
                }
            }
        }
        private void Create((Vector2Int, Sprite) cell, string label, Material material, MaterialPropertyBlock properties,
            List<Vector3> vertices, List<Vector2> uv, List<Color> colors, List<int> triangles)
        {
            var item = new GameObject(label); item.transform.SetParent(transform, false);
            var mesh = new Mesh { name = label, indexFormat = vertices.Count > 65535 ? IndexFormat.UInt32 : IndexFormat.UInt16 };
            mesh.SetVertices(vertices); mesh.SetUVs(0, uv); mesh.SetColors(colors); mesh.SetTriangles(triangles, 0);
            mesh.RecalculateBounds();
            item.AddComponent<MeshFilter>().sharedMesh = mesh;
            item.AddComponent<DistrictFloraShadowMesh>(); // Own and release the generated mesh.
            var renderer = item.AddComponent<MeshRenderer>(); renderer.sharedMaterial = material;
            renderer.SetPropertyBlock(properties); renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            outputs[cell].Add(item);
        }
        private static void Dispose(Object item)
        { if (Application.isPlaying) Destroy(item); else DestroyImmediate(item); }
    }
}

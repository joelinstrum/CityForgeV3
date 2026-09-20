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
        sealed class Chunk { public GameObject Object; public Mesh Mesh; public Renderer Renderer; public int Patches; }
        readonly Dictionary<Vector2Int,Chunk> _chunks=new();
        DistrictDirtyGrid _dirty;
        string _districtId;
        DistrictHillGroundOverlay _hillOverlay;
        public int LastUpdatedChunkCount { get; private set; }
        public int ChunkCount => _chunks.Count;
        private readonly List<Material> _materials = new();

        private DistrictWorldController _world;
        bool _presentationDirty=true,_lastEnabled;
        Color _lastTint;
        public int PatchCount { get; private set; }
        bool _presentationEnabled = true;
        public bool PresentationEnabled
        {
            get => _presentationEnabled;
            set
            {
                if (_presentationEnabled == value) return;
                _presentationEnabled = value;
                _presentationDirty = true;
                // The broad dry-grass hill treatment is part of the same
                // presentation-only dressing. The QA toggle must hide it too
                // so the authored base albedo can be reviewed in isolation.
                if (_hillOverlay != null)
                    _hillOverlay.PresentationEnabled = value;
            }
        }

        public void Rebuild(DistrictWorldController world, RegionCityTile district,float width,float depth)
            => Refresh(world,district,width,depth,null);

        public void Refresh(DistrictWorldController world,RegionCityTile district,float width,float depth,IReadOnlyList<Rect> changed)
        {
            bool initialize=_dirty==null || _districtId!=district.TileId || _dirty.Bounds.width!=width || _dirty.Bounds.height!=depth;
            if(initialize)
            {
                Release();_world=world;_districtId=district.TileId;
                _dirty=new DistrictDirtyGrid(new Rect(-width/2,-depth/2,width,depth),Spacing*CellsPerChunk);
                var shader=Shader.Find("CityForgeV3/SoftGroundDecal");if(shader==null)return;
                for(int i=1;i<=3;i++)
                {
                    var texture=Resources.Load<Texture2D>($"CityForgeV3/Decals/Grass/leaves-0{i}");
                    if(texture==null){Release();return;}
                    _materials.Add(new Material(shader){name=$"Default District Leaves {i}",mainTexture=texture,color=Color.white,renderQueue=3003});
                }
            }
            if(initialize || changed==null)_dirty.MarkAll();
            else foreach(var area in changed)_dirty.Mark(DistrictDirtyGrid.Expand(area,8));
            var cells=_dirty.Consume();LastUpdatedChunkCount=cells.Count;
            if(cells.Count>0)_presentationDirty=true;
            uint seed=2166136261;foreach(var c in district.TileId??"")seed=unchecked((seed^c)*16777619);
            int columns=Mathf.CeilToInt(width/Spacing),rows=Mathf.CeilToInt(depth/Spacing);
            foreach(var key in cells)
            {
                if(_chunks.TryGetValue(key,out var old))
                {PatchCount-=old.Patches;old.Object.SetActive(false);Dispose(old.Object);Dispose(old.Mesh);_chunks.Remove(key);}
                BuildChunk(key,world,width,depth,seed,columns,rows);
            }
            if(district.Hills!=null && district.Hills.HeightMeters>0)
            {
                if(_hillOverlay==null){var hills=new GameObject("Hill Surface Detail");hills.transform.SetParent(transform,false);_hillOverlay=hills.AddComponent<DistrictHillGroundOverlay>();}
                _hillOverlay.PresentationEnabled = _presentationEnabled;
                _hillOverlay.Refresh(world,district,width,depth,initialize?null:changed);
            }
            else if(_hillOverlay!=null){Dispose(_hillOverlay.gameObject);_hillOverlay=null;}
            LateUpdate();
        }
        void BuildChunk(Vector2Int key,DistrictWorldController world,float width,float depth,uint seed,int columns,int rows)
        {
            int cx=key.x*CellsPerChunk,cz=key.y*CellsPerChunk,patches=0;
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
                    patches++;
                }
                if (vertices.Count == 0) return;
                var mesh = new Mesh { name = $"District Leaves {cx},{cz}", subMeshCount = 3 };
                mesh.SetVertices(vertices);
                mesh.SetUVs(0, uv);
                for (var i = 0; i < 3; i++) mesh.SetTriangles(triangles[i], i);
                mesh.RecalculateBounds();

                var chunk = new GameObject(mesh.name);
                chunk.transform.SetParent(transform, false);
                chunk.AddComponent<MeshFilter>().sharedMesh = mesh;
                var renderer = chunk.AddComponent<MeshRenderer>();
                renderer.sharedMaterials = _materials.ToArray();
                renderer.shadowCastingMode = ShadowCastingMode.Off;
                renderer.receiveShadows = false;
                _chunks[key]=new Chunk{Object=chunk,Mesh=mesh,Renderer=renderer,Patches=patches};
                PatchCount+=patches;
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
            bool enabled=_presentationEnabled && alpha>.001f;
            if(!_presentationDirty && tint==_lastTint && enabled==_lastEnabled)return;
            _presentationDirty=false;_lastTint=tint;_lastEnabled=enabled;
            foreach (var material in _materials) material.color = tint;
            foreach (var chunk in _chunks.Values)
                chunk.Renderer.enabled = enabled;
        }

        private void Release()
        {
            for (var i = transform.childCount - 1; i >= 0; i--)
            {
                var child = transform.GetChild(i).gameObject;
                child.SetActive(false);
                Dispose(child);
            }
            foreach (var chunk in _chunks.Values) Dispose(chunk.Mesh);
            foreach (var material in _materials) Dispose(material);
            _chunks.Clear(); _materials.Clear(); PatchCount = 0; _dirty=null; _hillOverlay=null;
        }
        private static void Dispose(Object value)
        {
            if (Application.isPlaying) Destroy(value); else DestroyImmediate(value);
        }
        private void OnDestroy() => Release();
    }
}

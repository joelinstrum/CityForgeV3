using UnityEngine;
using UnityEngine.Rendering;

namespace CityForgeV3.World
{
    /// <summary>Bounded distant-view cloud presentation. No simulation or persistence.</summary>
    public sealed class DistrictCloudLayer : MonoBehaviour
    {
        public const int CloudCount = 2;
        public const string TextureResource = "CityForgeV3/Weather/CloudsV01/cumulus";
        Mesh _cloudMesh;
        Material _cloudMaterial, _shadowMaterial;
        MeshFilter _terrain, _shadow;
        MeshRenderer _body;
        static readonly int CloudMotionId = Shader.PropertyToID("_CloudMotion");
        readonly Vector4[] _motion = new Vector4[CloudCount];
        float _width, _depth;
        int _weatherSeed;
        double _weatherEpoch;
        public static bool VisibleAt(DistrictZoomLevel zoom) => zoom == DistrictZoomLevel.LOD4 || zoom == DistrictZoomLevel.LOD5Billboard;
        public void SetZoom(DistrictZoomLevel zoom)
        {
            bool visible = VisibleAt(zoom);
            if (_body != null && _body.enabled != visible) _body.enabled = visible;
        }
        public void Initialize(float width, float depth, float terrainHeight, Quaternion cameraRotation, MeshFilter terrain)
        {
            _terrain = terrain;
            _width = width; _depth = depth;
            _weatherEpoch = Time.realtimeSinceStartupAsDouble;
            var texture = Resources.Load<Texture2D>(TextureResource);
            var shader = Shader.Find("CityForgeV3/DistantClouds");
            if (texture == null || shader == null) return;
            var cloudData = new Vector4[CloudCount];
            var vertices = new Vector3[CloudCount * 4];
            var uv = new Vector2[vertices.Length];
            var centers = new Vector2[vertices.Length];
            var colors = new Color[vertices.Length];
            var triangles = new int[CloudCount * 6];
            var right = cameraRotation * Vector3.right;
            var up = cameraRotation * Vector3.up;
            var positions = new[] { new Vector2(-.24f,-.16f), new Vector2(.16f,.22f) };
            _weatherSeed = new System.Random().Next(1, 10000);
            float basis = Mathf.Clamp(Mathf.Min(width,depth) * .32f, 100f, 650f);
            for (int i = 0; i < CloudCount; i++)
            {
                float size = basis * (.72f + (i * 7 % 5) * .095f);
                var center = new Vector3(positions[i].x * width, Mathf.Max(0,terrainHeight) + 140 + size * .25f, positions[i].y * depth);
                cloudData[i] = new Vector4(center.x,center.z,size,size*.8f);
                for (int k = 0; k < 4; k++)
                {
                    float x = k == 0 || k == 3 ? -.5f : .5f;
                    float y = k < 2 ? -.5f : .5f;
                    vertices[i*4+k] = center + right*x*size + up*y*size*.8f;
                    uv[i*4+k] = new Vector2(i%2 == 0 ? x+.5f : .5f-x,y+.5f);
                    centers[i*4+k] = new Vector2(center.x,center.z);
                    colors[i*4+k] = new Color(i%2,0,0,1);
                }
                int v=i*4,t=i*6;
                triangles[t]=v;triangles[t+1]=v+1;triangles[t+2]=v+2;
                triangles[t+3]=v;triangles[t+4]=v+2;triangles[t+5]=v+3;
            }
            _cloudMesh = new Mesh { name="Two distant cloud billboards", vertices=vertices, uv=uv, uv2=centers, colors=colors, triangles=triangles };
            // Shader drift is bounded by the district footprint. Avoid stale CPU bounds.
            _cloudMesh.bounds = new Bounds(new Vector3(0,terrainHeight+200,0),new Vector3(width*2,terrainHeight+1000,depth*2));
            Material Make(bool shadow)
            {
                var material = new Material(shader) { name=shadow?"Distant cloud ground shadows":"Distant cumulus", mainTexture=texture,
                    renderQueue=shadow?3005:3100 };
                material.SetFloat("_Shadow",shadow?1:0);
                
                material.SetTexture("_AlternateTex",Resources.Load<Texture2D>("CityForgeV3/Weather/CloudsV01/cumulus-wisps") ?? texture);
                material.SetVector("_DistrictSize",new Vector4(width,depth,0,0));
                material.SetVectorArray("_Clouds",cloudData);
                return material;
            }
            _cloudMaterial=Make(false);_shadowMaterial=Make(true);
            UpdateMotion(0);
            var body=new GameObject("Clouds — two batched billboards");body.transform.SetParent(transform,false);
            body.AddComponent<MeshFilter>().sharedMesh=_cloudMesh;
            var renderer=body.AddComponent<MeshRenderer>();_body=renderer;renderer.sharedMaterial=_cloudMaterial;
            renderer.shadowCastingMode=ShadowCastingMode.Off;renderer.receiveShadows=false;
            var shadowObject=new GameObject("Cloud shadows — shared terrain mesh");shadowObject.transform.SetParent(transform,false);
            _shadow=shadowObject.AddComponent<MeshFilter>();_shadow.sharedMesh=terrain.sharedMesh;
            var shadowRenderer=shadowObject.AddComponent<MeshRenderer>();shadowRenderer.sharedMaterial=_shadowMaterial;
            shadowRenderer.shadowCastingMode=ShadowCastingMode.Off;shadowRenderer.receiveShadows=false;
        }
        public void SetLighting(Color tint, bool night)
        {
            if (_cloudMaterial != null) _cloudMaterial.color = tint;
            if (_shadowMaterial != null) _shadowMaterial.SetFloat("_ShadowStrength",night?.035f:.13f);
        }
        void LateUpdate()
        {
            // Two weather slots only: cached uniform arrays, no allocations or mesh uploads.
            UpdateMotion(Time.realtimeSinceStartupAsDouble - _weatherEpoch);
            // Constant-time reference check, no terrain sampling or mesh upload.
            if (_terrain != null && _shadow != null && _shadow.sharedMesh != _terrain.sharedMesh)
                _shadow.sharedMesh = _terrain.sharedMesh;
        }
        void UpdateMotion(double elapsed)
        {
            for (int i=0; i<CloudCount; i++)
                _motion[i] = EvaluateMotion(elapsed, i, _weatherSeed, _width, _depth);
            if (_cloudMaterial != null) _cloudMaterial.SetVectorArray(CloudMotionId, _motion);
            if (_shadowMaterial != null) _shadowMaterial.SetVectorArray(CloudMotionId, _motion);
        }
        // xy = district position, z = opacity. Shared by cloud bodies and ground shadows.
        public static Vector4 EvaluateMotion(double elapsed, int slot, int seed, float width, float depth)
        {
            double period = slot == 0 ? 113 : 149;
            double clock = elapsed + seed + slot * 57;
            int cycle = (int)System.Math.Floor(clock / period);
            float age = (float)(clock - cycle * period);
            int key = cycle + slot * 193;
            float life = (float)period * Mathf.Lerp(.70f, .94f, Noise(key+7, seed));
            float presence = Mathf.SmoothStep(0,1,age/10f) *
                (1-Mathf.SmoothStep(0,1,(age-life+12)/12f));
            var start = new Vector2((Noise(key+11,seed)-.5f)*width*.65f,
                (Noise(key+29,seed)-.5f)*depth*.65f);
            // Preserve perceptible travel when the district spans kilometres.
            float scale = Mathf.Max(1,Mathf.Min(width,depth)/640f);
            var position = start + (age-life*.5f)*new Vector2(1.65f,.525f)*scale;
            float edge = Mathf.Max(Mathf.Abs(position.x)/width,Mathf.Abs(position.y)/depth);
            presence *= 1-Mathf.SmoothStep(0,1,(edge-.42f)/.23f);
            return new Vector4(position.x,position.y,presence,0);
        }
        static float Noise(int key, int seed)
        {
            unchecked
            {
                uint h=(uint)(key*374761393+seed*668265263);
                h=(h^(h>>13))*1274126177u;h^=h>>16;
                return (h & 0x00ffffff)/16777216f;
            }
        }
        void OnDestroy()
        {
            Release(_cloudMesh);Release(_cloudMaterial);Release(_shadowMaterial);
        }
        static void Release(Object value)
        {
            if(value==null)return;
            if(Application.isPlaying)Destroy(value);else DestroyImmediate(value);
        }
    }
}

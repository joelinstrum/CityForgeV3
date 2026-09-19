using UnityEngine;

namespace CityForgeV3.World
{
    // A single authored mesh and one foliage texture; no per-leaf geometry.
    public sealed class LowPolyBoxwoodHedge : MonoBehaviour
    {
        public const float LengthMeters = 3f;
        public const float DepthMeters = 1f;
        public const string ModelResource =
            "CityForgeV3/Garden/LowPolyBoxwoodHedgeV01/" +
            "CF_LowPolyBoxwoodHedge_3x1_v01";
        public const string FoliageResource =
            "CityForgeV3/Garden/LowPolyBoxwoodHedgeV01/boxwood-foliage-v01";

        private static readonly Color BoxwoodGreen = new(.245f, .345f, .18f);
        private static Material _opaqueMaterial;
        private static Material _previewMaterial;

        private static bool EnsureMaterials()
        {
            if (_opaqueMaterial != null && _previewMaterial != null) return true;
            var foliage = Resources.Load<Texture2D>(FoliageResource);
            if (foliage == null) return false;
            _opaqueMaterial = new Material(Shader.Find("Standard"))
            {
                name = "CF Low-Poly Boxwood Hedge",
                mainTexture = foliage,
                color = BoxwoodGreen,
                enableInstancing = true
            };
            _opaqueMaterial.SetFloat("_Metallic", 0f);
            _opaqueMaterial.SetFloat("_Glossiness", .08f);
            _previewMaterial = new Material(_opaqueMaterial)
            {
                name = "CF Low-Poly Boxwood Hedge Preview"
            };
            _previewMaterial.SetFloat("_Mode", 3f);
            _previewMaterial.SetInt("_SrcBlend",
                (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            _previewMaterial.SetInt("_DstBlend",
                (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            _previewMaterial.SetInt("_ZWrite", 0);
            _previewMaterial.EnableKeyword("_ALPHABLEND_ON");
            _previewMaterial.renderQueue = 3000;
            return true;
        }

        public static Transform Create(string name, float alpha)
        {
            var prefab = Resources.Load<GameObject>(ModelResource);
            if (prefab == null || !EnsureMaterials()) return null;

            var root = new GameObject(name).transform;
            var model = Instantiate(prefab, root, false).transform;
            model.name = "Low-Poly Boxwood Hedge Model";
            var renderers = model.GetComponentsInChildren<MeshRenderer>();
            if (renderers.Length == 0)
            {
                if (Application.isPlaying) Destroy(root.gameObject);
                else DestroyImmediate(root.gameObject);
                return null;
            }

            var bounds = renderers[0].bounds;
            for (var i = 1; i < renderers.Length; i++)
                bounds.Encapsulate(renderers[i].bounds);
            model.localPosition -= new Vector3(bounds.center.x, bounds.min.y,
                bounds.center.z);

            foreach (var renderer in renderers)
            {
                renderer.sharedMaterial = _opaqueMaterial;
                renderer.shadowCastingMode =
                    UnityEngine.Rendering.ShadowCastingMode.On;
                renderer.receiveShadows = true;
            }
            foreach (var collider in model.GetComponentsInChildren<Collider>())
                collider.enabled = false;
            var hedge = root.gameObject.AddComponent<LowPolyBoxwoodHedge>();
            hedge.SetOpacity(alpha);
            return root;
        }

        public void SetOpacity(float alpha)
        {
            if (!EnsureMaterials()) return;
            var color = BoxwoodGreen;
            color.a = Mathf.Clamp01(alpha);
            var transparent = color.a < .999f;
            var block = new MaterialPropertyBlock();
            if (transparent) block.SetColor("_Color", color);
            foreach (var renderer in GetComponentsInChildren<MeshRenderer>())
            {
                renderer.sharedMaterial = transparent
                    ? _previewMaterial : _opaqueMaterial;
                renderer.SetPropertyBlock(transparent ? block : null);
            }
        }

        private void OnEnable()
        {
            // Script reloads can keep an already-placed FBX alive with its old
            // material. Rebind locally so the new boxwood grade appears without
            // rebuilding or saving the Lot.
            if (!Application.isPlaying) return;
            var renderer = GetComponentInChildren<MeshRenderer>();
            if (renderer == null) return;
            var alpha = 1f;
            if (renderer.sharedMaterial != null &&
                renderer.sharedMaterial.renderQueue >= 3000)
            {
                var block = new MaterialPropertyBlock();
                renderer.GetPropertyBlock(block);
                alpha = block.GetColor("_Color").a;
            }
            SetOpacity(alpha);
        }
    }
}

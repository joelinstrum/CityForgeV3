using UnityEngine;

namespace CityForgeV3.World
{
    // The supplied FBX is kept intact; only its Lot presentation is scaled and grounded.
    public sealed class StoneGardenFountain : MonoBehaviour
    {
        public const float FootprintMeters = 3f;
        public const string ModelResource =
            "CityForgeV3/Garden/StoneFountainV01/Source/tripo_convert_e6cd835e-5f59-4760-86ae-6b70082f56f1";
        public const string AlbedoResource = ModelResource +
            ".fbm/tripo_image_e6cd835e_0";

        private Material _material;
        private StoneFountainWater _water;

        public static Transform Create(string name, float alpha)
        {
            var prefab = Resources.Load<GameObject>(ModelResource);
            var albedo = Resources.Load<Texture2D>(AlbedoResource);
            if (prefab == null || albedo == null) return null;

            var root = new GameObject(name).transform;
            var model = Instantiate(prefab, root, false).transform;
            model.name = "Stone Fountain Model";
            var renderers = model.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0)
            {
                if (Application.isPlaying) Destroy(root.gameObject);
                else DestroyImmediate(root.gameObject);
                return null;
            }

            var bounds = renderers[0].bounds;
            for (var i = 1; i < renderers.Length; i++)
                bounds.Encapsulate(renderers[i].bounds);
            var diameter = Mathf.Max(bounds.size.x, bounds.size.z);
            if (diameter > 0.001f)
                model.localScale *= FootprintMeters / diameter;
            bounds = renderers[0].bounds;
            for (var i = 1; i < renderers.Length; i++)
                bounds.Encapsulate(renderers[i].bounds);
            model.localPosition -= new Vector3(bounds.center.x, bounds.min.y,
                bounds.center.z);

            var shader = Shader.Find("Standard");
            var material = new Material(shader)
            {
                name = "CF Stone Garden Fountain",
                mainTexture = albedo
            };
            material.SetFloat("_Metallic", 0f);
            material.SetFloat("_Glossiness", 0.12f);
            foreach (var renderer in renderers)
            {
                renderer.sharedMaterial = material;
                renderer.shadowCastingMode =
                    UnityEngine.Rendering.ShadowCastingMode.On;
                renderer.receiveShadows = true;
            }
            foreach (var collider in model.GetComponentsInChildren<Collider>())
                collider.enabled = false;
            var fountain = root.gameObject.AddComponent<StoneGardenFountain>();
            fountain._material = material;
            fountain.EnsureWater(alpha);
            fountain.SetOpacity(alpha);
            return root;
        }

        private void OnEnable()
        {
            // A live Lot can retain its fountain object through a script
            // refresh. Reattach presentation-only water locally in that case.
            if (!Application.isPlaying ||
                transform.Find("Stone Fountain Model") == null) return;
            if (_material == null)
                _material = transform.Find("Stone Fountain Model")
                    .GetComponentInChildren<Renderer>()?.sharedMaterial;
            EnsureWater(_material != null ? _material.color.a : 1f);
        }

        private void EnsureWater(float opacity)
        {
            if (_water != null) return;
            var existing = transform.Find("Running fountain water");
            _water = existing != null
                ? existing.GetComponent<StoneFountainWater>()
                : StoneFountainWater.Create(transform, opacity);
        }

        public void SetOpacity(float alpha)
        {
            if (_material == null) return;
            alpha = Mathf.Clamp01(alpha);
            var color = Color.white;
            color.a = alpha;
            _material.color = color;
            _water?.SetOpacity(alpha);
            var transparent = alpha < 0.999f;
            _material.SetFloat("_Mode", transparent ? 3f : 0f);
            _material.SetInt("_SrcBlend", transparent
                ? (int)UnityEngine.Rendering.BlendMode.SrcAlpha
                : (int)UnityEngine.Rendering.BlendMode.One);
            _material.SetInt("_DstBlend", transparent
                ? (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha
                : (int)UnityEngine.Rendering.BlendMode.Zero);
            _material.SetInt("_ZWrite", transparent ? 0 : 1);
            if (transparent) _material.EnableKeyword("_ALPHABLEND_ON");
            else _material.DisableKeyword("_ALPHABLEND_ON");
            _material.renderQueue = transparent ? 3000 : -1;
        }

        private void OnDestroy()
        {
            if (_material == null) return;
            if (Application.isPlaying) Destroy(_material);
            else DestroyImmediate(_material);
        }
    }
}

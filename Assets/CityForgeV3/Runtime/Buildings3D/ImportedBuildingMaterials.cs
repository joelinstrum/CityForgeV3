using System.Collections.Generic;
using UnityEngine;
namespace CityForgeV3.Buildings3D
{
    // Default for native FBX buildings. Explicitly authored packages retain
    // their own contracts; opt out with runtimeProfile="authored-materials".
    public static class ImportedBuildingMaterials
    {
        private const string SharedBuildingShaderName =
            "CityForgeV3/Experimental3DBuildingPBR";

        public static void Prepare(Transform root,
            ICollection<Material> ownedMaterials,
            IDictionary<Material, Material> preparedMaterials = null)
        {
            if (root == null || ownedMaterials == null) return;
            var shader = Shader.Find(SharedBuildingShaderName);
            if (shader == null)
            {
                Debug.LogError("Missing shared native-building shader: " +
                    SharedBuildingShaderName);
                return;
            }

            // A LotWorldController supplies a cache whose lifetime matches its
            // rendered lot. Editor utilities may omit it and receive the old
            // one-call sharing behavior.
            var copies = preparedMaterials ??
                new Dictionary<Material, Material>();
            foreach (var renderer in root.GetComponentsInChildren<Renderer>(true))
            {
                var slots = renderer.sharedMaterials;
                bool changed = false;
                for (int i = 0; i < slots.Length; i++)
                {
                    var source = slots[i];
                    if (source == null || source.shader == null ||
                        source.shader.name != "Standard" ||
                        !IsOrdinaryOpaqueSurface(source)) continue;
                    if (!copies.TryGetValue(source, out var material))
                    {
                        material = new Material(source)
                        {
                            name = source.name + " — Building Matte"
                        };
                        material.shader = shader;
                        // Metallic=0 alone still permits dielectric white highlights
                        // and sky reflections. Imported roughness JPEGs are not
                        // Unity metallic/smoothness maps; don't infer that packing.
                        material.SetFloat("_Metallic", 0f);
                        material.DisableKeyword("_METALLICGLOSSMAP");
                        material.SetFloat("_GlossMapScale", 0.1f);
                        material.SetFloat("_Contrast", 1f);
                        material.SetFloat("_Saturation", 1f);
                        material.SetFloat("_Vibrance", 0f);
                        material.SetFloat("_AlbedoBoost", 1f);
                        material.SetFloat("_PaintEnabled", 0f);
                        material.SetFloat("_NightEmissionIntensity", 0f);
                        material.SetFloat("_ConstructionRevealHeight", 100000f);
                        material.DisableKeyword("_EMISSION");
                        material.DisableKeyword("_SPECGLOSSMAP");
                        material.DisableKeyword("_ALPHATEST_ON");
                        material.DisableKeyword("_ALPHABLEND_ON");
                        material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                        material.enableInstancing = true;
                        copies.Add(source, material);
                        ownedMaterials.Add(material);
                    }
                    slots[i] = material;
                    changed = true;
                }
                if (changed) renderer.sharedMaterials = slots;
            }
        }

        private static bool IsOrdinaryOpaqueSurface(Material material)
        {
            if (material.HasProperty("_Mode") &&
                material.GetFloat("_Mode") != 0f) return false;
            if (material.renderQueue >=
                (int)UnityEngine.Rendering.RenderQueue.Transparent)
                return false;
            if (!material.IsKeywordEnabled("_EMISSION")) return true;
            var emissionColor = material.HasProperty("_EmissionColor")
                ? material.GetColor("_EmissionColor") : Color.black;
            var emissionMap = material.HasProperty("_EmissionMap")
                ? material.GetTexture("_EmissionMap") : null;
            return emissionColor.maxColorComponent <= 0f && emissionMap == null;
        }
    }
}

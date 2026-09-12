using System.Collections.Generic;
using UnityEngine;
namespace CityForgeV3.Buildings3D
{
    // Default for native FBX buildings. Explicitly authored packages retain
    // their own contracts; opt out with runtimeProfile="authored-materials".
    public static class ImportedBuildingMaterials
    {
        public static void Prepare(Transform root, ICollection<Material> ownedMaterials)
        {
            var copies = new Dictionary<Material, Material>();
            foreach (var renderer in root.GetComponentsInChildren<Renderer>(true))
            {
                var slots = renderer.sharedMaterials;
                bool changed = false;
                for (int i = 0; i < slots.Length; i++)
                {
                    var source = slots[i];
                    if (source == null || source.shader == null ||
                        source.shader.name != "Standard") continue;
                    if (!copies.TryGetValue(source, out var material))
                    {
                        material = new Material(source) {name = source.name + " — Building Matte"};
                        // Metallic=0 alone still permits dielectric white highlights
                        // and sky reflections. Imported roughness JPEGs are not
                        // Unity metallic/smoothness maps; don't infer that packing.
                        material.SetFloat("_Metallic", 0f);
                        material.DisableKeyword("_METALLICGLOSSMAP");
                        material.SetFloat("_Glossiness", 0.1f);
                        material.SetFloat("_SpecularHighlights", 0f);
                        material.SetFloat("_GlossyReflections", 0f);
                        material.EnableKeyword("_SPECULARHIGHLIGHTS_OFF");
                        material.EnableKeyword("_GLOSSYREFLECTIONS_OFF");
                        copies.Add(source, material);
                        ownedMaterials.Add(material);
                    }
                    slots[i] = material;
                    changed = true;
                }
                if (changed) renderer.sharedMaterials = slots;
            }
        }
    }
}

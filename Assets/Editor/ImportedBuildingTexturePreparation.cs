using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using CityForgeV3.World;
using CityForgeV3.Buildings3D;

// Use actual material connections, not guesses based on numeric Tripo filenames.
[InitializeOnLoad]
public static class ImportedBuildingTexturePreparation
{
    static bool queued;
    static ImportedBuildingTexturePreparation()
    {
        EditorApplication.projectChanged += Schedule;
        Schedule();
    }
    static void Schedule()
    {
        if (queued) return;
        queued = true;
        EditorApplication.delayCall += Prepare;
    }
    static void Prepare()
    {
        queued = false;
        if (EditorApplication.isCompiling || EditorApplication.isUpdating) { Schedule(); return; }
        BuildingContentCatalog.InvalidateCache();
        var roles = new Dictionary<string, int>();
        foreach (var entry in BuildingContentCatalog.All)
        {
            if (entry.provider != "resources" || entry.materialMode != "embedded" ||
                entry.runtimeProfile == "authored-materials" || entry.runtimeProfile == "opaque-prop" ||
                !entry.modelResourcePath.StartsWith("CityForgeV3/Buildings3D/")) continue;
            var model = BuildingContentCatalog.LoadModel(entry);
            if (model == null || model.GetComponent<Building3DPackageInstance>() != null) continue;
            foreach (var renderer in model.GetComponentsInChildren<Renderer>(true))
            foreach (var material in renderer.sharedMaterials)
            {
                if (material == null || material.shader.name != "Standard") continue;
                Add(roles, material, "_MainTex", 1);
                Add(roles, material, "_EmissionMap", 1);
                Add(roles, material, "_BumpMap", 2);
                Add(roles, material, "_DetailNormalMap", 2);
                Add(roles, material, "_MetallicGlossMap", 4);
                Add(roles, material, "_OcclusionMap", 4);
            }
        }
        foreach (var pair in roles)
        {
            // A map shared between incompatible roles requires author review.
            if (pair.Value != 1 && pair.Value != 2 && pair.Value != 4) continue;
            var importer = AssetImporter.GetAtPath(pair.Key) as TextureImporter;
            if (importer == null) continue;
            var normal = pair.Value == 2;
            var srgb = pair.Value == 1;
            var type = normal ? TextureImporterType.NormalMap : TextureImporterType.Default;
            if (importer.textureType == type && importer.sRGBTexture == srgb) continue;
            importer.textureType = type;
            importer.sRGBTexture = srgb;
            importer.SaveAndReimport();
        }
    }
    static void Add(Dictionary<string,int> roles, Material material, string property, int role)
    {
        if (!material.HasProperty(property)) return;
        var texture = material.GetTexture(property);
        if (texture == null) return;
        var path = AssetDatabase.GetAssetPath(texture);
        if (string.IsNullOrEmpty(path) || !path.Contains("/Buildings3D/")) return;
        roles.TryGetValue(path, out var existing);
        roles[path] = existing | role;
    }
}

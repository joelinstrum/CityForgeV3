using System;
using System.Linq;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEditor;
using CityForgeV3.Buildings3D;
public static class ImportedBuildingMaterialQa
{
    [MenuItem("City Forge/QA/Check Automatic Building Materials")]
    static void Check()
    {
        const string path="Assets/CityForgeV3/Resources/CityForgeV3/Buildings3D/WoodenCottageV01/Source/tripo_convert_2cf00425-385f-4f01-a4dc-105d1ef72de7.fbx";
        var source=AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if(source==null)throw new Exception("Missing cottage");
        var instance=UnityEngine.Object.Instantiate(source);
        var owned=new List<Material>();
        try
        {
            var renderers=instance.GetComponentsInChildren<Renderer>(true);
            var original=renderers.Select(r=>r.sharedMaterials).ToArray();
            var materials=original.SelectMany(x=>x).Distinct().ToArray();
            var snapshots=materials.Select(m=>EditorJsonUtility.ToJson(m)).ToArray();
            ImportedBuildingMaterials.Prepare(instance.transform,owned);
            int checkedSlots=0;
            for(int r=0;r<renderers.Length;r++)
            for(int i=0;i<original[r].Length;i++)
            {
                var before=original[r][i];var after=renderers[r].sharedMaterials[i];
                if(before.shader.name!="Standard")continue;
                if(before==after)throw new Exception("Shared material mutated instead of cloned");
                if(before.color!=after.color || before.mainTexture!=after.mainTexture ||
                    before.mainTextureScale!=after.mainTextureScale || before.mainTextureOffset!=after.mainTextureOffset ||
                    before.GetTexture("_BumpMap")!=after.GetTexture("_BumpMap") ||
                    before.GetColor("_EmissionColor")!=after.GetColor("_EmissionColor"))
                    throw new Exception("Source texture/UV/normal/emission contract changed");
                if(after.GetFloat("_Metallic")!=0 || after.GetFloat("_SpecularHighlights")!=0 ||
                    after.GetFloat("_GlossyReflections")!=0 || !after.IsKeywordEnabled("_SPECULARHIGHLIGHTS_OFF") ||
                    !after.IsKeywordEnabled("_GLOSSYREFLECTIONS_OFF") || after.IsKeywordEnabled("_METALLICGLOSSMAP"))
                    throw new Exception("Matte defaults missing");
                checkedSlots++;
            }
            for(int i=0;i<materials.Length;i++)if(EditorJsonUtility.ToJson(materials[i])!=snapshots[i])throw new Exception("Source asset modified");
            if(checkedSlots==0)throw new Exception("No Standard material checked");
            var report=$"PASS: {checkedSlots} cottage material slots; cloned materials; original color/UV/normal/emission preserved; source assets unchanged; metallic/highlights/reflections disabled.\n";
            File.WriteAllText("/Users/joelinstrum/dev/CityForgeMCP/artifacts/buildings/wooden-cottage/v02-shared-materials/validation.txt",report);
            Debug.Log(report);
        }
        finally {UnityEngine.Object.DestroyImmediate(instance);foreach(var m in owned)UnityEngine.Object.DestroyImmediate(m);}
    }
}

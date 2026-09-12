#if UNITY_EDITOR
using System;
using System.IO;
using CityForgeV3.World;
using UnityEngine;
using UnityEditor;
public static class FarmCabinColorBuilder
{
    const string Root="Assets/CityForgeV3/Resources/CityForgeV3/Buildings3D/FarmCabinHybridV03/";
    [MenuItem("City Forge/QA/Build Farm Cabin Color Derivative")]
    static void Build()
    {
        var source=Resources.Load<GameObject>("CityForgeV3/Buildings3D/FarmCabinHybridV01/Source/tripo_convert_29314f7c-a5a1-4e1e-bb31-4fdb32c8c27d");
        var shader=Shader.Find("CityForgeV3/Buildings/Selective Matte Color");
        if(source==null||shader==null||!shader.isSupported||ShaderUtil.ShaderHasError(shader))throw new Exception("Source/shader missing or shader has errors");
        var instance=UnityEngine.Object.Instantiate(source);
        try
        {
            instance.name="Founders Farmhouse and Cabin — Color v03";
            var original=instance.GetComponentInChildren<Renderer>().sharedMaterial;
            var material=new Material(original){name="Farm Cabin Color v03",shader=shader};
            material.SetFloat("_Metallic",0);material.SetFloat("_Glossiness",.1f);
            material.SetFloat("_SpecularHighlights",0);material.SetFloat("_GlossyReflections",0);
            material.SetFloat("_GradeStrength",1);material.SetFloat("_RoofDarkening",.25f);material.SetFloat("_GreenDarkening",.18f);
            material.SetFloat("_Contrast",1.1f);material.SetFloat("_Saturation",1.08f);
            material.EnableKeyword("_NORMALMAP");
            var path=Root+"FarmCabinColor.mat";
            var existing=AssetDatabase.LoadAssetAtPath<Material>(path);
            if(existing==null)AssetDatabase.CreateAsset(material,path);
            else {EditorUtility.CopySerialized(material,existing);UnityEngine.Object.DestroyImmediate(material);material=existing;}
            foreach(var renderer in instance.GetComponentsInChildren<Renderer>())renderer.sharedMaterial=material;
            PrefabUtility.SaveAsPrefabAsset(instance,Root+"FarmCabinColor.prefab");
            AssetDatabase.SaveAssets();
            Directory.CreateDirectory("QA/FarmCabin");
            File.WriteAllText("QA/FarmCabin/color-derivative.txt",$"source={source.name}\nalbedo={material.mainTexture.name}\nshader={shader.name}\nshaderErrors={ShaderUtil.ShaderHasError(shader)}\n");
        }
        finally{UnityEngine.Object.DestroyImmediate(instance);}
    }
    [MenuItem("City Forge/QA/Toggle Farm Cabin Color Preview")]
    static void Toggle()
    {
        foreach(var renderer in UnityEngine.Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None))
        foreach(var m in renderer.sharedMaterials)
            if(m!=null && m.shader.name=="CityForgeV3/Buildings/Selective Matte Color")
            {
                var block=new MaterialPropertyBlock();renderer.GetPropertyBlock(block);
                block.SetFloat("_GradeStrength",block.HasFloat("_GradeStrength")&&block.GetFloat("_GradeStrength")==0?1:0);
                renderer.SetPropertyBlock(block);
            }
    }
}
#endif

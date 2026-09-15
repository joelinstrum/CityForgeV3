#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;
[InitializeOnLoad]
public static class CoalMineAssetBuilder
{
    private const string Root="Assets/CityForgeV3/Resources/CityForgeV3/Industry/CoalMineV01/";
    static CoalMineAssetBuilder(){EditorApplication.delayCall+=Ensure;}
    [MenuItem("City Forge/QA/Coal Mine/Prepare Model")]
    public static void Ensure()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode||EditorApplication.isCompiling)return;
        if(AssetDatabase.LoadAssetAtPath<GameObject>(Root+"CoalMine.prefab")!=null)return;
        var source=AssetDatabase.LoadAssetAtPath<GameObject>(Root+"Model.fbx");if(source==null)return;
        var ni=AssetImporter.GetAtPath(Root+"Normal.jpg") as TextureImporter;
        if(ni!=null&&ni.textureType!=TextureImporterType.NormalMap){ni.textureType=TextureImporterType.NormalMap;ni.sRGBTexture=false;ni.SaveAndReimport();}
        var material=new Material(Shader.Find("Standard")){name="Coal Mine Original Wood",color=Color.white};
        material.mainTexture=AssetDatabase.LoadAssetAtPath<Texture2D>(Root+"BaseColor.jpg");material.SetTexture("_BumpMap",AssetDatabase.LoadAssetAtPath<Texture2D>(Root+"Normal.jpg"));material.EnableKeyword("_NORMALMAP");
        material.SetFloat("_Metallic",0);material.SetFloat("_Glossiness",.1f);material.SetFloat("_SpecularHighlights",0);material.SetFloat("_GlossyReflections",0);material.EnableKeyword("_SPECULARHIGHLIGHTS_OFF");material.EnableKeyword("_GLOSSYREFLECTIONS_OFF");
        AssetDatabase.CreateAsset(material,Root+"Wood.mat");
        var root=new GameObject("Coal Mine");var model=(GameObject)PrefabUtility.InstantiatePrefab(source);model.transform.SetParent(root.transform,false);
        foreach(var r in model.GetComponentsInChildren<Renderer>()){var slots=r.sharedMaterials;for(int i=0;i<slots.Length;i++)slots[i]=material;r.sharedMaterials=slots;}
        var bounds=new Bounds();bool first=true;foreach(var r in root.GetComponentsInChildren<Renderer>()){if(first){bounds=r.bounds;first=false;}else bounds.Encapsulate(r.bounds);}
        if(Mathf.Abs(bounds.size.y-4)>0.05f){Object.DestroyImmediate(root);throw new System.Exception("Mine import metric height mismatch: "+bounds);}
        PrefabUtility.SaveAsPrefabAsset(root,Root+"CoalMine.prefab");Object.DestroyImmediate(root);AssetDatabase.SaveAssets();
        Debug.Log("COAL MINE ASSET ready bounds="+bounds+" front=local+Z terrain=rear-Z");
    }
}
#endif

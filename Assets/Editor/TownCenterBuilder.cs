#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CityForgeV3.Buildings3D;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

public static class TownCenterBuilder
{
    public const string Root = "Assets/CityForgeV3/Resources/CityForgeV3/Buildings3D/TownCenterV01";
    [MenuItem("City Forge/3D Buildings/Build Town Center")]
    public static void Build()
    {
        if (EditorApplication.isPlaying) throw new InvalidOperationException("Build outside Play mode.");
        foreach (var folder in new[] {"Materials", "Meshes", "Prefabs"})
            Directory.CreateDirectory(Root + "/" + folder);
        AssetDatabase.Refresh();
        var thumbnail = AssetImporter.GetAtPath(Root+"/thumbnail.png") as TextureImporter;
        if(thumbnail != null) { thumbnail.maxTextureSize=512;thumbnail.mipmapEnabled=false;thumbnail.SaveAndReimport(); }
        var importer = (ModelImporter)AssetImporter.GetAtPath(Root + "/Derived/TownCenter.fbx");
        importer.materialImportMode = ModelImporterMaterialImportMode.None;
        importer.importAnimation = false;
        importer.SaveAndReimport();
        var source = Root + "/Source/tripo_convert_bd63034a-224d-480d-8639-bfce1980cb54.fbm/";
        foreach (var path in Directory.GetFiles(source))
        {
            var ti = AssetImporter.GetAtPath(path) as TextureImporter;
            if (ti == null) continue;
            ti.maxTextureSize = 2048; ti.mipmapEnabled = true;
            ti.textureCompression = TextureImporterCompression.CompressedHQ;
            if (path.EndsWith("_2.png")) { ti.textureType = TextureImporterType.NormalMap; ti.sRGBTexture = false; }
            else ti.sRGBTexture = path.EndsWith("_0.jpg");
            ti.SaveAndReimport();
        }
        var wood = Mat("Wood", new Color(.34f,.26f,.17f));
        var iron = Mat("Iron", new Color(.045f,.05f,.045f));
        var room = Mat("Interior", new Color(.32f,.24f,.15f), true);
        var attic = Mat("AtticGlow", new Color(.035f,.04f,.035f), true);
        var lamp = Mat("LanternGlass", new Color(.28f,.3f,.26f), true);
        var shell = Mat("Shell", Color.white);
        var siding = Mat("RearSiding", Color.white);
        siding.mainTexture=AssetDatabase.LoadAssetAtPath<Texture2D>(Root+"/Derived/RearSiding.png");
        var stone = Mat("RearStone", Color.white);
        stone.mainTexture=AssetDatabase.LoadAssetAtPath<Texture2D>(Root+"/Derived/RearStone.png");
        shell.mainTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(source + "tripo_image_bd63034a_0.jpg");
        shell.SetTexture("_BumpMap", AssetDatabase.LoadAssetAtPath<Texture2D>(source + "tripo_image_bd63034a_2.png"));
        shell.EnableKeyword("_NORMALMAP"); shell.SetFloat("_BumpScale", .5f);
        var glass = Mat("Glass", new Color(.42f,.5f,.53f,.055f));
        glass.SetFloat("_Mode",3); glass.SetInt("_SrcBlend",(int)BlendMode.One);
        glass.SetInt("_DstBlend",(int)BlendMode.OneMinusSrcAlpha);glass.SetInt("_ZWrite",0);
        glass.EnableKeyword("_ALPHAPREMULTIPLY_ON");glass.renderQueue=3000;
        var actorMat = AssetDatabase.LoadAssetAtPath<Material>(Root + "/Materials/InteriorPeople.mat");
        if (actorMat == null) { actorMat=new Material(Shader.Find("CityForge/Interior Automata")); AssetDatabase.CreateAsset(actorMat,Root+"/Materials/InteriorPeople.mat"); }
        var visual = UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(Root + "/Derived/TownCenter.fbx"));
        visual.name = "Town Center Visual";
        try
        {
            // Bake FBX coordinate transforms so geometry, windows and room
            // animation all use one metre-based, foundation-centred contract.
            var renderers = visual.GetComponentsInChildren<MeshRenderer>();
            var mats = new Dictionary<string, Material> { {"TC_Shell",shell}, {"TC_Wood",wood}, {"TC_Interior",room}, {"TC_Glass",glass}, {"TC_AtticGlow",attic}, {"TC_LanternGlass",lamp}, {"TC_Iron",iron}, {"TC_RearSiding",siding}, {"TC_RearStone",stone} };
            foreach (var renderer in renderers)
            {
                var mf=renderer.GetComponent<MeshFilter>();
                var mesh=UnityEngine.Object.Instantiate(mf.sharedMesh);
                var matrix=visual.transform.worldToLocalMatrix * renderer.transform.localToWorldMatrix;
                mesh.vertices=mesh.vertices.Select(matrix.MultiplyPoint3x4).ToArray();
                var normals=matrix.inverse.transpose;
                mesh.normals=mesh.normals.Select(n=>normals.MultiplyVector(n).normalized).ToArray();
                mesh.tangents=mesh.tangents.Select(t=> { var v=matrix.MultiplyVector(new Vector3(t.x,t.y,t.z)).normalized; return new Vector4(v.x,v.y,v.z,t.w * (matrix.determinant < 0 ? -1 : 1)); }).ToArray();
                mesh.RecalculateBounds();mesh.name=renderer.name;
                var path=Root+"/Meshes/"+renderer.name+".asset";
                var saved=AssetDatabase.LoadAssetAtPath<Mesh>(path);
                if (saved==null) { AssetDatabase.CreateAsset(mesh,path);saved=mesh; }
                else { EditorUtility.CopySerialized(mesh,saved); UnityEngine.Object.DestroyImmediate(mesh); }
                mf.sharedMesh=saved;
                renderer.transform.SetParent(visual.transform,false);
                renderer.transform.localPosition=Vector3.zero;renderer.transform.localRotation=Quaternion.identity;renderer.transform.localScale=Vector3.one;
                renderer.sharedMaterials=Enumerable.Repeat(mats[renderer.name],saved.subMeshCount).ToArray();
            }
            var controls=new List<BuildingNightLighting>();
            BuildingNightLighting Control(string name, Renderer target, Color color, float intensity)
            {
                var node=new GameObject(name);node.transform.SetParent(visual.transform,false);
                var ctl=node.AddComponent<BuildingNightLighting>();
                ctl.ConfigurePane(target,0,color);ctl.ConfigureOccupancy(1,false);
                ctl.ConfigureTuning(intensity,0,0,0,0);ctl.SetNightAmount(0);controls.Add(ctl);return ctl;
            }
            var warm=new Color(1f,.55f,.24f);
            Control("Warm interior",renderers.Single(r=>r.name=="TC_Interior"),warm,2f);
            Control("Attic windows",renderers.Single(r=>r.name=="TC_AtticGlow"),warm,2.4f);
            var lamps=Control("Porch lanterns",renderers.Single(r=>r.name=="TC_LanternGlass"),warm,2.8f);
            var positions=new[] { new Vector3(-.14f,3.52f,4.55f), new Vector3(6.84f,6.02f,-2.91f), new Vector3(5.28f,3.1f,-2.86f), new Vector3(-5.30f,3.30f,4.12f), new Vector3(4.50f,3.30f,4.12f) };
            var anchors=new List<Transform>();
            foreach (var p in positions) { var a=new GameObject("Lantern spill").transform;a.SetParent(visual.transform,false);a.localPosition=new Vector3(-p.x,p.y,p.z);anchors.Add(a); }
            lamps.ConfigureAnchors(null,anchors);lamps.ConfigureTuning(2.8f,0,0,3.2f,3f);lamps.ConfigurePerPixel(true);lamps.SetNightAmount(0);
            var art=new GameObject("Interior strolling couple").AddComponent<SpriteRenderer>();
            art.transform.SetParent(visual.transform,false);
            art.transform.localPosition=new Vector3(.45f,4.9f,.7f);
            art.sharedMaterial=actorMat;art.shadowCastingMode=ShadowCastingMode.Off;art.enabled=false;
            Control("People room fill",art,new Color(.72f,.48f,.27f),.85f);
            var life=visual.AddComponent<BuildingInteriorAutomata>();
            life.Configure(art,renderers.Single(r=>r.name=="TC_Shell"),controls.ToArray());
            var prefab=PrefabUtility.SaveAsPrefabAsset(visual,Root+"/Prefabs/TownCenterVisual.prefab");
            var far=UnityEngine.Object.Instantiate(visual);
            GameObject farPrefab;
            try
            {
                far.name="Town Center Distant";
                var farSource=AssetDatabase.LoadAssetAtPath<GameObject>(Root+"/Derived/TownCenterFarShell.fbx");
                var sourceFilter=farSource.GetComponentInChildren<MeshFilter>();
                var reduced=UnityEngine.Object.Instantiate(sourceFilter.sharedMesh);
                var matrix=farSource.transform.worldToLocalMatrix * sourceFilter.transform.localToWorldMatrix;
                reduced.vertices=reduced.vertices.Select(matrix.MultiplyPoint3x4).ToArray();
                var nm=matrix.inverse.transpose;reduced.normals=reduced.normals.Select(n=>nm.MultiplyVector(n).normalized).ToArray();
                reduced.tangents=reduced.tangents.Select(t=> { var v=matrix.MultiplyVector(new Vector3(t.x,t.y,t.z)).normalized;return new Vector4(v.x,v.y,v.z,t.w*(matrix.determinant<0?-1:1)); }).ToArray();
                reduced.RecalculateBounds();
                var reducedPath=Root+"/Meshes/DistantShell.asset";
                var saved=AssetDatabase.LoadAssetAtPath<Mesh>(reducedPath);
                if(saved==null) { AssetDatabase.CreateAsset(reduced,reducedPath);saved=reduced; }
                else { EditorUtility.CopySerialized(reduced,saved);UnityEngine.Object.DestroyImmediate(reduced); }
                far.GetComponentsInChildren<MeshFilter>().Single(m=>m.name=="TC_Shell").sharedMesh=saved;
                UnityEngine.Object.DestroyImmediate(far.GetComponent<BuildingInteriorAutomata>());
                UnityEngine.Object.DestroyImmediate(far.transform.Find("Interior strolling couple").gameObject);
                UnityEngine.Object.DestroyImmediate(far.transform.Find("People room fill").gameObject);
                foreach(var ctl in far.GetComponentsInChildren<BuildingNightLighting>())ctl.ConfigureAnchors(null,null);
                foreach(var light in far.GetComponentsInChildren<Light>(true))UnityEngine.Object.DestroyImmediate(light.gameObject);
                farPrefab=PrefabUtility.SaveAsPrefabAsset(far,Root+"/Prefabs/TownCenterDistant.prefab");
            }
            finally { UnityEngine.Object.DestroyImmediate(far); }
            var package=AssetDatabase.LoadAssetAtPath<Building3DPackage>(Root+"/TownCenter.asset");
            if(package==null) { package=ScriptableObject.CreateInstance<Building3DPackage>();AssetDatabase.CreateAsset(package,Root+"/TownCenter.asset"); }
            package.AssetId="town-center-v01";package.AuthoredScale=Vector3.one;package.FrontYawDegrees=180;
            package.FootprintMeters=new Vector2(14.2f,11.64f);package.UseCrossFade=false;
            package.SourceProvenance="Joe's hollow-window Tripo model; Blender rear wall, interior, sashes and lantern derivative. Shared strolling Automata.";
            package.Representations=new List<Building3DRepresentation> {
                new Building3DRepresentation { Level=Building3DLevel.LOD0,ScreenRelativeHeight=.18f,VisualPrefab=prefab,LocalScale=Vector3.one,TargetTriangleBudget=renderers.Sum(r=>r.GetComponent<MeshFilter>().sharedMesh.triangles.Length/3) },
                new Building3DRepresentation { Level=Building3DLevel.LOD3,ScreenRelativeHeight=.001f,VisualPrefab=farPrefab,LocalScale=Vector3.one,TargetTriangleBudget=farPrefab.GetComponentsInChildren<MeshFilter>().Sum(m=>m.sharedMesh.triangles.Length/3) }
            };
            EditorUtility.SetDirty(package);
            var root=new GameObject("Town Center");
            try { root.AddComponent<Building3DPackageInstance>().Configure(package);PrefabUtility.SaveAsPrefabAsset(root,Root+"/Prefabs/TownCenter.prefab"); }
            finally { UnityEngine.Object.DestroyImmediate(root); }
            foreach(var m in mats.Values)EditorUtility.SetDirty(m);
            AssetDatabase.SaveAssets();
        }
        finally { UnityEngine.Object.DestroyImmediate(visual); }
    }

    static Material Mat(string name, Color color, bool emission=false)
    {
        var path=Root+"/Materials/"+name+".mat";
        var m=AssetDatabase.LoadAssetAtPath<Material>(path);
        if(m==null) { m=new Material(Shader.Find("Standard"));AssetDatabase.CreateAsset(m,path); }
        m.color=color;m.SetFloat("_Metallic",0);m.SetFloat("_Glossiness",.1f);
        m.SetFloat("_SpecularHighlights",0);m.SetFloat("_GlossyReflections",0);
        m.EnableKeyword("_SPECULARHIGHLIGHTS_OFF");m.EnableKeyword("_GLOSSYREFLECTIONS_OFF");
        m.enableInstancing=true;
        if(emission) { m.EnableKeyword("_EMISSION");m.SetColor("_EmissionColor",new Color(.001f,.001f,.001f));m.globalIlluminationFlags=MaterialGlobalIlluminationFlags.RealtimeEmissive; }
        return m;
    }
}
#endif

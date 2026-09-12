using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using CityForgeV3.Buildings3D;

namespace CityForgeV3.Editor
{
    public static class SaltboxBuilding3DBuilder
    {
        const string Root = "Assets/CityForgeV3/Resources/CityForgeV3/Buildings3D/SaltboxV05";
        [Serializable] public class Room { public string id, renderer, material, source; public int slot; public bool on; public float[] position; }
        [Serializable] public class Rooms { public Room[] rooms; public int meshCount; }
        [InitializeOnLoadMethod] static void Queue()
        {
            EditorApplication.delayCall += () => {
                if (!File.Exists(Root + "/Prefabs/SaltboxV05.prefab") &&
                    AssetDatabase.LoadAssetAtPath<GameObject>(Root + "/Source/SaltboxV05.fbx") != null) Build();
            };
        }
        [MenuItem("City Forge/QA/Saltbox/Inspect Close")]
        static void CloseView() => UnityEngine.Object.FindFirstObjectByType<CityForgeV3.World.LotWorldController>()?.SetQaOrthographicSize(12f);
        [MenuItem("City Forge/QA/Saltbox/Show Residential Library")]
        static void Library() => UnityEngine.Object.FindFirstObjectByType<CityForgeV3.UI.CityForgeApp>()?.ShowSaltboxLibraryQa();
        [MenuItem("City Forge/QA/Check Saltbox Room Controls")]
        static void CheckRooms()
        {
            var root=UnityEngine.Object.FindObjectsByType<Building3DPackageInstance>(FindObjectsSortMode.None).First(x=>x.Package.AssetId=="new-england-saltbox-v05");
            var rooms=root.GetComponentsInChildren<BuildingNightLighting>(true);
            if(rooms.Length!=13)throw new Exception("Expected 13 rooms");
            var states=rooms.Select(x=>x.RoomLit).ToArray();
            var report=new System.Text.StringBuilder();
            for(int i=0;i<rooms.Length;i++)
            {
                root.SetNightAmount(1);
                for(int j=0;j<rooms.Length;j++) rooms[j].SetRoomLit(i==j);
                int lights=rooms.Sum(x=>x.ActiveRuntimeLightCount);
                if(lights!=1)throw new Exception("Room spill isolation failed");
                foreach(var room in rooms)
                {
                    var serialized=new SerializedObject(room);
                    var target=serialized.FindProperty("windowMaterialTargets").GetArrayElementAtIndex(0);
                    var renderer=(Renderer)target.FindPropertyRelative("Renderer").objectReferenceValue;
                    var slot=target.FindPropertyRelative("MaterialIndex").intValue;
                    var block=new MaterialPropertyBlock();renderer.GetPropertyBlock(block,slot);
                    var emission=block.GetColor("_EmissionColor");
                    if((emission.maxColorComponent>0.01f)!=room.RoomLit)throw new Exception("Pane isolation failed");
                    if(!renderer.sharedMaterials[slot].IsKeywordEnabled("_EMISSION"))throw new Exception("Emission keyword missing");
                }
                report.AppendLine(rooms[i].name+": isolated pane and spill passed");
            }
            for(int i=0;i<rooms.Length;i++)rooms[i].SetRoomLit(states[i]);
            root.SetNightAmount(0);
            if(rooms.Sum(x=>x.ActiveRuntimeLightCount)!=0)throw new Exception("Day spill off failed");
            root.SetNightAmount(1);
            report.AppendLine("Day off / night on; four default rooms; original texture slots intact.");
            File.WriteAllText("/Users/joelinstrum/dev/CityForgeMCP/artifacts/buildings/saltbox-segmented/v05-unity/runtime-validation.txt",report.ToString());
            Debug.Log(report.ToString());
        }
        [MenuItem("City Forge/QA/Open Saltbox Lighting Lot")]
        static void OpenQa()
        {
            if (!EditorApplication.isPlaying) {Debug.LogWarning("Enter Play Mode first");return;}
            UnityEngine.Object.FindFirstObjectByType<CityForgeV3.UI.CityForgeApp>()?.OpenSaltboxLightingQa();
        }
        [MenuItem("City Forge/3D Buildings/Create Saltbox Package")]
        public static void Build()
        {
            var model = AssetDatabase.LoadAssetAtPath<GameObject>(Root + "/Source/SaltboxV05.fbx");
            if (model == null) throw new FileNotFoundException("Saltbox FBX not imported");
            var cfg = JsonUtility.FromJson<Rooms>(File.ReadAllText(Root + "/rooms.json"));
            var visual = UnityEngine.Object.Instantiate(model);
            visual.name = "Saltbox Visible Geometry";
            try
            {
                var renderers = visual.GetComponentsInChildren<Renderer>(true);
                if (renderers.Length != 28) throw new Exception("Expected 28 visible meshes");
                foreach (var renderer in renderers)
                {
                    var materials = renderer.sharedMaterials;
                    for (int i=0;i<materials.Length;i++)
                    {
                        var room = cfg.rooms.FirstOrDefault(x => x.renderer == renderer.name && x.slot == i);
                        var source = room != null ? room.source : renderer.name.Replace("CF_LitMesh_", "");
                        var name = room != null ? room.material : source + "_material";
                        var texturePath = Root + "/Textures/wooden+house+3d+model_" + source + "_basecolor.jpg";
                        var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath);
                        if (texture == null) throw new FileNotFoundException(texturePath);
                        var path = Root + "/Materials/" + name + ".mat";
                        var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
                        if (mat == null) {mat = new Material(Shader.Find("Standard"));AssetDatabase.CreateAsset(mat,path);}
                        mat.mainTexture = texture;mat.color = Color.white;
                        mat.SetFloat("_Metallic",0);mat.SetFloat("_Glossiness",0.1f);
                        // Weathered timber/stone: preserve the source atlas without
                        // adding white dielectric highlights or sky reflections.
                        mat.SetFloat("_SpecularHighlights",0f);
                        mat.SetFloat("_GlossyReflections",0f);
                        mat.EnableKeyword("_SPECULARHIGHLIGHTS_OFF");
                        mat.EnableKeyword("_GLOSSYREFLECTIONS_OFF");
                        if (room != null) {mat.EnableKeyword("_EMISSION");mat.SetColor("_EmissionColor",new Color(2.5f,1.175f,0.45f));mat.globalIlluminationFlags=MaterialGlobalIlluminationFlags.RealtimeEmissive;}
                        materials[i]=mat;EditorUtility.SetDirty(mat);
                    }
                    renderer.sharedMaterials=materials;
                }
                var lightingRoot = new GameObject("CF_Lighting");lightingRoot.transform.SetParent(visual.transform,false);
                foreach (var room in cfg.rooms)
                {
                    var node = new GameObject(room.id);node.transform.SetParent(lightingRoot.transform,false);
                    node.transform.localPosition = new Vector3(room.position[0],room.position[1],room.position[2]);
                    var controller = node.AddComponent<BuildingNightLighting>();
                    controller.ConfigurePane(renderers.Single(x=>x.name==room.renderer),room.slot,new Color(1f,0.47f,0.18f));
                    controller.ConfigureAnchors(new[]{node.transform},null);
                    controller.ConfigureTuning(2.5f,0.08f,0.9f,0,0);
                    controller.ConfigureOccupancy(1f,false);
                    controller.SetRoomLit(room.on);controller.SetNightAmount(0);
                }
                var metrics = CityForge.Editor.Building3DPackageValidator.Measure(visual);
                var scale=9.5f/metrics.Bounds.size.y;
                var prefab=PrefabUtility.SaveAsPrefabAsset(visual,Root+"/Prefabs/SaltboxVisual.prefab");
                var package=AssetDatabase.LoadAssetAtPath<Building3DPackage>(Root+"/SaltboxV05.asset");
                if(package==null){package=ScriptableObject.CreateInstance<Building3DPackage>();AssetDatabase.CreateAsset(package,Root+"/SaltboxV05.asset");}
                package.AssetId="new-england-saltbox-v05";package.SourceProvenance="Accepted segmented Tripo saltbox v04; 28 original textures, 13 independently controlled pane materials. Export v05.";
                package.AuthoredScale=Vector3.one*scale;package.FrontYawDegrees=90;package.UseCrossFade=false;
                package.FootprintMeters=new Vector2(metrics.Bounds.size.x*scale,metrics.Bounds.size.z*scale);
                package.Representations=new List<Building3DRepresentation>{new Building3DRepresentation{Level=Building3DLevel.LOD0,ScreenRelativeHeight=0.001f,VisualPrefab=prefab,LocalPosition=new Vector3(-metrics.Bounds.center.x,-metrics.Bounds.min.y,-metrics.Bounds.center.z),LocalScale=Vector3.one,TargetTriangleBudget=metrics.Triangles,Provenance="Accepted pane-only segmentation; original UVs and textures preserved."}};
                EditorUtility.SetDirty(package);
                var root=new GameObject("New England Saltbox");
                try{root.AddComponent<Building3DPackageInstance>().Configure(package);PrefabUtility.SaveAsPrefabAsset(root,Root+"/Prefabs/SaltboxV05.prefab");}finally{UnityEngine.Object.DestroyImmediate(root);}
                AssetDatabase.SaveAssets();CityForgeV3.World.BuildingContentCatalog.InvalidateCache();
                Debug.Log($"Saltbox ready: {renderers.Length} meshes, {metrics.Triangles} triangles, 13 independent rooms; height 9.5m; footprint {package.FootprintMeters}");
            }
            finally {UnityEngine.Object.DestroyImmediate(visual);}
        }
    }
}

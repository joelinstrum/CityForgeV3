#if UNITY_EDITOR
using System.IO;
using CityForgeV3.UI;
using CityForgeV3.World;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using Object=UnityEngine.Object;
public static class RiverNormalsQa
{
    [MenuItem("City Forge/QA/River/Check Bank Lighting Normals")]
    static void Check()
    {
        if(!EditorApplication.isPlaying)return;
        var app=Object.FindFirstObjectByType<CityForgeApp>();app.OpenGeneratedRiverQa();
        app.GetComponent<UIDocument>().rootVisualElement.schedule.Execute(()=>
        {
            var world=Object.FindFirstObjectByType<DistrictWorldController>();
            var banks=0;var down=0;var lowest=1f;var centers=0;var centerError=0f;
            foreach(var f in world.GetComponentsInChildren<MeshFilter>())
            {
                if(!f.name.StartsWith("Riverbed "))continue;
                banks++;
                foreach(var n in f.sharedMesh.normals)
                {
                    lowest=Mathf.Min(lowest,n.y);if(n.y<0)down++;
                    if(f.name.Contains("Broad Center")){centers++;centerError=Mathf.Max(centerError,Vector3.Distance(n,Vector3.up));}
                }
            }
            Directory.CreateDirectory("QA/RiverNormals");
            File.WriteAllText("QA/RiverNormals/checks.txt",$"bankMeshes={banks} downwardNormals={down} minimumNormalY={lowest} flatCenterNormals={centers} centerNormalError={centerError}\n");
            if(down!=0||centers==0||centerError>.0001f)throw new System.Exception("River bank normal check failed");
            world.SetZoom(DistrictZoomLevel.LOD1);
        }).StartingIn(500);
    }
}
#endif

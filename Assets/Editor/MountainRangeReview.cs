#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using CityForgeV3.World;
using CityForgeV3.UI;
public static class MountainRangeReview
{
    static MountainRangeReview(){EditorApplication.update+=Poll;}
    static int Islands(DistrictElevation h)
    {
        var seen=new bool[h.Heights.Length];int count=0,w=h.Columns+1;
        for(int i=0;i<seen.Length;i++){
            if(seen[i]||h.Heights[i]<5)continue;count++;var q=new Queue<int>();q.Enqueue(i);seen[i]=true;
            while(q.Count>0){int p=q.Dequeue();foreach(int n in new[]{p-w,p+w,p%w>0?p-1:-1,p%w<w-1?p+1:-1})
                if(n>=0&&n<seen.Length&&!seen[n]&&h.Heights[n]>=5){seen[n]=true;q.Enqueue(n);}}
        }return count;
    }
    static void Poll(){const string path="/tmp/cityforge-mountain-range-command.txt";if(EditorApplication.isCompiling||!File.Exists(path))return;var c=File.ReadAllText(path).Trim();File.Delete(path);try{
        var app=UnityEngine.Object.FindFirstObjectByType<CityForgeApp>();string result="OK "+c;
        if(c=="migrate")app.UpgradeMountainRangeQa();
        else if(c=="wide")app.MountainRangeOverviewQa();
        else if(c=="reload")app.CheckMineQa();
        else if(c=="check"){
            var d=new RegionCityTile{Hills=new(){Mountains=true,Version=2,HeightMeters=180,Coverage=.45f,Seed=1209}};
            var terrain=new DistrictElevation(d);var roundtrip=new DistrictElevation(JsonUtility.FromJson<RegionCityTile>(JsonUtility.ToJson(d)));
            if(!terrain.Heights.SequenceEqual(roundtrip.Heights))throw new Exception("Non deterministic range");
            int connected=Islands(terrain);d.Hills.Version=1;int cones=Islands(new DistrictElevation(d));
            if(connected!=1||connected>=cones)throw new Exception("Disconnected range "+connected+" legacy="+cones);
            d.Hills.Mountains=false;var gentle=new DistrictElevation(d);d.Hills.Version=2;
            if(!gentle.Heights.SequenceEqual(new DistrictElevation(d).Heights))throw new Exception("Gentle terrain changed");
            var mesh=terrain.CreateMesh();try{foreach(var v in mesh.vertices)if(Mathf.Abs(terrain.Sample(v.x,v.z)-v.y)>.001f)throw new Exception("Mesh sampler mismatch");}finally{UnityEngine.Object.Destroy(mesh);}
            var shader=Shader.Find("CityForgeV3/MountainGroundSurfaceV03");if(shader==null||ShaderUtil.ShaderHasError(shader))throw new Exception("Shader compile failed");
            result="PASS deterministic=true connectedIslands="+connected+" legacyIslands="+cones+" gentleUnchanged=true meshSampler=true shader=true peak="+terrain.Heights.Max();
        }else throw new Exception("Unknown "+c);
        File.WriteAllText("/tmp/cityforge-mountain-range-result.txt",result);
    }catch(Exception e){File.WriteAllText("/tmp/cityforge-mountain-range-result.txt",e.ToString());Debug.LogException(e);}}
}
#endif

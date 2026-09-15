#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;
using CityForgeV3.World;
[InitializeOnLoad]public static class MineInsetReview
{
    static MineInsetReview(){EditorApplication.update+=Poll;}
    static void Poll(){const string file="/tmp/cityforge-mine-inset-command.txt";if(EditorApplication.isCompiling||!File.Exists(file))return;var command=File.ReadAllText(file).Trim();File.Delete(file);try{
        var mine=UnityEngine.Object.FindFirstObjectByType<DistrictCoalMinePresentation>();if(mine==null)throw new Exception("Mine missing");
        var world=mine.GetComponentInParent<DistrictWorldController>();
        if(command=="close")world.WorldCamera.orthographicSize=12;
        var lines=new System.Collections.Generic.List<string>{"approachVertices="+mine.ApproachVertices+" inset="+mine.InsetMeters+" rearBurial="+mine.RearBurialMeters+" entryGround="+mine.EntranceGroundMeters+" position="+mine.transform.localPosition};
        foreach(var r in mine.GetComponentsInChildren<MeshRenderer>())lines.Add(r.name+" bounds="+r.bounds+" queue="+r.sharedMaterial.renderQueue);
        // Inspect local height profile through the actual mine pivot for seating review.
        var p=mine.transform.localPosition;var f=mine.transform.localRotation*Vector3.forward;
        for(int z=-5;z<=5;z++)lines.Add("localZ="+z+" groundAboveFloor="+(world.TerrainElevation(p.x+f.x*z,p.z+f.z*z)-p.y));
        if(command=="check"){
            if(mine.RearBurialMeters<3.5f||mine.EntranceGroundMeters>.08f)throw new Exception("Seating gate failed: "+lines[0]);
            if(mine.GetComponentsInChildren<MeshRenderer>().Any(r=>r.sharedMaterial.renderQueue!=1998))throw new Exception("Wrong depth composition");
            lines.Add("PASS rear embedded, entry exposed, mine before terrain");
        }
        File.WriteAllLines("/tmp/cityforge-mine-inset-result.txt",lines);
    }catch(Exception e){File.WriteAllText("/tmp/cityforge-mine-inset-result.txt",e.ToString());}}
}
#endif

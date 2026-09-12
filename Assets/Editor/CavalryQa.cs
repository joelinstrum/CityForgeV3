#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using CityForgeV3.UI;
using CityForgeV3.World;
using UnityEditor;
using UnityEngine;
using Object=UnityEngine.Object;
public static class CavalryQa
{
    const BindingFlags F=BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic;
    const string Resource="CityForgeV3/Props/Animals/CavalryV01/Cavalry_Walk_Trot_Idle_v01";
    static LotWorldController world;
    static CityForgeApp app;
    static Transform root,rider;
    static Transform[] hooves;
    static Vector3[] baseline;
    static Vector2 destination;
    static float start,minRider,maxRider,hoofMotion;
    static bool walked;
    static object Call(object o,string name,params object[] args)=>o.GetType().GetMethod(name,F).Invoke(o,args);
    static void Fixture()
    {
        typeof(CarriageDriveQa).GetMethod("Load",BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,null);
        world=Object.FindFirstObjectByType<LotWorldController>();app=Object.FindFirstObjectByType<CityForgeApp>();
        var index=world.Session.Data.Props.FindIndex(p=>p.PropId==LotWorldController.HorseCarriagePropId);
        var p=world.Session.Data.Props[index];p.PropId=LotWorldController.CavalryPropId;p.AnimationState="idle";p.PositionX=25f;p.PositionZ=-20f;p.RotationQuarterTurns=0;p.MovementX=p.MovementZ=0;
        Call(world,"RebuildPropPresentations");
        typeof(LotWorldController).GetProperty("SelectedPropIndex",F).SetValue(world,index);
        typeof(LotWorldController).GetProperty("ActiveObjectSelection",F).SetValue(world,LotObjectSelectionKind.Prop);
        var roots=(System.Collections.IList)world.GetType().GetField("_propPresentations",F).GetValue(world);root=(Transform)roots[index];
        var camera=(Camera)world.GetType().GetField("_camera",F).GetValue(world);
        var shift=camera.transform.right*(world.ZoomLevel==LotZoomLevel.Detail?-LotWorldController.DetailInspectorClearanceMeters:LotWorldController.CameraFramingOffsetMeters(world.LotSizeMeters));
        world.SetQaCameraPan(root.localPosition.x+shift.x,root.localPosition.z+1.4f+shift.z);world.SetQaOrthographicSize(4f);
        Call(app,"RefreshLotEditor");
    }
    [MenuItem("City Forge/QA/Cavalry/Open Mounted Rider")]
    static void Open(){if(EditorApplication.isPlaying)Fixture();}
    [MenuItem("City Forge/QA/Cavalry/Check Imported Gaits")]
    static void Check()
    {
        Directory.CreateDirectory("QA/Cavalry");var model=Object.Instantiate(Resources.Load<GameObject>(Resource));model.SetActive(false);
        var bones=model.GetComponentsInChildren<Transform>(true);var feet=bones.Where(t=>t.name.StartsWith("Hoof.")).ToArray();var seat=bones.First(t=>t.name=="RIDER");var report="";
        foreach(var clip in Resources.LoadAll<AnimationClip>(Resource).Where(c=>!c.name.StartsWith("__preview__")))
        {
            clip.SampleAnimation(model,0);var initial=feet.Select(t=>t.position).ToArray();var firstRider=seat.position;
            var motion=0f;var low=seat.position.y;var high=low;
            for(var i=1;i<=60;i++)
            {
                clip.SampleAnimation(model,clip.length*i/60f);
                for(var j=0;j<feet.Length;j++)motion=Mathf.Max(motion,Vector3.Distance(initial[j],feet[j].position));
                low=Mathf.Min(low,seat.position.y);high=Mathf.Max(high,seat.position.y);
            }
            var loop=0f;for(var j=0;j<feet.Length;j++)loop=Mathf.Max(loop,Vector3.Distance(initial[j],feet[j].position));
            report+=$"{clip.name}: duration={clip.length} hooves={feet.Length} hoofMotion={motion} hoofLoopError={loop} riderVerticalRange={high-low} riderLoopError={Vector3.Distance(firstRider,seat.position)}\n";
        }
        Object.DestroyImmediate(model);File.WriteAllText("QA/Cavalry/imported-clips.txt",report);
        if(EditorApplication.isPlaying)Fixture();
    }
    [MenuItem("City Forge/QA/Cavalry/Preview Click To Ride")]
    static void Preview()
    {
        if(!EditorApplication.isPlaying)return;
        Fixture();var p=root.localPosition;destination=new Vector2(p.x,p.z+4f);
        var accepted=world.CommandSelectedAnimal(destination);
        hooves=root.GetComponentsInChildren<Transform>().Where(t=>t.name.StartsWith("Hoof.")).ToArray();rider=root.GetComponentsInChildren<Transform>().First(t=>t.name=="RIDER");
        baseline=hooves.Select(t=>root.InverseTransformPoint(t.position)).ToArray();minRider=maxRider=root.InverseTransformPoint(rider.position).y;hoofMotion=0;walked=false;start=Time.time;
        Directory.CreateDirectory("QA/Cavalry");File.WriteAllText("QA/Cavalry/live.txt",$"destinationAccepted={accepted} selectedName={world.SelectedAnimalName}\n");
        EditorApplication.update-=Tick;EditorApplication.update+=Tick;
    }
    static void Tick()
    {
        if(!EditorApplication.isPlaying||root==null){EditorApplication.update-=Tick;return;}
        var player=root.GetComponent<ThreeDimensionalCharacterAnimator>();walked|=player.State=="walk";
        if(player.State=="walk")
        {
            var y=root.InverseTransformPoint(rider.position).y;minRider=Mathf.Min(minRider,y);maxRider=Mathf.Max(maxRider,y);
            for(var i=0;i<hooves.Length;i++)hoofMotion=Mathf.Max(hoofMotion,Vector3.Distance(baseline[i],root.InverseTransformPoint(hooves[i].position)));
        }
        if(Time.time-start<7)return;
        var pos=root.localPosition;
        File.AppendAllText("QA/Cavalry/live.txt",$"walked={walked} hooves={hooves.Length} hoofMotion={hoofMotion} riderBobMeters={maxRider-minRider} destinationError={Vector2.Distance(new Vector2(pos.x,pos.z),destination)} finalGait={player.State} travelSpeed={root.GetComponent<HorseGaitController>().TravelSpeed}\n");
        EditorApplication.update-=Tick;
    }
}
#endif

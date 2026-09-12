#if UNITY_EDITOR
using System;
using System.IO;
using System.Reflection;
using System.Collections.Generic;
using CityForgeV3.UI;
using CityForgeV3.World;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using Object=UnityEngine.Object;
public static class FoodWagonQa
{
    const BindingFlags F=BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic;
    static LotWorldController world;
    static CityForgeApp app;
    static object Call(object o,string name,params object[] args)=>o.GetType().GetMethod(name,F).Invoke(o,args);
    static HorseCarriageController Team(){var roots=(System.Collections.IList)world.GetType().GetField("_propPresentations",F).GetValue(world);return ((Transform)roots[world.SelectedPropIndex]).GetComponent<HorseCarriageController>();}
    static void Fixture(string id)
    {
        typeof(CarriageDriveQa).GetMethod("Load",BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,null);
        world=Object.FindFirstObjectByType<LotWorldController>();app=Object.FindFirstObjectByType<CityForgeApp>();
        var index=world.Session.Data.Props.FindIndex(p=>p.PropId==LotWorldController.HorseCarriagePropId);
        world.Session.Data.Props[index].PropId=id;
        world.Session.Data.Props[index].CarriageFast=false;
        world.Session.Data.Props[index].HasCarriagePose=false;
        Call(world,"RebuildPropPresentations");
        typeof(LotWorldController).GetProperty("SelectedPropIndex",F).SetValue(world,index);
        typeof(LotWorldController).GetProperty("ActiveObjectSelection",F).SetValue(world,LotObjectSelectionKind.Prop);
        Call(app,"RefreshLotEditor");
    }
    static void Frame()
    {
        var team=Team();var camera=(Camera)world.GetType().GetField("_camera",F).GetValue(world);
        var shift=camera.transform.right*(world.ZoomLevel==LotZoomLevel.Detail?-LotWorldController.DetailInspectorClearanceMeters:LotWorldController.CameraFramingOffsetMeters(world.LotSizeMeters));
        world.SetQaCameraPan(team.transform.localPosition.x+shift.x,team.transform.localPosition.z-1.2f+shift.z);
        world.SetQaOrthographicSize(5f);
    }
    [MenuItem("City Forge/QA/Food Wagon/Open Track Preview")]
    static void Open()
    {
        if(!EditorApplication.isPlaying)return;
        Fixture(LotWorldController.HorseFoodWagonPropId);Frame();
        Directory.CreateDirectory("QA/FoodWagon");
        var team=Team();var report="";
        foreach(var t in team.Carriage.GetComponentsInChildren<Transform>())
            if(t.name.StartsWith("Carriage_"))report+=$"{t.name} pivot={team.Carriage.InverseTransformPoint(t.position)} parent={t.parent.name}\n";
        File.WriteAllText("QA/FoodWagon/rig.txt",report);
    }
    [MenuItem("City Forge/QA/Food Wagon/Check Shared Driving")]
    static void Check()
    {
        if(!EditorApplication.isPlaying)return;
        Directory.CreateDirectory("QA/FoodWagon");var report="";
        foreach(var id in new[]{LotWorldController.HorseFoodWagonPropId})
        {
            Fixture(id);var t=Team();world.SetSelectedCarriageFast(true);var accepted=world.DriveSelectedCarriage();
            var clear=(Func<Vector3,Vector3,bool>)Delegate.CreateDelegate(typeof(Func<Vector3,Vector3,bool>),world,world.GetType().GetMethod("CarriageSegmentClear",F));
            var wraps=0;var hitch=0f;var maxSteer=0f;var firstSpeed=0f;var wheelController=t.Carriage.GetComponent<CarriageWheelController>();
            var rotations=new Dictionary<Transform,Quaternion>();foreach(var x in t.Carriage.GetComponentsInChildren<Transform>())if(x.name.StartsWith("Carriage_Front_")||x.name.StartsWith("Carriage_Rear_"))rotations[x]=x.rotation;
            for(var i=0;i<5000&&t.IsMoving;i++)
            {
                var cursor=t.CaptureMotion().Cursor;t.Step(.05f,_=>.06f,clear);if(i==0)firstSpeed=t.CurrentSpeed;
                Call(wheelController,"LateUpdate");
                hitch=Mathf.Max(hitch,t.HitchError);maxSteer=Mathf.Max(maxSteer,Quaternion.Angle(t.Forecarriage.rotation,t.Carriage.rotation));
                if(t.CaptureMotion().Cursor<cursor)wraps++;if(wraps==2)break;
            }
            var wheelsMoved=0;foreach(var pair in rotations)if(Quaternion.Angle(pair.Value,pair.Key.rotation)>1)wheelsMoved++;
            var cruise=t.CurrentSpeed;world.StopSelectedCarriage();var coasts=t.IsMoving&&t.IsStopping;
            for(var i=0;i<20;i++)t.Step(.05f,_=>.06f,clear);
            var braking=t.CurrentSpeed;var prop=world.Session.Data.Props[world.SelectedPropIndex];
            prop.HasCarriagePose=true;prop.PositionX=t.transform.localPosition.x;prop.PositionZ=t.transform.localPosition.z;prop.HorseHeadingDegrees=t.transform.localEulerAngles.y;
            var parent=t.transform.parent.rotation;prop.CarriageHeadingDegrees=(Quaternion.Inverse(parent)*t.Carriage.rotation).eulerAngles.y;prop.ForecarriageHeadingDegrees=(Quaternion.Inverse(parent)*t.Forecarriage.rotation).eulerAngles.y;
            Call(world,"RebuildPropPresentations");t=Team();var persists=t.IsStopping&&Mathf.Abs(t.CurrentSpeed-braking)<.0001f&&t.Fast;
            for(var i=0;i<100&&t.IsMoving;i++)t.Step(.05f,_=>.06f,clear);
            var parked=!t.IsMoving&&t.CurrentSpeed==0;var point=t.transform.position;t.Step(.05f,_=>.06f,clear);
            var shadows=0;foreach(var r in t.GetComponentsInChildren<MeshRenderer>())if(r.sharedMaterial!=null&&r.sharedMaterial.name.Contains("Road Contact Shadow"))shadows++;
            report+=$"{id}: accepted={accepted} wraps={wraps} firstSpeed={firstSpeed} cruise={cruise} wheels={wheelController.WheelCount} wheelsMoved={wheelsMoved} maxHitchError={hitch} maxSteer={maxSteer} coasts={coasts} brakingAfter1s={braking} rebuildPreservesMotion={persists} parked={parked} stable={point==t.transform.position} shadows={shadows} horses={(t.SecondHorse!=null?2:1)} harnessLines={t.GetComponentsInChildren<LineRenderer>().Length}\n";
        }
        File.WriteAllText("QA/FoodWagon/driving.txt",report);Open();
    }
    [MenuItem("City Forge/QA/Food Wagon/Preview Drive and Stop")]
    static void Preview()
    {
        Open();var root=(VisualElement)app.GetType().GetField("_root",F).GetValue(app);
        root.schedule.Execute(()=>
        {
            var camera=(Camera)world.GetType().GetField("_camera",F).GetValue(world);var t=Team();var pixel=camera.WorldToScreenPoint(t.Carriage.position+Vector3.up);
            var size=new Vector2(root.resolvedStyle.width,root.resolvedStyle.height);var panel=new Vector2(pixel.x/Screen.width*size.x,(1-pixel.y/Screen.height)*size.y);
            var selected=world.BeginExistingObjectManipulationFromPanel(panel,size);world.EndPropDrag();Call(app,"RefreshLotEditor");
            var drive=root.Q<Button>("drive-carriage");typeof(Clickable).GetMethod("Invoke",F).Invoke(drive.clickable,new object[]{null});
            root.schedule.Execute(()=>
            {
                var team=Team();File.WriteAllText("QA/FoodWagon/live.txt",$"selectionHit={selected} selected={world.SelectedHorseWagonName} moving={team.IsMoving} gait={team.Horse.GetComponent<ThreeDimensionalCharacterAnimator>().State} wheelTravel={team.Carriage.GetComponent<CarriageWheelController>().TotalTravelMeters}\n");
                var stop=root.Q<Button>("stop-carriage");typeof(Clickable).GetMethod("Invoke",F).Invoke(stop.clickable,new object[]{null});
            }).StartingIn(3500);
            root.schedule.Execute(()=>{Frame();File.AppendAllText("QA/FoodWagon/live.txt",$"stopped={!Team().IsMoving} speed={Team().CurrentSpeed}\n");}).StartingIn(5500);
        }).StartingIn(500);
    }
}
#endif

#if UNITY_EDITOR
using System;
using System.IO;
using System.Collections.Generic;
using System.Reflection;
using CityForgeV3.UI;
using CityForgeV3.World;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using Object=UnityEngine.Object;
public static class CarriageEaseQa
{
    const BindingFlags Flags=BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic;
    static LotWorldController world;
    static HorseCarriageController Team(){var roots=(System.Collections.IList)world.GetType().GetField("_propPresentations",Flags).GetValue(world);return ((Transform)roots[world.SelectedPropIndex]).GetComponent<HorseCarriageController>();}
    static void Load()
    {
        typeof(CarriageDriveQa).GetMethod("Load",BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,null);
        typeof(CarriageDriveQa).GetMethod("Select",BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,null);
        world=Object.FindFirstObjectByType<LotWorldController>();
    }
    [MenuItem("City Forge/QA/Carriage/Check Easing and Shadows")]
    static void Check()
    {
        if(!EditorApplication.isPlaying)return;
        Load();world.SetSelectedCarriageFast(true);world.DriveSelectedCarriage();var team=Team();
        var clear=(Func<Vector3,Vector3,bool>)Delegate.CreateDelegate(typeof(Func<Vector3,Vector3,bool>),world,world.GetType().GetMethod("CarriageSegmentClear",Flags));
        var first=0f;var one=0f;
        for(var i=0;i<80;i++){team.Step(.05f,_=>.06f,clear);if(i==0)first=team.CurrentSpeed;if(i==19)one=team.CurrentSpeed;}
        var cruise=team.CurrentSpeed;var stopPosition=team.transform.position;
        world.StopSelectedCarriage();var stopImmediate=team.IsMoving&&team.IsStopping&&team.CurrentSpeed==cruise;
        for(var i=0;i<20;i++)team.Step(.05f,_=>.06f,clear);
        var brakeOne=team.CurrentSpeed;
        var p=world.Session.Data.Props[world.SelectedPropIndex];
        p.HasCarriagePose=true;p.PositionX=team.transform.localPosition.x;p.PositionZ=team.transform.localPosition.z;
        p.HorseHeadingDegrees=team.transform.localEulerAngles.y;p.CarriageHeadingDegrees=team.Carriage.eulerAngles.y;p.ForecarriageHeadingDegrees=team.Forecarriage.eulerAngles.y;
        world.GetType().GetMethod("RebuildPropPresentations",Flags).Invoke(world,null);team=Team();
        var rebuild=team.IsStopping&&Mathf.Abs(team.CurrentSpeed-brakeOne)<.0001f;
        var stopTime=1f;
        while(team.IsMoving&&stopTime<10){team.Step(.05f,_=>.06f,clear);stopTime+=.05f;}
        var stopDistance=Vector3.Distance(stopPosition,team.transform.position);var parked=team.transform.position;
        team.Step(.05f,_=>.06f,clear);
        Directory.CreateDirectory("QA/CarriageEase");
        File.WriteAllText("QA/CarriageEase/checks.txt",$"firstFrameSpeed={first} afterOneSecond={one} cruise={cruise} stopCoasts={stopImmediate} brakeAfterOneSecond={brakeOne} rebuildKeepsBraking={rebuild} stopTime={stopTime} stopDistance={stopDistance} parked={!team.IsMoving} stable={parked==team.transform.position} hitchError={team.HitchError}\n");
        var start=team.transform.position;var destination=start+team.transform.forward*8f;destination.y=0;
        team.SetRoute(new List<Vector3>{destination});
        var seconds=0f;var lastSpeed=0f;
        while(team.IsMoving&&seconds<20){lastSpeed=team.CurrentSpeed;team.Step(.05f,_=>.06f,(_,__)=>true);seconds+=.05f;}
        var flat=team.transform.position;flat.y=0;
        File.AppendAllText("QA/CarriageEase/checks.txt",$"destinationStopped={!team.IsMoving} destinationError={Vector3.Distance(flat,destination)} lastArrivalSpeed={lastSpeed}\n");
        team.SetRoute(new List<Vector3>{destination+team.transform.forward*8f});
        team.Step(.05f,_=>.06f,(_,__)=>false);
        File.AppendAllText("QA/CarriageEase/checks.txt",$"obstacleStops={team.WasBlocked&&!team.IsMoving&&team.CurrentSpeed==0}\n");
        Open();
    }
    [MenuItem("City Forge/QA/Carriage/Open Road Shadow Closeup")]
    static void Open()
    {
        if(!EditorApplication.isPlaying)return;
        Directory.CreateDirectory("QA/CarriageEase");
        Load();var team=Team();
        var camera=(Camera)world.GetType().GetField("_camera",Flags).GetValue(world);
        var shift=camera.transform.right*(world.ZoomLevel==LotZoomLevel.Detail ? -LotWorldController.DetailInspectorClearanceMeters : LotWorldController.CameraFramingOffsetMeters(world.LotSizeMeters));
        world.SetQaCameraPan(team.transform.localPosition.x+shift.x,team.transform.localPosition.z-1f+shift.z);
        world.SetQaOrthographicSize(5f);
        var app=Object.FindFirstObjectByType<CityForgeApp>();
        var root=(VisualElement)app.GetType().GetField("_root",Flags).GetValue(app);
        root.schedule.Execute(()=>
        {
            var shadows=team.GetComponent<HorseCarriageGroundShadow>().GetComponentsInChildren<MeshRenderer>();
            var count=0;foreach(var r in shadows)if(r.sharedMaterial!=null&&r.sharedMaterial.name.Contains("Road Contact Shadow"))count++;
            File.WriteAllText("QA/CarriageEase/shadows.txt",$"contactShadows={count} queue={StreetVehicleGroundShadow.RenderQueue} position={team.transform.position}\n");
        }).StartingIn(500);
    }
    [MenuItem("City Forge/QA/Carriage/Preview Gentle Drive Stop")]
    static void Preview()
    {
        if(!EditorApplication.isPlaying)return;
        Open();var app=Object.FindFirstObjectByType<CityForgeApp>();var root=(VisualElement)app.GetType().GetField("_root",Flags).GetValue(app);
        root.schedule.Execute(()=>
        {
            world.DriveSelectedCarriage();
            root.schedule.Execute(()=>world.StopSelectedCarriage()).StartingIn(2500);
            root.schedule.Execute(()=>
            {
                var t=Team();File.WriteAllText("QA/CarriageEase/live.txt",$"parked={!t.IsMoving} speed={t.CurrentSpeed} gait={t.Horse.GetComponent<ThreeDimensionalCharacterAnimator>().State} wheels={t.Carriage.GetComponent<CarriageWheelController>().WheelCount}\n");
            }).StartingIn(5000);
        }).StartingIn(500);
    }
}
#endif

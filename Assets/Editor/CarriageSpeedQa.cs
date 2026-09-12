#if UNITY_EDITOR
using System;
using System.IO;
using System.Reflection;
using CityForgeV3.UI;
using CityForgeV3.World;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using Object=UnityEngine.Object;
public static class CarriageSpeedQa
{
    const BindingFlags Flags=BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic;
    [MenuItem("City Forge/QA/Carriage/Check Slow Fast Template")]
    static void Check()
    {
        if(!EditorApplication.isPlaying)return;
        typeof(CarriageDriveQa).GetMethod("Load",BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,null);
        typeof(CarriageDriveQa).GetMethod("Select",BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,null);
        var world=Object.FindFirstObjectByType<LotWorldController>();
        HorseCarriageController Team(){var roots=(System.Collections.IList)world.GetType().GetField("_propPresentations",Flags).GetValue(world);return ((Transform)roots[world.SelectedPropIndex]).GetComponent<HorseCarriageController>();}
        var team=Team();var start=team.transform.position;
        var clear=(Func<Vector3,Vector3,bool>)Delegate.CreateDelegate(typeof(Func<Vector3,Vector3,bool>),world,world.GetType().GetMethod("CarriageSegmentClear",Flags));
        var distances=new float[2];
        for(var speed=0;speed<2;speed++)
        {
            team.transform.position=start;team.RestoreHeadings(0,0,0);
            world.SetSelectedCarriageFast(speed==1);
            if(!world.DriveSelectedCarriage())throw new Exception("Drive rejected");
            for(var i=0;i<20;i++){var before=team.transform.position;team.Step(.05f,_=>.06f,clear);distances[speed]+=Vector3.Distance(before,team.transform.position);}
        }
        var json=world.Session.Serialize();var restored=JsonUtility.FromJson<LotSaveData>(json);
        var saved=restored.Props[world.SelectedPropIndex].CarriageFast;
        var prop=world.Session.Data.Props[world.SelectedPropIndex];
        prop.HasCarriagePose=true;prop.PositionX=team.transform.localPosition.x;prop.PositionZ=team.transform.localPosition.z;
        prop.HorseHeadingDegrees=team.transform.localEulerAngles.y;
        prop.CarriageHeadingDegrees=team.Carriage.eulerAngles.y;
        prop.ForecarriageHeadingDegrees=team.Forecarriage.eulerAngles.y;
        world.GetType().GetMethod("RebuildPropPresentations",Flags).Invoke(world,null);
        team=Team();var rebuild=team.Fast&&team.IsMoving;
        var wraps=0;var maxError=0f;
        for(var i=0;i<5000&&team.IsMoving;i++)
        {
            var cursor=team.CaptureMotion().Cursor;
            team.Step(.05f,_=>.06f,clear);
            if(team.CaptureMotion().Cursor<cursor)wraps++;
            maxError=Mathf.Max(maxError,team.HitchError);
            if(wraps==2)break;
        }
        Directory.CreateDirectory("QA/CarriageSpeed");
        File.WriteAllText("QA/CarriageSpeed/checks.txt",$"slowOneSecond={distances[0]} fastOneSecond={distances[1]} ratio={distances[1]/distances[0]} savedFast={saved} rebuiltFastMoving={rebuild} fastLaps={wraps} blocked={team.WasBlocked} hitchError={maxError}\n");
        typeof(CarriageDriveQa).GetMethod("Load",BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,null);
        typeof(CarriageDriveQa).GetMethod("Select",BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,null);
        var app=Object.FindFirstObjectByType<CityForgeApp>();
        var root=(VisualElement)app.GetType().GetField("_root",Flags).GetValue(app);
        root.schedule.Execute(()=>
        {
            var fast=root.Q<Button>("carriage-speed-fast");
            typeof(Clickable).GetMethod("Invoke",Flags).Invoke(fast.clickable,new object[]{null});
            world.DriveSelectedCarriage();
            root.schedule.Execute(()=>
            {
                var live=Team();var gait=live.Horse.GetComponent<HorseGaitController>();
                File.WriteAllText("QA/CarriageSpeed/live.txt",$"fast={world.SelectedCarriageFast} speed={live.SpeedMetersPerSecond} observedSpeed={gait.TravelSpeed} animation={live.Horse.GetComponent<ThreeDimensionalCharacterAnimator>().State} fastHighlighted={root.Q<Button>("carriage-speed-fast").ClassListContains("cf-button--mode-selected")} screens={root.childCount}\n");
                var slow=root.Q<Button>("carriage-speed-slow");
                typeof(Clickable).GetMethod("Invoke",Flags).Invoke(slow.clickable,new object[]{null});
                root.schedule.Execute(()=>
                {
                    File.AppendAllText("QA/CarriageSpeed/live.txt",$"slow={!world.SelectedCarriageFast} moving={world.SelectedCarriageIsMoving} animation={Team().Horse.GetComponent<ThreeDimensionalCharacterAnimator>().State} slowHighlighted={root.Q<Button>("carriage-speed-slow").ClassListContains("cf-button--mode-selected")}\n");
                    world.SetSelectedCarriageFast(true);
                }).StartingIn(2000);
            }).StartingIn(3000);
        }).StartingIn(500);
    }
}
#endif

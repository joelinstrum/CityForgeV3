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
[InitializeOnLoad]
public static class CarriageDriveQa
{
    const BindingFlags Flags=BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic;
    static CityForgeApp app;
    static LotWorldController world;
    static void Field(object obj,string name,object value)=>obj.GetType().GetField(name,Flags).SetValue(obj,value);
    static object Call(object obj,string name,params object[] args)=>obj.GetType().GetMethod(name,Flags).Invoke(obj,args);
    static double nextLog;
    static CarriageDriveQa(){EditorApplication.update+=Observe;}
    static void Observe()
    {
        if(!EditorApplication.isPlaying||world==null||EditorApplication.timeSinceStartup<nextLog)return;
        nextLog=EditorApplication.timeSinceStartup+1;
        var team=Object.FindFirstObjectByType<HorseCarriageController>();if(team==null)return;
        var root=(VisualElement)app.GetType().GetField("_root",Flags).GetValue(app);
        var panel=root.Q<VisualElement>("carriage-inspector");
        var drive=root.Q<Button>("drive-carriage"); var stop=root.Q<Button>("stop-carriage");
        Directory.CreateDirectory("QA/CarriageDrive");
        File.WriteAllText("QA/CarriageDrive/live.txt",$"selected={world.SelectedPropIsCarriageTeam} active={world.ActiveObjectSelection} moving={team.IsMoving} position={team.transform.localPosition} animation={team.Horse.GetComponent<ThreeDimensionalCharacterAnimator>().State}\npanel={panel?.worldBound} drive={drive?.worldBound} enabled={drive?.enabledSelf} stopEnabled={stop?.enabledSelf} screens={root.childCount} mouse={Input.mousePosition} screen={Screen.width}x{Screen.height} ui={root.worldBound}\n");
    }
    [MenuItem("City Forge/QA/Carriage/Load Saved Track")]
    static void Load()
    {
        if(!EditorApplication.isPlaying)return;
        app=Object.FindFirstObjectByType<CityForgeApp>();
        Call(app,"EnsureLotWorld");
        world=Object.FindFirstObjectByType<LotWorldController>(FindObjectsInactive.Include);
        world.gameObject.SetActive(true);
        if(!world.LoadLot("track"))throw new Exception("Track not found");
        Field(app,"_hasOpenLot",true); Field(app,"_lotInspectorVisible",true);
        Field(app,"_lotInspectorPosition",new Vector2(-1,-1));
        Field(app,"_lotEditorCategory",Enum.Parse(app.GetType().GetField("_lotEditorCategory",Flags).FieldType,"Main"));
        var show=app.GetType().GetMethod("Show",Flags);
        show.Invoke(app,new[]{Enum.Parse(show.GetParameters()[0].ParameterType,"LotEditor")});
    }
    [MenuItem("City Forge/QA/Carriage/Select Track Carriage")]
    static void Select()
    {
        if(!EditorApplication.isPlaying)return;
        if(world==null)Load();
        var team=Object.FindFirstObjectByType<HorseCarriageController>();
        var camera=(Camera)world.GetType().GetField("_camera",Flags).GetValue(world);
        var root=(VisualElement)app.GetType().GetField("_root",Flags).GetValue(app);
        var pixel=camera.WorldToScreenPoint(team.Carriage.position+Vector3.up);
        var panelSize=new Vector2(root.resolvedStyle.width,root.resolvedStyle.height);
        var panelPoint=new Vector2(pixel.x/Screen.width*panelSize.x,(1-pixel.y/Screen.height)*panelSize.y);
        var selected=world.BeginExistingObjectManipulationFromPanel(panelPoint,panelSize);
        world.EndPropDrag();
        Field(app,"_lotInspectorVisible",true);
        Field(app,"_lotInspectorPosition",new Vector2(-1,-1));
        Call(app,"RefreshLotEditor");
        Directory.CreateDirectory("QA/CarriageDrive");
        File.WriteAllText("QA/CarriageDrive/selection.txt",$"selection={selected} carriage={world.SelectedPropIsCarriageTeam} panelPoint={panelPoint}\n");
    }
    [MenuItem("City Forge/QA/Carriage/Check Drive Panel")]
    static void CheckPanel()
    {
        Load(); Select();
        var root=(VisualElement)app.GetType().GetField("_root",Flags).GetValue(app);
        root.schedule.Execute(()=>
        {
            var drive=root.Q<Button>("drive-carriage");
            typeof(Clickable).GetMethod("Invoke",Flags).Invoke(drive.clickable,new object[]{null});
            root.schedule.Execute(()=>
            {
                var started=world.SelectedCarriageIsMoving;
                var team=Object.FindFirstObjectByType<HorseCarriageController>();
                var animation=team.Horse.GetComponent<ThreeDimensionalCharacterAnimator>().State;
                var stop=root.Q<Button>("stop-carriage");
                typeof(Clickable).GetMethod("Invoke",Flags).Invoke(stop.clickable,new object[]{null});
                root.schedule.Execute(()=>
                {
                    var stopped=!world.SelectedCarriageIsMoving;
                    var button=root.Q<Button>("drive-carriage");
                    File.WriteAllText("QA/CarriageDrive/panel.txt",$"started={started} animation={animation} stopped={stopped} driveEnabled={button.enabledSelf} screens={root.childCount}\n");
                    typeof(Clickable).GetMethod("Invoke",Flags).Invoke(button.clickable,new object[]{null});
                }).StartingIn(300);
            }).StartingIn(3000);
        }).StartingIn(500);
    }
    [MenuItem("City Forge/QA/Carriage/Check Track Drive")]
    static void Check()
    {
        if(!EditorApplication.isPlaying)return;
        if(world==null)Load();
        var index=world.Session.Data.Props.FindIndex(p=>p.PropId==LotWorldController.HorseCarriagePropId);
        typeof(LotWorldController).GetProperty("SelectedPropIndex",Flags).SetValue(world,index);
        typeof(LotWorldController).GetProperty("ActiveObjectSelection",Flags).SetValue(world,LotObjectSelectionKind.Prop);
        var accepted=world.DriveSelectedCarriage();
        Call(app,"ComposeLotEditor");
        var team=Object.FindFirstObjectByType<HorseCarriageController>();
        var clear=(Func<Vector3,Vector3,bool>)Delegate.CreateDelegate(typeof(Func<Vector3,Vector3,bool>),world,world.GetType().GetMethod("CarriageSegmentClear",Flags));
        var path=team.CaptureMotion();
        var distance=0f;var maxError=0f;var maxShaft=0f;var maxAxle=0f;var wraps=0;
        for(var i=0;i<10000&&team.IsMoving;i++)
        {
            var old=team.transform.position;var cursor=team.CaptureMotion().Cursor;
            team.Step(.05f,_=>.06f,clear);
            distance+=Vector3.Distance(old,team.transform.position);
            if(team.CaptureMotion().Cursor<cursor)wraps++;
            maxError=Mathf.Max(maxError,team.HitchError);
            maxShaft=Mathf.Max(maxShaft,Quaternion.Angle(team.Horse.rotation,team.Forecarriage.rotation));
            maxAxle=Mathf.Max(maxAxle,Quaternion.Angle(team.Forecarriage.rotation,team.Carriage.rotation));
            if(wraps==2)break;
        }
        var moving=team.IsMoving;var blocked=team.WasBlocked;
        world.StopSelectedCarriage();var parked=team.transform.position;
        team.Step(.05f,_=>.06f,clear);
        Directory.CreateDirectory("QA/CarriageDrive");
        File.WriteAllText("QA/CarriageDrive/checks.txt",$"accepted={accepted}\nloopStart={path.LoopStart}\npoints={path.Path?.Count}\ndistance={distance}\nwraps={wraps}\nmoving={moving}\nblocked={blocked}\nmaxHitchError={maxError}\nmaxShaftAngle={maxShaft}\nmaxAxleAngle={maxAxle}\nstopped={!team.IsMoving}\nstopStable={parked==team.transform.position}\n");
        Load();
    }
}
#endif

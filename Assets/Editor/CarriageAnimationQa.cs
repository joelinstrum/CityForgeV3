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
[InitializeOnLoad]
public static class CarriageAnimationQa
{
    static LotWorldController world;
    static Transform root,body;
    static Transform[] wheels;
    static Quaternion[] initialWheels;
    static Quaternion bodyRotation;
    static Vector3 bodyPosition;
    static float start,previous,writeAt;
    static bool active;
    static CarriageAnimationQa(){EditorApplication.update+=Tick;}
    [MenuItem("City Forge/QA/Carriage/Open Rolling Wheels")]
    static void Open()
    {
        if(!EditorApplication.isPlaying)return;
        Object.FindFirstObjectByType<CityForgeApp>().OpenAnimatedHorseQa();
        world=Object.FindFirstObjectByType<LotWorldController>();
        world.Session.Data.Props.Clear();
        world.PlacePropForQa(LotWorldController.CarriagePropId,0,0);
        world.SetQaOrthographicSize(6f);
        root=Object.FindFirstObjectByType<CarriageWheelController>().transform;
        root.localRotation=Quaternion.Euler(0,180,0);
        root.localPosition=new Vector3(0,root.localPosition.y,1);
        body=root.GetComponentsInChildren<Transform>().First(t=>t.name=="Carriage_Body");
        wheels=root.GetComponentsInChildren<Transform>().Where(t=>t.name.StartsWith("Carriage_Front_")||t.name.StartsWith("Carriage_Rear_")).ToArray();
        initialWheels=wheels.Select(t=>t.localRotation).ToArray();
        bodyPosition=body.localPosition;bodyRotation=body.localRotation;
        start=previous=Time.time;writeAt=start;active=true;
        Directory.CreateDirectory("QA/CarriageV01");
    }
    [MenuItem("City Forge/QA/Carriage/Stop Preview")]
    static void Stop(){active=false;}
    static void Tick()
    {
        if(!active||!EditorApplication.isPlaying||root==null)return;
        var elapsed=Time.time-start;var dt=Mathf.Min(Time.time-previous,.05f);previous=Time.time;
        // Straight forward/reverse travel with stationary pauses, using the lot camera.
        var phase=elapsed%12f;
        var velocity=phase<2?0:phase<4?1.06f:phase<6?0:phase<8?-1.06f:0;
        root.localPosition+=root.forward*(velocity*dt);
        if(Time.time<writeAt)return;writeAt=Time.time+.5f;
        var angles=wheels.Select((t,i)=>Quaternion.Angle(initialWheels[i],t.localRotation)).ToArray();
        var controller=root.GetComponent<CarriageWheelController>();
        var fixedBody=Vector3.Distance(body.localPosition,bodyPosition)<.00001f&&Quaternion.Angle(body.localRotation,bodyRotation)<.0001f;
        File.WriteAllText("QA/CarriageV01/live.txt",$"wheels={controller.WheelCount}\ntravel={controller.TotalTravelMeters}\nangles={string.Join(",",angles)}\nbodyFixed={fixedBody}\nposition={root.position}\nforward={root.forward}\ncommandSpeed={velocity}\n");
    }
}
#endif

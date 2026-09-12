#if UNITY_EDITOR
using System;
using System.IO;
using System.Reflection;
using CityForgeV3.World;
using UnityEditor;
using UnityEngine;
using Object=UnityEngine.Object;
public static class LotLoadReflectionQa
{
    const BindingFlags F=BindingFlags.Instance|BindingFlags.NonPublic;
    static LotWorldController world;
    static int phase,frame,errors;
    static bool deferred,restarted;
    static string report;
    static object Field(string name)=>typeof(LotWorldController).GetField(name,F).GetValue(world);
    static void Log(string condition,string stack,LogType type){if(type==LogType.Error||type==LogType.Exception)errors++;}
    [MenuItem("City Forge/QA/Lot Loading/Check Inactive Reflection Refresh")]
    static void Check()
    {
        if(!EditorApplication.isPlaying)return;
        EditorApplication.update-=Tick;
        typeof(CarriageDriveQa).GetMethod("Load",BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,null);
        world=Object.FindFirstObjectByType<LotWorldController>();errors=0;
        Application.logMessageReceived+=Log;
        world.gameObject.SetActive(false);
        var loaded=world.LoadLot("track");
        // Two inactive rebuild requests must coalesce without starting a routine.
        typeof(LotWorldController).GetMethod("ScheduleWetStreetReflectionRefresh",F).Invoke(world,null);
        deferred=(bool)Field("_wetReflectionRefreshPending")&&Field("_wetReflectionRefreshRoutine")==null;
        report=$"loadedWhileInactive={loaded} deferred={deferred}";
        world.gameObject.SetActive(true);
        var scheduled=Field("_wetReflectionRefreshRoutine")!=null;
        world.gameObject.SetActive(false); // Hide again before the deferred frame.
        var interrupted=(bool)Field("_wetReflectionRefreshPending")&&Field("_wetReflectionRefreshRoutine")==null;
        world.gameObject.SetActive(true);
        restarted=Field("_wetReflectionRefreshRoutine")!=null;
        report+=$" scheduledOnEnable={scheduled} interruptedRequestRetained={interrupted} restarted={restarted}";
        phase=0;frame=Time.frameCount;EditorApplication.update+=Tick;
    }
    static void Tick()
    {
        if(!EditorApplication.isPlaying||world==null){Finish(" aborted");return;}
        if(Time.frameCount<frame+3)return;
        var completed=!(bool)Field("_wetReflectionRefreshPending")&&Field("_wetReflectionRefreshRoutine")==null;
        if(phase==0)
        {
            report+=$" completedAfterEnable={completed}";
            // Active rebuilds still refresh after frame-end destruction.
            typeof(LotWorldController).GetMethod("ScheduleWetStreetReflectionRefresh",F).Invoke(world,null);
            phase=1;frame=Time.frameCount;return;
        }
        Finish($" activeRefreshCompleted={completed} errors={errors}\n");
    }
    static void Finish(string result)
    {
        EditorApplication.update-=Tick;Application.logMessageReceived-=Log;
        Directory.CreateDirectory("QA/LotLoadReflection");
        File.WriteAllText("QA/LotLoadReflection/checks.txt",report+result);
    }
}
#endif

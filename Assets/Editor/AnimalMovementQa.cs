#if UNITY_EDITOR
using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using CityForgeV3.UI;
using CityForgeV3.World;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

[InitializeOnLoad]
public static class AnimalMovementQa
{
    static LotWorldController world;
    static Vector2 initial;
    static bool active, sawWalk, sawArrow;
    static float distance;
    static int ordersObserved;
    static object lastOrder;
    static float nextReport;
    const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
    static AnimalMovementQa() { EditorApplication.update += Tick; }
    [MenuItem("City Forge/QA/Animals/Open Click To Move")]
    static void Open()
    {
        if (!EditorApplication.isPlaying) return;
        Object.FindFirstObjectByType<CityForgeApp>().OpenAnimatedHorseQa();
        world = Object.FindFirstObjectByType<LotWorldController>();
        world.PlacePropForQa(LotWorldController.BearAnimalId,-3,0);
        world.SetQaOrthographicSize(5f);
        initial=Vector2.zero; distance=0; ordersObserved=0; lastOrder=null;
        active=true; sawWalk=sawArrow=false;
        Directory.CreateDirectory("QA/AnimalMovementV01");

    }
    [MenuItem("City Forge/QA/Animals/Check Obstacle Routes")]
    static void Routes()
    {
        if(world==null) return;
        world.PlacePropForQa(LotWorldController.OrnateBenchPropId,0,2f);
        typeof(LotWorldController).GetMethod("RebuildBearObstacles",Private).Invoke(world,null);
        var find=typeof(LotWorldController).GetMethod("FindAnimalRoute",Private);
        var route=(IList)find.Invoke(world,new object[]{Vector2.zero,new Vector2(0,5)});
        var blocked=find.Invoke(world,new object[]{Vector2.zero,new Vector2(0,2)});
        var outside=find.Invoke(world,new object[]{Vector2.zero,new Vector2(10000,0)});
        Directory.CreateDirectory("QA/AnimalMovementV01");
        File.WriteAllText("QA/AnimalMovementV01/routes.txt",$"detourWaypoints={route?.Count ?? 0}\nblockedRejected={blocked==null}\noutsideRejected={outside==null}\npassed={route!=null && route.Count>1 && blocked==null && outside==null}\n");
    }
    [MenuItem("City Forge/QA/Animals/Verify Horse Detour")]
    static void Detour()
    {
        if (world == null) return;
        var index = world.Session.Data.Props.FindIndex(p => p.PropId == LotWorldController.HorseAnimalId);
        typeof(LotWorldController).GetProperty("SelectedPropIndex").SetValue(world,index);
        world.CommandSelectedAnimal(new Vector2(0,5));
    }
    [MenuItem("City Forge/QA/Animals/Check Selection Routing")]
    static void SelectionRouting()
    {
        if(world==null) return;
        var horse=Object.FindFirstObjectByType<HorseGaitController>();
        var camera=Camera.main;
        var size=new Vector2(camera.pixelWidth,camera.pixelHeight);
        Vector2 Panel(Vector3 point) { var p=camera.WorldToScreenPoint(point); return new Vector2(p.x,size.y-p.y); }
        world.DeselectAll();
        var kind=world.BeginExistingObjectManipulationFromPanel(Panel(horse.transform.position+Vector3.up),size);
        world.EndPropDrag();
        var selected=world.SelectedPropIsAnimal && kind==LotObjectSelectionKind.Prop;
        var point=horse.transform.position+Vector3.back*2;
        point.y=0;
        var handled=world.TryAnimalDestinationFromPanel(Panel(point),size,out var status);
        File.WriteAllText("QA/AnimalMovementV01/selection-routing.txt",$"selected={selected}\nhandled={handled}\nstatus={status}\npassed={selected && handled && status.Contains("destination set")}\n");
    }
    static void Tick()
    {
        if(!active || !EditorApplication.isPlaying || world==null) return;
        var prop=world.Session.Data.Props.FirstOrDefault(p=>p.PropId==LotWorldController.HorseAnimalId);
        if(prop==null) {active=false;return;}
        var pos=new Vector2(prop.PositionX,prop.PositionZ);
        distance=Mathf.Max(distance,Vector2.Distance(initial,pos));
        var horse=Object.FindFirstObjectByType<HorseGaitController>();
        if(horse!=null) sawWalk |= horse.GetComponent<ThreeDimensionalCharacterAnimator>().IsPlaying("walk");
        var arrow=(LineRenderer)typeof(LotWorldController).GetField("_animalOrderArrow",Private).GetValue(world);
        sawArrow |= arrow!=null && arrow.enabled;
        var orders=(IDictionary)typeof(LotWorldController).GetField("_animalOrders",Private).GetValue(world);
        var order=orders[prop.InstanceId];
        if(order==null) return;
        if(!ReferenceEquals(lastOrder,order)){ordersObserved++;lastOrder=order;}
        var arrived=(bool)order.GetType().GetField("Arrived").GetValue(order);
        var target=(Vector2)order.GetType().GetField("Destination").GetValue(order);
        var error=Vector2.Distance(pos,target);
        if (Time.time < nextReport) return;
        nextReport = Time.time + 0.2f;
        Directory.CreateDirectory("QA/AnimalMovementV01");
        File.WriteAllText("QA/AnimalMovementV01/movement-result.txt",$"ordersObserved={ordersObserved}\nwalkObserved={sawWalk}\narrowObserved={sawArrow}\nmaxDisplacement={distance}\narrived={arrived}\ndistanceToDestination={error}\narrowHidden={arrow==null || !arrow.enabled}\nposition={pos}\ntarget={target}\nnextWaypoint={order.GetType().GetField("Next").GetValue(order)}\nrotation={horse.transform.localEulerAngles.y}\npassed={arrived && error<.07f && distance>.3f && sawWalk && sawArrow}\n");
    }
}
#endif

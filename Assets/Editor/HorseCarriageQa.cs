#if UNITY_EDITOR
using System.IO;
using System.Linq;
using CityForgeV3.UI;
using CityForgeV3.World;
using UnityEditor;
using UnityEngine;
using Object=UnityEngine.Object;
[InitializeOnLoad]
public static class HorseCarriageQa
{
    static LotWorldController world;
    static HorseCarriageController team;
    static float writeAt,maxArticulation,maxError;
    static bool active;
    static int commands,phase;
    static float nextAction;
    static bool rebuilt;
    static HorseCarriageQa(){EditorApplication.update+=Observe;}
    [MenuItem("City Forge/QA/Carriage/Open One Horse Team")]
    static void Open()
    {
        if(!EditorApplication.isPlaying)return;
        Object.FindFirstObjectByType<CityForgeApp>().OpenAnimatedHorseQa();
        world=Object.FindFirstObjectByType<LotWorldController>();world.Session.Data.Props.Clear();
        world.PlacePropForQa(LotWorldController.HorseCarriagePropId,0,0);world.SetQaOrthographicSize(7f);
        team=Object.FindFirstObjectByType<HorseCarriageController>();
        // Show the driver/horse toward the normal lot camera.
        team.transform.localRotation=Quaternion.Euler(0,180,0);
        maxArticulation=maxError=0;commands=0;active=true;phase=0;rebuilt=false;nextAction=Time.time+3f;
        Directory.CreateDirectory("QA/HorseCarriageV02");
    }
    static bool Command(Vector2 target)
    {
        if(world==null)return false;
        typeof(LotWorldController).GetProperty("SelectedPropIndex").SetValue(world,0);
        var ok=world.CommandSelectedCarriage(target);if(ok)commands++;
        File.AppendAllText("QA/HorseCarriageV02/commands.txt",$"target={target} accepted={ok}\n");return ok;
    }
    [MenuItem("City Forge/QA/Carriage/Team Left Turn")]
    static void Left(){Command(new Vector2(5,-3));}
    [MenuItem("City Forge/QA/Carriage/Team Return Turn")]
    static void Return(){Command(new Vector2(0,0));}
    static void CheckRebuild()
    {
        var horsePos=team.Horse.position;var horseRot=team.Horse.rotation;
        var carriagePos=team.Carriage.position;var carriageRot=team.Carriage.rotation;var moving=team.IsMoving;var frontPos=team.Forecarriage.position;var frontRot=team.Forecarriage.rotation;
        var json=world.Session.Serialize();var data=JsonUtility.FromJson<LotSaveData>(json);
        var pose=data.Props[0];
        typeof(LotWorldController).GetMethod("RebuildPropPresentations",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).Invoke(world,null);
        var list=(System.Collections.IList)typeof(LotWorldController).GetField("_propPresentations",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).GetValue(world);
        team=((Transform)list[0]).GetComponent<HorseCarriageController>();
        var stable=Vector3.Distance(horsePos,team.Horse.position)<.001f&&Vector3.Distance(carriagePos,team.Carriage.position)<.001f&&Quaternion.Angle(horseRot,team.Horse.rotation)<.01f&&Quaternion.Angle(carriageRot,team.Carriage.rotation)<.01f&&Vector3.Distance(frontPos,team.Forecarriage.position)<.001f&&Quaternion.Angle(frontRot,team.Forecarriage.rotation)<.01f;
        File.AppendAllText("QA/HorseCarriageV02/rebuild.txt",$"wasMoving={moving} stillMoving={team.IsMoving} poseStable={stable} serializedPose={pose.HasCarriagePose}\n");
    }
    static void Observe()
    {
        if(!active||!EditorApplication.isPlaying||team==null)return;
        if(phase==0&&Time.time>=nextAction){Command(new Vector2(5,-3));phase=1;nextAction=Time.time+2;}
        if(phase==1&&team.IsMoving&&!rebuilt&&Time.time>=nextAction){CheckRebuild();rebuilt=true;}
        if(phase==1&&!team.IsMoving&&commands>0){phase=2;nextAction=Time.time+2;}
        if(phase==2&&Time.time>=nextAction){Command(new Vector2(0,0));phase=3;}
        if(phase==3&&!team.IsMoving){CheckRebuild();phase=4;}
        maxArticulation=Mathf.Max(maxArticulation,team.ArticulationDegrees);maxError=Mathf.Max(maxError,team.HitchError);
        if(Time.time<writeAt)return;writeAt=Time.time+.25f;
        var player=team.Horse.GetComponent<ThreeDimensionalCharacterAnimator>();
        File.WriteAllText("QA/HorseCarriageV02/live.txt",$"commands={commands}\nmoving={team.IsMoving}\nblocked={team.WasBlocked}\nhorse={team.Horse.position}\ncarriage={team.Carriage.position}\nhorseYaw={team.Horse.eulerAngles.y}\ncarriageYaw={team.Carriage.eulerAngles.y}\nfrontAxleYaw={team.Forecarriage.eulerAngles.y}\nshaftAngle={Quaternion.Angle(team.Horse.rotation,team.Forecarriage.rotation)}\naxleAngle={Quaternion.Angle(team.Carriage.rotation,team.Forecarriage.rotation)}\narticulation={team.ArticulationDegrees}\nmaxArticulation={maxArticulation}\nhitchError={team.HitchError}\nmaxHitchError={maxError}\nhorseAnimation={player.State}\nwheels={team.Carriage.GetComponent<CarriageWheelController>().WheelCount}\n");
    }
}
#endif

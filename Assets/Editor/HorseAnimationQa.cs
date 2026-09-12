#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using CityForgeV3.UI;
using CityForgeV3.World;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

[InitializeOnLoad]
public static class HorseAnimationQa
{
    [Serializable] sealed class Report
    {
        public bool complete, passed;
        public int horses;
        public bool idle, walk, trot, turnSteps;
        public float walkHoofMotion, trotHoofMotion, idleHoofMotion;
        public string clips;
    }
    static LotWorldController world;
    static HorseGaitController[] horses;
    static Transform[][] hooves;
    static Vector3[][] baseline;
    static Report report;
    static float started, lastTime;
    static int previousStage = -1;
    static bool pending, active;
    static int readyFrame;
    static HorseAnimationQa() { EditorApplication.update += Tick; }

    [MenuItem("City Forge/QA/Horse/Open Animated Pair")]
    static void Open()
    {
        if (!EditorApplication.isPlaying) { Debug.LogError("Enter Play mode first."); return; }
        Object.FindFirstObjectByType<CityForgeApp>().OpenAnimatedHorseQa();
        world = Object.FindFirstObjectByType<LotWorldController>();
        world.Session.Data.Props.Add(new PlacedProp {
            InstanceId = Guid.NewGuid().ToString("N"),
            PropId = LotWorldController.HorseAnimalId, PositionX = 0.75f, PositionZ = 0f
        });
        world.PlacePropForQa(LotWorldController.HorseAnimalId, -0.75f, 0f);
        readyFrame = Time.frameCount + 2;
        world.SetQaOrthographicSize(6.5f);
        pending = active = true; previousStage = -1;
        report = new Report();
    }
    static void Tick()
    {
        if (!active || !EditorApplication.isPlaying || world == null) return;
        if (pending)
        {
            if (Time.frameCount < readyFrame) return;
            horses = Object.FindObjectsByType<HorseGaitController>(FindObjectsSortMode.InstanceID);
            if (horses.Length != 2) return;
            hooves = horses.Select(h => h.GetComponentsInChildren<Transform>().Where(t => t.name.StartsWith("Hoof.")).ToArray()).ToArray();
            if (hooves.Any(h => h.Length != 4)) { Debug.LogError("Horse hoof bones missing"); active = false; return; }
            report.horses = horses.Length;
            report.clips = string.Join(",", horses[0].GetComponent<ThreeDimensionalCharacterAnimator>().States);
            started = lastTime = Time.time; pending = false;
        }
        if (horses.Any(h => h == null)) { active = false; return; }
        if (Time.time <= lastTime) return;
        lastTime = Time.time;
        var elapsed = Time.time - started;
        var t = elapsed % 26f;
        var stage = t < 4f ? 0 : t < 12f ? 1 : t < 18f ? 2 : t < 22f ? 3 : 4;
        var stageTime = t - (stage == 0 ? 0 : stage == 1 ? 4 : stage == 2 ? 12 : stage == 3 ? 18 : 22);
        if (stage != previousStage)
        {
            baseline = hooves.Select((set,i) => set.Select(h => horses[i].transform.InverseTransformPoint(h.position)).ToArray()).ToArray();
            previousStage = stage;
        }
        // Read evaluated bone poses before supplying the next motion sample.
        if (stageTime > 0.3f)
        {
            foreach (var h in horses)
            {
                var p = h.GetComponent<ThreeDimensionalCharacterAnimator>();
                if (stage == 0) report.idle |= p.IsPlaying("idle");
                if (stage == 1) report.walk |= p.IsPlaying("walk");
                if (stage == 2) report.trot |= p.IsPlaying("trot");
                if (stage == 3) report.turnSteps |= p.IsPlaying("walk");
            }
            var motion = 0f;
            for (var i=0;i<horses.Length;i++)
                for(var j=0;j<4;j++) motion = Mathf.Max(motion, Vector3.Distance(baseline[i][j], horses[i].transform.InverseTransformPoint(hooves[i][j].position)));
            if(stage==0) report.idleHoofMotion=Mathf.Max(report.idleHoofMotion,motion);
            if(stage==1) report.walkHoofMotion=Mathf.Max(report.walkHoofMotion,motion);
            if(stage==2) report.trotHoofMotion=Mathf.Max(report.trotHoofMotion,motion);
        }
        for(var i=0;i<horses.Length;i++)
        {
            var h=horses[i].transform;
            if(stage==0) { h.localPosition=new Vector3((i-.5f)*1.5f, h.localPosition.y, 0); h.localRotation=Quaternion.identity; }
            else if(stage==1 || stage==2)
            {
                var radius=2.2f+i*1.2f;
                var angle=stageTime*(stage==1 ? HorseGaitController.WalkMetersPerSecond : HorseGaitController.TrotMetersPerSecond)/2.2f;
                h.localPosition=new Vector3(radius*Mathf.Sin(angle),h.localPosition.y,radius*Mathf.Cos(angle));
                h.localRotation=Quaternion.Euler(0,90f+angle*Mathf.Rad2Deg,0);
            }
            else if(stage==3) h.localRotation=Quaternion.Euler(0,stageTime*40f,0);
            // Keep this generated QA lot's saved positions consistent with its live roots.
            var prop=world.Session.Data.Props.Where(p=>p.PropId==LotWorldController.HorseAnimalId).ElementAt(i);
            prop.PositionX=h.localPosition.x; prop.PositionZ=h.localPosition.z;
        }
        if(!report.complete && elapsed>25f)
        {
            report.complete=true;
            report.passed=report.horses==2 && report.idle && report.walk && report.trot && report.turnSteps && report.walkHoofMotion>.05f && report.trotHoofMotion>.05f && report.idleHoofMotion<.01f;
            Directory.CreateDirectory("QA/HorseV01");
            File.WriteAllText("QA/HorseV01/report.json", JsonUtility.ToJson(report,true));
            Debug.Log("CF_HORSE_QA "+JsonUtility.ToJson(report));
        }
    }
}
#endif

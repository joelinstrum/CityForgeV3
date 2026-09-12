using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CityForgeV3.UI;
using CityForgeV3.World;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

[InitializeOnLoad]
public static class BearBehaviorQa
{
    [Serializable] private sealed class Report
    {
        public string stage;
        public bool complete;
        public bool passed;
        public bool sawRoaming, sawIdle, headMoved, idleStayedStill, ignoredPerson;
        public bool musketRetreat, towerRetreat;
        public float roamDistance, musketDistanceGain, towerDistanceGain, idleHeadAngle;
        public string headName, playerState, clipStates;
        public List<string> failures = new();
    }
    private static Report report;
    private static LotWorldController world;
    private static float stageStart;
    private static Vector3 previousPosition;
    private static Quaternion previousHead;
    private static Quaternion idleHeadStart;
    private static BearMode previousMode;
    private static Vector2 threatPosition;
    private static float initialThreatDistance;
    private static int stage;
    private static readonly string ReportPath = Path.GetFullPath("QA/BearBehaviorV03/report.json");
    static BearBehaviorQa() { EditorApplication.update += Update; }

    [MenuItem("City Forge/QA/Bear Behaviors/Open Roaming Bear")]
    private static void Open()
    {
        var app = Object.FindFirstObjectByType<CityForgeApp>();
        if (!EditorApplication.isPlaying || app == null) { Debug.LogError("Enter Play Mode first."); return; }
        app.OpenAnimatedBearQa();
        world = Object.FindFirstObjectByType<LotWorldController>();
        world.SetQaOrthographicSize(5f);
    }

    [MenuItem("City Forge/QA/Bear Behaviors/Run Behavior Checks")]
    private static void Run()
    {
        Open(); if (world == null || !EditorApplication.isPlaying) return;
        report = new Report { stage = "ordinary-person-roaming-idle", idleStayedStill = true, ignoredPerson = true };
        Check(!BearBehavior.FearsPerson(LotWorldController.VictorianGentlemanCharacterId), "ordinary person classified as threat");
        Check(BearBehavior.FearsPerson(LotWorldController.MusketmanCharacterId), "musketman classification");
        Check(BearBehavior.FearsBuilding(LotWorldController.FortWatchtowerEvaluationId), "tower classification");
        Check(!BearBehavior.FearsBuilding(LotWorldController.NorwalkClockTowerEvaluationId), "clock tower classified as gun tower");
        var policy = new BearBehavior(); policy.Tick(0, false, 0); policy.Tick(12, false, 0);
        Check(policy.Mode == BearMode.Looking, "roam to idle transition");
        policy.Tick(13, true, 0); Check(policy.Mode == BearMode.Retreating, "threat interrupts idle");
        policy.Tick(14, false, 0); Check(policy.Mode == BearMode.Retreating, "retreat cooldown");
        policy.Tick(16, false, 0); Check(policy.Mode == BearMode.Roaming, "returns to roaming");
        Check(BearBehavior.ThreatRadius(false, true) > BearBehavior.ThreatRadius(false, false), "threat hysteresis");
        world.PlacePropForQa(LotWorldController.VictorianGentlemanCharacterId, 3f, 0f);
        stage = 0; stageStart = Time.time; previousPosition = Vector3.zero; previousMode = BearMode.Retreating;
        Write();
    }
    private static void Update()
    {
        if (report == null || report.complete || !EditorApplication.isPlaying || world == null) return;
        var bear = Object.FindFirstObjectByType<BearRoamingAgent>();
        if (bear == null) return;
        var head = bear.GetComponentsInChildren<Transform>().FirstOrDefault(t => t.name == "HEAD");
        var elapsed = Time.time - stageStart;
        report.headName = head == null ? "NOT FOUND: " + string.Join(",", bear.GetComponentsInChildren<Transform>().Select(t=>t.name)) : head.name;
        var player = bear.GetComponent<ThreeDimensionalCharacterAnimator>();
        report.playerState = player.State;
        report.clipStates = string.Join(",", player.States);
        if (stage == 0)
        {
            report.ignoredPerson &= !bear.ThreatDetected;
            if (bear.Mode == BearMode.Roaming)
            {
                report.sawRoaming = true;
                if (previousMode == BearMode.Roaming) report.roamDistance += Vector3.Distance(previousPosition, bear.transform.position);
            }
            if (bear.Mode == BearMode.Looking)
            {
                report.sawIdle = true;
                if (previousMode != BearMode.Looking && head != null) idleHeadStart = head.localRotation;
                if (previousMode == BearMode.Looking)
                {
                    report.idleStayedStill &= Vector3.Distance(previousPosition, bear.transform.position) < 0.005f;
                    if (head != null)
                    {
                        report.idleHeadAngle = Mathf.Max(report.idleHeadAngle, Quaternion.Angle(idleHeadStart, head.localRotation));
                        report.headMoved |= report.idleHeadAngle > 0.5f;
                    }
                }
            }
            if (elapsed > 30f)
            {
                Check(report.sawRoaming && report.roamDistance > 0.3f, "bear did not roam");
                Check(report.sawIdle && report.headMoved && report.idleStayedStill, "stationary head-look idle failed");
                Check(report.ignoredPerson, "bear feared ordinary person");
                BeginThreat(false); return;
            }
        }
        else
        {
            var pos = new Vector2(bear.transform.localPosition.x, bear.transform.localPosition.z);
            var gain = Vector2.Distance(pos, threatPosition) - initialThreatDistance;
            if (stage == 1) { report.musketRetreat |= bear.Mode == BearMode.Retreating; report.musketDistanceGain = gain; }
            else { report.towerRetreat |= bear.Mode == BearMode.Retreating; report.towerDistanceGain = gain; }
            if (elapsed > 12f)
            {
                Check((stage == 1 ? report.musketRetreat : report.towerRetreat) && gain > 0.3f,
                    stage == 1 ? "musketman retreat did not increase distance" : "tower retreat did not increase distance");
                if (stage == 1) { BeginThreat(true); return; }
                report.stage = "complete"; report.complete = true; report.passed = report.failures.Count == 0;
                Write(); Debug.Log("CF_BEAR_BEHAVIOR_QA " + JsonUtility.ToJson(report)); return;
            }
        }
        previousPosition = bear.transform.position; previousMode = bear.Mode;
        if (head != null) previousHead = head.localRotation;
    }
    private static void BeginThreat(bool tower)
    {
        Open(); threatPosition = new Vector2(7f, 0f); initialThreatDistance = 7f;
        if (tower) world.AddExperimentalBuilding3D(LotWorldController.FortWatchtowerEvaluationId, 7f, 0f, 0);
        else
        {
            world.PlacePropForQa(LotWorldController.MusketmanCharacterId, 7f, 0f);
            // Keep the probe threat stationary; gameplay behavior remains unchanged.
            var p = world.Session.Data.Props.Last(); p.AnimationState = "sit"; p.MovementX = p.MovementZ = 0;
        }
        stage = tower ? 2 : 1; stageStart = Time.time;
        report.stage = tower ? "tower-retreat" : "musketman-retreat"; Write();
    }
    private static void Check(bool ok, string failure) { if (!ok) report.failures.Add(failure); }
    private static void Write() { Directory.CreateDirectory(Path.GetDirectoryName(ReportPath)); File.WriteAllText(ReportPath, JsonUtility.ToJson(report, true)); }
}

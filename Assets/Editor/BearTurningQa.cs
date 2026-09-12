#if UNITY_EDITOR
using System.IO;
using System.Linq;
using CityForgeV3.UI;
using CityForgeV3.World;
using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public static class BearTurningQa
{
    static BearRoamingAgent bear;
    static Transform[] paws;
    static Vector3[] initialPaws;
    static float[] pawMotion;
    static float started, initialYaw, totalYaw;
    static bool walkThroughout;
    static bool pending;
    static BearTurningQa() { EditorApplication.update += Tick; }

    [MenuItem("City Forge/QA/Bear Behaviors/Check Turning Steps")]
    static void Run()
    {
        if (!EditorApplication.isPlaying) return;
        Object.FindFirstObjectByType<CityForgeApp>().OpenAnimatedBearQa();
        Object.FindFirstObjectByType<LotWorldController>().SetQaOrthographicSize(5f);
        pending = true;
    }
    static void Prepare()
    {
        bear = Object.FindFirstObjectByType<BearRoamingAgent>();
        if (bear == null) return;
        pending = false;
        bear.Direction = Vector2.up;
        bear.NextSteer = Time.time + 8f;
        bear.transform.localRotation = Quaternion.Euler(0, 180, 0);
        paws = bear.GetComponentsInChildren<Transform>().Where(t => t.name.StartsWith("Paw.")).ToArray();
        initialPaws = paws.Select(p => bear.transform.InverseTransformPoint(p.position)).ToArray();
        pawMotion = new float[paws.Length];
        started = Time.time; initialYaw = bear.transform.localEulerAngles.y;
        totalYaw = 0f; walkThroughout = true;
    }
    static void Tick()
    {
        if (!EditorApplication.isPlaying) { pending = false; bear = null; return; }
        if (pending) { Prepare(); return; }
        if (bear == null) return;
        var elapsed = Time.time - started;
        if (elapsed < 0.1f) return;
        var yaw = bear.transform.localEulerAngles.y;
        var change = Mathf.Abs(Mathf.DeltaAngle(initialYaw, yaw));
        initialYaw = yaw;
        if (change > 0.001f)
        {
            totalYaw += change;
            walkThroughout &= bear.GetComponent<ThreeDimensionalCharacterAnimator>().IsPlaying("walk");
            for (var i = 0; i < paws.Length; i++)
                pawMotion[i] = Mathf.Max(pawMotion[i], Vector3.Distance(initialPaws[i], bear.transform.InverseTransformPoint(paws[i].position)));
        }
        if (elapsed < 5f) return;
        var passed = totalYaw > 140f && walkThroughout && paws.Length == 4 && pawMotion.All(d => d > 0.02f);
        Directory.CreateDirectory("QA/BearTurningV05");
        File.WriteAllText("QA/BearTurningV05/report.txt", $"passed={passed}\nturnDegrees={totalYaw}\nwalkThroughoutTurn={walkThroughout}\npawCount={paws.Length}\npawMotionMeters={string.Join(",",pawMotion)}\n");
        Debug.Log($"CF_BEAR_TURN_QA passed={passed}");
        bear = null;
    }
}
#endif

#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using CityForgeV3.UI;
public static class DistrictMineQa
{
    [MenuItem("City Forge/QA/Coal Mine/Focus Industry Button")]static void Inspect(){if(EditorApplication.isPlaying)App?.FocusMineButtonQa();}
    [MenuItem("City Forge/QA/Coal Mine/Remake Mountain District")]static void Remake(){if(EditorApplication.isPlaying)App?.RemakeMountainDistrictQa();}
    static CityForgeApp App=>Object.FindFirstObjectByType<CityForgeApp>();
    [MenuItem("City Forge/QA/Coal Mine/Open Little River Bend")]static void Open(){if(EditorApplication.isPlaying)App?.OpenMineQa();}
    [MenuItem("City Forge/QA/Coal Mine/Industry Menu")]static void Menu(){if(EditorApplication.isPlaying)App?.ShowMineMenuQa();}
    [MenuItem("City Forge/QA/Coal Mine/Check Saved Reload")]static void Check(){if(EditorApplication.isPlaying)App?.CheckMineQa();}
    [MenuItem("City Forge/QA/Coal Mine/Undo Build")]static void Undo(){if(EditorApplication.isPlaying)App?.UndoMineQa();}
}
#endif

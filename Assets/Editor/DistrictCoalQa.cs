#if UNITY_EDITOR
using CityForgeV3.UI;
using UnityEditor;
using UnityEngine;
public static class DistrictCoalQa
{
    private static CityForgeApp App=>Object.FindFirstObjectByType<CityForgeApp>();
    [MenuItem("City Forge/QA/Coal/Open Little River Bend")] private static void Open(){if(EditorApplication.isPlaying)App?.OpenCoalQa();}
    [MenuItem("City Forge/QA/Coal/Close View")] private static void Close(){if(EditorApplication.isPlaying)App?.FocusCoalQa(true);}
    [MenuItem("City Forge/QA/Coal/Second Deposit")] private static void Second(){if(EditorApplication.isPlaying)App?.FocusCoalQa(true,1);}
    [MenuItem("City Forge/QA/Coal/District View")] private static void District(){if(EditorApplication.isPlaying)App?.FocusCoalQa(false);}
    [MenuItem("City Forge/QA/Coal/Check Saved Reload")] private static void Check(){if(EditorApplication.isPlaying)App?.CheckCoalQa();}
}
#endif

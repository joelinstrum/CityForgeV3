#if UNITY_EDITOR
using CityForgeV3.UI;
using UnityEditor;
using UnityEngine;
public static class DistrictHillOverlayQa
{
    private static CityForgeApp App=>Object.FindFirstObjectByType<CityForgeApp>();
    [MenuItem("City Forge/QA/Hill Overlay/Open Little River Bend")] private static void Open(){if(EditorApplication.isPlaying)App?.OpenHillOverlayQa();}
    [MenuItem("City Forge/QA/Hill Overlay/Close View")] private static void Close(){if(EditorApplication.isPlaying)App?.FocusHillOverlayQa(true);}
    [MenuItem("City Forge/QA/Hill Overlay/District View")] private static void District(){if(EditorApplication.isPlaying)App?.FocusHillOverlayQa(false);}
    [MenuItem("City Forge/QA/Hill Overlay/Overlay Off")] private static void Off(){if(EditorApplication.isPlaying)App?.ToggleHillOverlayQa(false);}
    [MenuItem("City Forge/QA/Hill Overlay/Overlay On")] private static void On(){if(EditorApplication.isPlaying)App?.ToggleHillOverlayQa(true);}
    [MenuItem("City Forge/QA/Hill Overlay/Check Saved Reload")] private static void Check(){if(EditorApplication.isPlaying)App?.CheckHillOverlayQa();}
}
#endif

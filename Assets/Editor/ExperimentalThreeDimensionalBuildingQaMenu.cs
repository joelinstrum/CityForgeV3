using CityForgeV3.UI;
using CityForgeV3.World;
using CityForgeV3.Buildings3D;
using UnityEditor;
using UnityEngine;
using System.Text;

public static class ExperimentalThreeDimensionalBuildingQaMenu
{
    [MenuItem("City Forge/QA/Open 3D Buildings Experimental Lot")]
    private static void Open()
    {
        if (!EditorApplication.isPlaying)
        {
            Debug.LogWarning(
                "Enter Play Mode, then run Open 3D Buildings Experimental Lot.");
            return;
        }
        var app = Object.FindFirstObjectByType<CityForgeApp>();
        if (app == null || !app.OpenExperimentalThreeDimensionalBuildingsQa())
            Debug.LogError("The City Forge runtime is not ready for 3D building QA.");
    }

    [MenuItem("City Forge/QA/Open Art Museum LOD Lot")]
    private static void OpenArtMuseum()
    {
        if (!EditorApplication.isPlaying)
        {
            Debug.LogWarning("Enter Play Mode, then open the Art Museum LOD lot.");
            return;
        }
        var app = Object.FindFirstObjectByType<CityForgeApp>();
        if (app == null || !app.OpenArtMuseumLodQa())
            Debug.LogError("The City Forge runtime is not ready for Art Museum QA.");
    }

    [MenuItem("City Forge/QA/Open Ivy Townhouse White LOD Lot")]
    private static void OpenIvyTownhouseWhite()
    {
        if (!EditorApplication.isPlaying)
        {
            Debug.LogWarning("Enter Play Mode, then open the Ivy Townhouse White LOD lot.");
            return;
        }
        var app = Object.FindFirstObjectByType<CityForgeApp>();
        if (app == null || !app.OpenIvyTownhouseWhiteLodQa())
            Debug.LogError("The City Forge runtime is not ready for Ivy Townhouse QA.");
    }

    [MenuItem("City Forge/QA/Open Plymouth Store LOD Lot")]
    private static void OpenPlymouthStore()
    {
        if (!EditorApplication.isPlaying)
        {
            Debug.LogWarning("Enter Play Mode, then open the Plymouth Store LOD lot.");
            return;
        }
        var app = Object.FindFirstObjectByType<CityForgeApp>();
        if (app == null || !app.OpenPlymouthStoreLodQa())
            Debug.LogError("The City Forge runtime is not ready for Plymouth Store QA.");
    }

    [MenuItem("City Forge/QA/Open Plymouth Store Side-by-Side Comparison")]
    private static void OpenPlymouthStoreComparison()
    {
        if (!EditorApplication.isPlaying)
        {
            Debug.LogWarning("Enter Play Mode, then open the Plymouth comparison lot.");
            return;
        }
        var app = Object.FindFirstObjectByType<CityForgeApp>();
        if (app == null || !app.OpenPlymouthStoreComparisonQa())
            Debug.LogError("The City Forge runtime is not ready for Plymouth comparison QA.");
    }

    [MenuItem("City Forge/QA/Open Gilded Age Mansion LOD Lot")]
    private static void OpenGildedAgeMansion()
    {
        if (!EditorApplication.isPlaying)
        {
            Debug.LogWarning("Enter Play Mode, then open the Gilded Age Mansion LOD lot.");
            return;
        }
        var app = Object.FindFirstObjectByType<CityForgeApp>();
        if (app == null)
        {
            Debug.LogError("The City Forge runtime is not ready for mansion QA.");
            return;
        }
        app.OpenGildedAgeMansionLodQa();
    }

    [MenuItem("City Forge/QA/Plymouth Store/Force LOD0")]
    private static void ForcePlymouthLod0() => ForcePlymouthLod(0);

    [MenuItem("City Forge/QA/Plymouth Store/Force LOD1")]
    private static void ForcePlymouthLod1() => ForcePlymouthLod(1);

    [MenuItem("City Forge/QA/Plymouth Store/Force LOD2")]
    private static void ForcePlymouthLod2() => ForcePlymouthLod(2);

    [MenuItem("City Forge/QA/Plymouth Store/Force LOD3")]
    private static void ForcePlymouthLod3() => ForcePlymouthLod(3);

    [MenuItem("City Forge/QA/Plymouth Store/Resume Automatic LOD")]
    private static void ResumePlymouthLod() => ForcePlymouthLod(-1);

    [MenuItem("City Forge/QA/Plymouth Store/Log Runtime Renderers")]
    private static void LogPlymouthRuntimeRenderers()
    {
        var report = new StringBuilder("Plymouth runtime renderers:\n");
        foreach (var instance in Object.FindObjectsByType<Building3DPackageInstance>(
                     FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (instance.Package == null) continue;
            report.AppendLine($"ROOT {Path(instance.transform)} active={instance.gameObject.activeInHierarchy}");
            foreach (var renderer in instance.GetComponentsInChildren<Renderer>(true))
                report.AppendLine($"  {Path(renderer.transform)} enabled={renderer.enabled} " +
                    $"active={renderer.gameObject.activeInHierarchy} cast={renderer.shadowCastingMode} " +
                    $"material={renderer.sharedMaterial?.name} boundsCenter={renderer.bounds.center} " +
                    $"boundsSize={renderer.bounds.size} localScale={renderer.transform.localScale} " +
                    $"localEuler={renderer.transform.localEulerAngles}");
        }
        Debug.Log(report.ToString());
    }

    private static string Path(Transform transform)
    {
        var result = transform.name;
        while (transform.parent != null)
        {
            transform = transform.parent;
            result = transform.name + "/" + result;
        }
        return result;
    }

    private static void ForcePlymouthLod(int index)
    {
        foreach (var instance in Object.FindObjectsByType<Building3DPackageInstance>(
                     FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (instance.Package == null ||
                instance.Package.AssetId != LotWorldController.PlymouthStoreProductionId ||
                !instance.gameObject.name.StartsWith("3D Building — Plymouth Store"))
                continue;
            instance.LodGroup.ForceLOD(index);
            Debug.Log(index < 0
                ? $"{instance.Package.AssetId} resumed automatic LOD selection."
                : $"{instance.Package.AssetId} forced to LOD{index}.");
        }
    }

    [MenuItem("City Forge/QA/3D Buildings Lighting/Morning")]
    private static void Morning() => SetTime(TimeOfDayPreset.Morning);

    [MenuItem("City Forge/QA/3D Buildings Lighting/Noon")]
    private static void Noon() => SetTime(TimeOfDayPreset.Noon);

    [MenuItem("City Forge/QA/3D Buildings Lighting/Afternoon")]
    private static void Afternoon() => SetTime(TimeOfDayPreset.Afternoon);

    [MenuItem("City Forge/QA/3D Buildings Lighting/Evening")]
    private static void Evening() => SetTime(TimeOfDayPreset.Evening);

    [MenuItem("City Forge/QA/3D Buildings Lighting/Night")]
    private static void Night() => SetTime(TimeOfDayPreset.Night);

    [MenuItem("City Forge/QA/3D Buildings Lighting/Verify Ivy Overlay")]
    private static void VerifyIvyOverlay()
    {
        foreach (var instance in Object.FindObjectsByType<Building3DPackageInstance>(
                     FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (instance.Package == null ||
                instance.Package.AssetId != LotWorldController.IvyTownhouseWhiteProductionId)
                continue;
            var overlay = instance.transform.Find("Representations/Night Lighting Overlay");
            var renderer = overlay == null
                ? null : overlay.GetComponentInChildren<Renderer>(true);
            Debug.Log($"Ivy overlay QA: present={overlay != null}, " +
                $"active={overlay != null && overlay.gameObject.activeInHierarchy}, " +
                $"renderer={renderer != null}, bounds={renderer?.bounds.ToString() ?? "none"}, " +
                $"material={renderer?.sharedMaterial?.name ?? "none"}.");
            return;
        }
        Debug.LogError("Ivy overlay QA: no runtime package instance found.");
    }

    [MenuItem("City Forge/QA/Select Art Museum 3D Building")]
    private static void SelectArtMuseum()
    {
        foreach (var world in Object.FindObjectsByType<LotWorldController>(
                     FindObjectsSortMode.None))
            if (world.SelectBuilding3DForQa(0))
                return;
        Debug.LogError("No runtime 3D building is available for selection QA.");
    }

    [MenuItem("City Forge/QA/Open Surface Layers and Road Shadows")]
    private static void OpenSurfaceLayersAndRoadShadows()
    {
        if (!EditorApplication.isPlaying)
        {
            Debug.LogWarning("Enter Play Mode, then open surface-layer QA.");
            return;
        }
        var app = Object.FindFirstObjectByType<CityForgeApp>();
        if (app == null || !app.OpenSurfaceLayersAndRoadShadowsQa())
            Debug.LogError("The City Forge runtime is not ready for surface QA.");
    }

    private static void SetTime(TimeOfDayPreset preset)
    {
        foreach (var world in Object.FindObjectsByType<LotWorldController>(
                     FindObjectsSortMode.None))
            if (world.ExperimentalBuilding3DCount > 0)
                world.SetTimeOfDay(preset);
    }
}

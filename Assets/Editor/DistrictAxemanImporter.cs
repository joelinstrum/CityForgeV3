#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
public sealed class DistrictAxemanImporter : AssetPostprocessor
{
    private bool IsAxeman=>assetPath=="Assets/Resources/Characters/AxemanLaborV01/Axeman.fbx";
    void OnPreprocessModel()
    {
        if(!IsAxeman)return;var importer=(ModelImporter)assetImporter;
        importer.animationType=ModelImporterAnimationType.Generic;importer.importAnimation=true;importer.materialImportMode=ModelImporterMaterialImportMode.ImportStandard;importer.importCameras=false;importer.importLights=false;importer.isReadable=false;
    }
    void OnPostprocessMaterial(Material material)
    {
        if(!IsAxeman)return;
        material.SetFloat("_Metallic",0);material.SetFloat("_Glossiness",.15f);
    }
}
#endif

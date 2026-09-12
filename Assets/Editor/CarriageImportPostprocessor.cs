using UnityEditor;
public sealed class CarriageImportPostprocessor : AssetPostprocessor
{
    void OnPreprocessModel()
    {
        if (!assetPath.EndsWith("/CarriageV01/Carriage_Rolling_Wheels_v01.fbx")) return;
        var m=(ModelImporter)assetImporter;
        m.importAnimation=false; m.importCameras=false; m.importLights=false;
        m.materialImportMode=ModelImporterMaterialImportMode.None;
        m.preserveHierarchy=true; m.optimizeGameObjects=false;
    }
}

using UnityEditor;
using UnityEngine;

// Keep the supplied quadruped's baked skeleton; no humanoid retargeting.
public sealed class BearQuadrupedImportPostprocessor : AssetPostprocessor
{
    private const string ModelPath = "Assets/CityForgeV3/Resources/CityForgeV3/Props/Animals/BearQuadrupedV02/Bear_Quadruped_Walk_Unity_v02.fbx";

    private void OnPreprocessModel()
    {
        if (assetPath != ModelPath && assetPath != "Assets/CityForgeV3/Resources/CityForgeV3/Props/Animals/BearBehaviorsV03/Bear_Walk_Idle_v03.fbx") return;
        var importer = (ModelImporter)assetImporter;
        importer.animationType = ModelImporterAnimationType.Generic;
        importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
        importer.importAnimation = true;
        importer.importCameras = false;
        importer.importLights = false;
        importer.optimizeGameObjects = false;
        importer.animationCompression = ModelImporterAnimationCompression.Off;
        importer.materialImportMode = ModelImporterMaterialImportMode.None;
    }

    private void OnPreprocessAnimation()
    {
        if (assetPath != ModelPath && assetPath != "Assets/CityForgeV3/Resources/CityForgeV3/Props/Animals/BearBehaviorsV03/Bear_Walk_Idle_v03.fbx") return;
        var importer = (ModelImporter)assetImporter;
        var clips = importer.defaultClipAnimations;
        foreach (var clip in clips)
        {
            clip.name = clip.takeName.IndexOf("Idle", System.StringComparison.OrdinalIgnoreCase) >= 0 ? "Bear_Idle" : "Bear_Walk";
            clip.loopTime = true;
            clip.loopPose = false;
            clip.wrapMode = WrapMode.Loop;
        }
        importer.clipAnimations = clips;
    }
}

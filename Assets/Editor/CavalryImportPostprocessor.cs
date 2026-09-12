using UnityEditor;
using UnityEngine;
public sealed class CavalryImportPostprocessor : AssetPostprocessor
{
    const string ModelPath = "Assets/CityForgeV3/Resources/CityForgeV3/Props/Animals/CavalryV01/Cavalry_Walk_Trot_Idle_v01.fbx";
    void OnPreprocessModel()
    {
        if (assetPath != ModelPath) return;
        var m = (ModelImporter)assetImporter;
        m.animationType = ModelImporterAnimationType.Generic;
        m.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
        m.importAnimation = true; m.importCameras = false; m.importLights = false;
        m.optimizeGameObjects = false; m.animationCompression = ModelImporterAnimationCompression.Off;
        m.materialImportMode = ModelImporterMaterialImportMode.None;
    }
    void OnPreprocessAnimation()
    {
        if (assetPath != ModelPath) return;
        var m = (ModelImporter)assetImporter;
        var clips = m.defaultClipAnimations;
        foreach (var c in clips)
        {
            c.name = c.takeName.Contains("Idle") ? "Cavalry_Idle" : c.takeName.Contains("Trot") ? "Cavalry_Trot" : "Cavalry_Walk";
            c.loopTime = true; c.loopPose = false; c.wrapMode = WrapMode.Loop;
        }
        m.clipAnimations = clips;
    }
}

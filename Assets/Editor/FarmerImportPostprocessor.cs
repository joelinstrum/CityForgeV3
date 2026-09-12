using UnityEditor;
using UnityEngine;
public sealed class FarmerImportPostprocessor : AssetPostprocessor
{
    const string ModelPath = "Assets/CityForgeV3/Resources/CityForgeV3/Props/Characters/FarmerV01/Farmer_Animated_v01.fbx";
    void OnPreprocessTexture()
    {
        if (!assetPath.Contains("/Characters/FarmerV01/")) return;
        var t=(TextureImporter)assetImporter;
        t.textureType=assetPath.EndsWith("/normal.jpg") ? TextureImporterType.NormalMap : TextureImporterType.Default;
        t.sRGBTexture=!assetPath.EndsWith("/normal.jpg");
        t.maxTextureSize=2048;
    }
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
            c.name = c.takeName.Contains("Idle") ? "Farmer_Idle" : c.takeName.Contains("Hoe") ? "Farmer_Hoe" : "Farmer_Walk";
            c.loopTime = true; c.loopPose = false; c.wrapMode = WrapMode.Loop;
        }
        m.clipAnimations = clips;
    }
}

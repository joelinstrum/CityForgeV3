using UnityEditor;

namespace CityForgeV3.Editor
{
    internal sealed class SwampWaterTextureImportPostprocessor : AssetPostprocessor
    {
        private const string Root =
            "Assets/CityForgeV3/Resources/CityForgeV3/Water/Swamp/";

        private void OnPreprocessTexture()
        {
            if (!assetPath.StartsWith(Root)) return;
            var importer = (TextureImporter)assetImporter;
            importer.maxTextureSize = 2048;
            importer.wrapMode = UnityEngine.TextureWrapMode.Repeat;
            importer.mipmapEnabled = true;
            importer.alphaIsTransparency = false;
            if (assetPath.Contains("_Normal"))
            {
                importer.textureType = TextureImporterType.NormalMap;
                importer.sRGBTexture = false;
            }
            else
            {
                importer.textureType = TextureImporterType.Default;
                importer.sRGBTexture = true;
            }
        }
    }
}

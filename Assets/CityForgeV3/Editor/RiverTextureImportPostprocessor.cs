using UnityEditor;
using UnityEngine;

namespace CityForgeV3.Editor
{
    internal sealed class RiverTextureImportPostprocessor : AssetPostprocessor
    {
        private const string Root =
            "Assets/CityForgeV3/Resources/CityForgeV3/Water/River/";

        // Every river layer is viewed at steep, widely varying scales. Filter
        // all of them during minification; UV animation remains intact while
        // distant water no longer aliases into pixel noise.
        public override uint GetVersion() => 5;

        private void OnPreprocessTexture()
        {
            if (!assetPath.StartsWith(Root)) return;
            var importer = (TextureImporter)assetImporter;
            importer.textureType = TextureImporterType.Default;
            importer.maxTextureSize = 2048;
            importer.wrapModeU = TextureWrapMode.Repeat;
            importer.wrapModeV = assetPath.EndsWith("river-texture.png")
                ? TextureWrapMode.Repeat
                : TextureWrapMode.Clamp;
            var waterArtwork = assetPath.EndsWith("river-texture.png") ||
                               assetPath.EndsWith("white-cap-river.png");
            var featheredBank = assetPath.Contains("grass-to-dirt") ||
                                assetPath.Contains("border");
            importer.mipmapEnabled = true;
            importer.mipMapBias = 0f;
            importer.mipMapsPreserveCoverage = featheredBank ||
                                                assetPath.EndsWith("white-cap-river.png");
            importer.alphaTestReferenceValue = 0.02f;
            importer.alphaIsTransparency = featheredBank;
            importer.sRGBTexture = true;
            importer.filterMode = FilterMode.Trilinear;
            importer.anisoLevel = 8;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
        }
    }
}

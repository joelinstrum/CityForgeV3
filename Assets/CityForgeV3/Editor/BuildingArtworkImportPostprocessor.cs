using UnityEditor;
using UnityEngine;

namespace CityForgeV3.Editor
{
    /// <summary>
    /// Keeps hybrid building billboards crisp at the editor's detail zooms.
    /// This is deliberately package-agnostic: every current and future
    /// building artwork image receives the same lossless import contract.
    /// </summary>
    [InitializeOnLoad]
    internal sealed class BuildingArtworkImportPostprocessor : AssetPostprocessor
    {
        private const string ImportContractVersionKey =
            "CityForgeV3.BuildingArtworkImportContract.V2";

        static BuildingArtworkImportPostprocessor()
        {
            EditorApplication.delayCall += ReimportExistingArtworkOnce;
        }

        private static void ReimportExistingArtworkOnce()
        {
            if (EditorPrefs.GetBool(ImportContractVersionKey, false))
                return;

            var guids = AssetDatabase.FindAssets(
                "t:Texture2D",
                new[] { "Assets/CityForgeV3/Resources/CityForgeV3/Buildings" });
            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                if (path.EndsWith(".png", System.StringComparison.OrdinalIgnoreCase))
                {
                    AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
                }
            }

            EditorPrefs.SetBool(ImportContractVersionKey, true);
        }

        private void OnPreprocessTexture()
        {
            if (!assetPath.Contains("/Resources/CityForgeV3/Buildings/") ||
                !assetPath.EndsWith(".png", System.StringComparison.OrdinalIgnoreCase))
                return;

            var importer = (TextureImporter)assetImporter;
            importer.textureType = TextureImporterType.Default;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.maxTextureSize = 4096;
            importer.mipmapEnabled = false;
            importer.npotScale = TextureImporterNPOTScale.None;
            importer.filterMode = FilterMode.Bilinear;
            importer.alphaIsTransparency = true;
        }
    }
}

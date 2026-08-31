using System;
using UnityEditor;
using UnityEngine;

namespace CityForgeV3.Editor
{
    /// <summary>
    /// Flora selection samples billboard alpha so transparent pixels do not
    /// steal hover/clicks from the tree actually beneath the pointer. Keep
    /// flora artwork CPU-readable whenever it is imported or regenerated.
    /// </summary>
    [InitializeOnLoad]
    internal sealed class FloraArtworkImportPostprocessor : AssetPostprocessor
    {
        private const string FloraRoot =
            "Assets/CityForgeV3/Resources/CityForgeV3/Flora/";
        private const string ImportContractVersionKey =
            "CityForgeV3.FloraArtworkImportContract.V1";

        static FloraArtworkImportPostprocessor()
        {
            EditorApplication.delayCall += ReimportExistingArtworkOnce;
        }

        private static void ReimportExistingArtworkOnce()
        {
            if (EditorPrefs.GetBool(ImportContractVersionKey, false))
                return;

            var guids = AssetDatabase.FindAssets(
                "t:Texture2D", new[] { FloraRoot.TrimEnd('/') });
            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                if (path.EndsWith(".png", StringComparison.OrdinalIgnoreCase))
                    AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            }

            EditorPrefs.SetBool(ImportContractVersionKey, true);
        }

        private void OnPreprocessTexture()
        {
            if (!assetPath.StartsWith(FloraRoot, StringComparison.Ordinal) ||
                !assetPath.EndsWith(".png", StringComparison.OrdinalIgnoreCase))
                return;

            var importer = (TextureImporter)assetImporter;
            importer.isReadable = true;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.npotScale = TextureImporterNPOTScale.None;
            importer.filterMode = FilterMode.Bilinear;
        }
    }
}

using UnityEditor;

namespace CityForge.Editor
{
    public sealed class Brownstone3DImportPostprocessor : AssetPostprocessor
    {
        private const string Brownstone22kFolder =
            "Assets/CityForgeV3/Resources/CityForgeV3/Buildings3D/BrownstoneBuilding22k/";

        private const string LowPolyBrownstoneFolder =
            "Assets/CityForgeV3/Resources/CityForgeV3/Buildings3D/LowPolyBrownstoneV01/";

        private const string ArtMuseumFolder =
            "Assets/CityForgeV3/Resources/CityForgeV3/Buildings3D/ArtMuseumProduction/";

        private bool IsProjectedShadowBuilding =>
            assetPath.StartsWith(Brownstone22kFolder) ||
            assetPath.StartsWith(LowPolyBrownstoneFolder) ||
            assetPath.StartsWith(ArtMuseumFolder);

        private void OnPreprocessModel()
        {
            if (!IsProjectedShadowBuilding) return;
            // Runtime ground-shadow projection reads the authored vertices.
            // Editor meshes remain readable even when this flag is off, which
            // previously made the QA view pass while standalone players built
            // an empty shadow mesh and logged "isReadable is false".
            ((ModelImporter)assetImporter).isReadable = true;
        }

        private void OnPreprocessTexture()
        {
            if (!assetPath.StartsWith(Brownstone22kFolder) &&
                !assetPath.StartsWith(ArtMuseumFolder)) return;
            var importer = (TextureImporter)assetImporter;
            importer.maxTextureSize = 2048;
            importer.mipmapEnabled = true;
            importer.textureCompression = TextureImporterCompression.Compressed;
            importer.compressionQuality = 75;
            importer.anisoLevel = 4;
            if (assetPath.Contains("_normal"))
            {
                importer.textureType = TextureImporterType.NormalMap;
                importer.sRGBTexture = false;
            }
            else if (assetPath.Contains("_metallic") ||
                     assetPath.Contains("_roughness") ||
                     assetPath.Contains("_rm") ||
                     assetPath.Contains("metallic-smoothness"))
            {
                importer.sRGBTexture = false;
            }
        }
    }
}

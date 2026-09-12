using UnityEngine;
namespace CityForgeV3.World
{
    // Each shadow owns its generated mesh; release it when flora is rebuilt.
    public sealed class DistrictFloraShadowMesh : MonoBehaviour
    {
        private void OnDestroy()
        {
            var mesh = GetComponent<MeshFilter>()?.sharedMesh;
            if (mesh == null) return;
            if (Application.isPlaying) Destroy(mesh);
            else DestroyImmediate(mesh);
        }
    }
}

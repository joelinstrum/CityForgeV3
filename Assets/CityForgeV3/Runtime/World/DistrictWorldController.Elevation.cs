using UnityEngine;
namespace CityForgeV3.World
{
    public sealed partial class DistrictWorldController
    {
        private DistrictElevation _elevation;
        private MeshCollider _terrainCollider;
        private RegionCityTile _terrainDistrict;
        private bool _buildingDistrict;
        public float TerrainElevation(float x,float z)=>_elevation?.Sample(x,z)??0;
        private bool TerrainRaycast(Ray ray,out Vector3 point)
        {
            point=default;if(_terrainCollider==null||!_terrainCollider.Raycast(ray,out var hit,10000))return false;
            point=_content.InverseTransformPoint(hit.point);return true;
        }
        private void RefreshElevation()
        {
            if(_buildingDistrict||_terrainDistrict?.Hills==null||_terrainDistrict.Hills.HeightMeters<=0||_groundRenderer==null)return;
            _elevation=new DistrictElevation(_terrainDistrict);
            ConfigureMountainGroundMaterial();
            var filter=_groundRenderer.GetComponent<MeshFilter>();var old=filter.sharedMesh;
            var mesh=_elevation.CreateMesh();filter.sharedMesh=mesh;_terrainCollider.sharedMesh=mesh;
            if(Application.isPlaying)Destroy(old);else DestroyImmediate(old);
            RefreshFlora(_terrainDistrict);
            RefreshNaturalResources(_terrainDistrict);
            _groundDecals?.Rebuild(this,_terrainDistrict,_widthMeters,_depthMeters);
            if(_grid!=null){var oldGrid=_grid.gameObject;oldGrid.SetActive(false);if(Application.isPlaying)Destroy(oldGrid);else DestroyImmediate(oldGrid);BuildGrid();}
        }
    }
}

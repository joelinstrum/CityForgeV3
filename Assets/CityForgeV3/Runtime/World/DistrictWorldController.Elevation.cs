using UnityEngine;
namespace CityForgeV3.World
{
    public sealed partial class DistrictWorldController
    {
        private DistrictElevation _elevation;
        private DistrictSurfaceCache _surfaceCache=new();
        private DistrictSurfaceCache.Changes _surfaceChanges=new(){Full=true};
        public int SurfaceCacheRevision => _surfaceCache.Revision;
        public int LastTerrainSamplesUpdated => _elevation?.LastUpdatedSampleCount??0;
        public void CommitSurfaceChanges()=>RefreshElevation(preservePresentations:true);
        private MeshCollider _terrainCollider;
        private RegionCityTile _terrainDistrict;
        private bool _buildingDistrict;
        public float TerrainElevation(float x,float z)=>_elevation?.Sample(x,z)??0;
        private bool TerrainRaycast(Ray ray,out Vector3 point)
        {
            point=default;if(_terrainCollider==null||!_terrainCollider.Raycast(ray,out var hit,10000))return false;
            point=_content.InverseTransformPoint(hit.point);return true;
        }
        private void RefreshElevation(bool preservePresentations = false, bool rebuildDecals = true)
        {
            if(_buildingDistrict)return;
            if(_terrainDistrict==null)return;
            _surfaceChanges=_surfaceCache.Update(_terrainDistrict);
            if(!_surfaceChanges.Any)return;
            if(((_terrainDistrict.Hills==null || _terrainDistrict.Hills.HeightMeters<=0) && !_surfaceChanges.Full) || _groundRenderer==null)
            {
                if(rebuildDecals)_groundDecals?.Refresh(this,_terrainDistrict,_widthMeters,_depthMeters,_surfaceChanges.Full?null:_surfaceChanges.Areas);
                return;
            }
            ConfigureMountainGroundMaterial();
            var filter=_groundRenderer.GetComponent<MeshFilter>();
            bool heightChanged=true;
            if(_surfaceChanges.Full || _elevation==null)
            {
                _elevation=new DistrictElevation(_terrainDistrict);
                var old=filter.sharedMesh;var mesh=_elevation.CreateMesh();filter.sharedMesh=mesh;_terrainCollider.sharedMesh=mesh;
                if(Application.isPlaying)Destroy(old);else DestroyImmediate(old);
            }
            else
            {
                heightChanged=_elevation.RefreshLocal(_terrainDistrict,_surfaceChanges.Areas);
                if(heightChanged){_elevation.UpdateMesh(filter.sharedMesh);_terrainCollider.sharedMesh=null;_terrainCollider.sharedMesh=filter.sharedMesh;}
            }
            if (!preservePresentations && _surfaceChanges.Full)
            {
                RefreshFlora(_terrainDistrict);
                RefreshNaturalResources(_terrainDistrict);
            }
            else if(heightChanged)
            {
                // A removed river changes relief, but existing trees retain their instances.
                foreach (var placed in _terrainDistrict.Flora)
                    if (_districtFloraPresentations.TryGetValue(placed.InstanceId, out var renderer) && renderer != null)
                    {
                        renderer.transform.localPosition = DistrictFloraPosition(placed);
                        renderer.sortingOrder = DistrictFloraSortingOrder(renderer.transform.localPosition);
                    }
                UpdateDistrictFloraShadows();
            }
            if(rebuildDecals)_groundDecals?.Refresh(this,_terrainDistrict,_widthMeters,_depthMeters,_surfaceChanges.Full?null:_surfaceChanges.Areas);
            if(heightChanged && _grid!=null){var oldGrid=_grid.gameObject;oldGrid.SetActive(false);if(Application.isPlaying)Destroy(oldGrid);else DestroyImmediate(oldGrid);BuildGrid();}
        }
    }
}

#if UNITY_EDITOR
using System;
using System.Linq;
using CityForgeV3.World;
using UnityEngine;
namespace CityForgeV3.UI
{
    public sealed partial class CityForgeApp
    {
        public void OpenLittleRiverBendHillsQa()
        {
            var region=RegionSaveStore.List().Select(r=>RegionSaveStore.Load(r.RegionId)).FirstOrDefault(r=>r?.Tiles.Any(t=>t.Name=="Little River Bend")==true);
            if(region==null)throw new Exception("Little River Bend saved district not found");
            var district=region.Tiles.First(t=>t.Name=="Little River Bend");
            _districtUndoQaSaveRoot=null;_openRegion=region;_openRegionWasCreatedThisSession=false;
            if(_lotWorld!=null)_lotWorld.gameObject.SetActive(false);
            SelectRegionTile(district.TileId);ClearDistrictUndo();_districtWorldCompositionKey="";
            _terraformZoomLevel=DistrictZoomLevel.LOD2;_terraformPanOffset=Vector2.zero;SelectDistrictCategory("Terrain");
            Show(AppScreen.DistrictTerraform);SetDistrictSimulationPaused(true);
        }
        public void ApplyLittleRiverBendHillsQa()
        {
            var d=FindSelectedRegionTile();if(d?.Name!="Little River Bend")throw new Exception("Open Little River Bend first");
            // Joe explicitly authorized landscape replacement in this saved district.
            if(d.Rivers.Count==0)d.Rivers.Add(DistrictRiverGenerator.Generate(d,DistrictRiverDirection.WestToEast,.65f,DistrictRiverDepth.Shallow,1209).River);
            _districtWorld.RefreshRivers(d);
            int cleared=d.Flora.RemoveAll(tree=>_districtWorld.IsUnderRiverWater(new Vector2(tree.NormalizedX,tree.NormalizedZ)));
            Debug.Log($"HILLS RIVER cleared {cleared} submerged flora from Little River Bend");
            ApplyDistrictHills(new(){Seed=1209,HeightMeters=45,Coverage=.75f});
            FocusLittleRiverBendHillsQa();CheckLittleRiverBendHillsQa();
        }
        public void FocusLittleRiverBendHillsQa()
        {
            var d=FindSelectedRegionTile();if(d?.Name!="Little River Bend")return;
            _terraformZoomLevel=DistrictZoomLevel.LOD2;_terraformPanOffset=new Vector2(-100,0);
            _districtWorld.SetZoom(_terraformZoomLevel);_districtWorld.SetPan(_terraformPanOffset);
        }
        public void CheckLittleRiverBendHillsQa()
        {
            var d=FindSelectedRegionTile();if(d?.Name!="Little River Bend")throw new Exception("Wrong district");
            var before=JsonUtility.ToJson(d);var height=new DistrictElevation(d);SaveDistrictEdit();
            _openRegion=RegionSaveStore.Load(_openRegion.RegionId);var after=_openRegion.Tiles.First(t=>t.TileId==d.TileId);
            if(JsonUtility.ToJson(after)!=before)throw new Exception("District changed on disk reload");
            ClearDistrictUndo();_districtWorldCompositionKey="";EnsureDistrictWorld(after);Show(AppScreen.DistrictTerraform);SetDistrictSimulationPaused(true);FocusLittleRiverBendHillsQa();
            var loaded=new DistrictElevation(after);if(!height.Heights.SequenceEqual(loaded.Heights))throw new Exception("Relief differs after reload");
            foreach(var tree in after.Flora)
            {
                var renderer=_districtWorld.GetComponentsInChildren<SpriteRenderer>().FirstOrDefault(r=>r.gameObject.name=="District Flora — "+tree.FloraId && Vector2.Distance(new Vector2(r.transform.localPosition.x,r.transform.localPosition.z),DistrictLabor.TreePoint(after,tree))<.01f);
                if(renderer==null)continue;var p=renderer.transform.localPosition;
                if(!_districtWorld.SampleRiverSurface(renderer.transform.position).HasValue && Mathf.Abs(p.y-(loaded.Sample(p.x,p.z)+.19f))>.01f)throw new Exception("Floating flora "+tree.InstanceId);
            }
            var ground=_districtWorld.GetComponentsInChildren<MeshCollider>().First(c=>c.sharedMesh.name=="District Elevation");
            for(int i=0;i<20;i++)
            {
                float x=-400+i*40,z=80;var ray=new Ray(new Vector3(x,200,z),Vector3.down);
                if(!ground.Raycast(ray,out var hit,500)||Mathf.Abs(hit.point.y-loaded.Sample(x,z))>.01f)throw new Exception("Ground picking differs from terrain");
            }
            Debug.Log($"HILLS QA PASS district={after.Name} peak={Mathf.Max(loaded.Heights):F2} flora={after.Flora.Count} rivers={after.Rivers.Count} lots={after.Lots.Count} savedReload=true collider=true");
        }
        public void ShowDistrictHillsQaModal()=>ComposeDistrictHillsModal();
    }
}
#endif

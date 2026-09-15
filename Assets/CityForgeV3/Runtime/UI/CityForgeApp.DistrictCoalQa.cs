#if UNITY_EDITOR
using System;
using System.Linq;
using UnityEngine;
using CityForgeV3.World;
namespace CityForgeV3.UI
{
    public sealed partial class CityForgeApp
    {
        public void OpenCoalQa(){OpenLittleRiverBendHillsQa();_root.schedule.Execute(()=>CheckCoalQa()).StartingIn(500);}
        public void FocusCoalQa(bool close,int index=0)
        {
            var d=FindSelectedRegionTile();if(d?.ResourceDeposits?.Count<1)return;
            var p=d.ResourceDeposits[Mathf.Min(index,d.ResourceDeposits.Count-1)];
            _districtEdgePanDirection=Vector2Int.zero;
            _terraformPanOffset=new Vector2((p.NormalizedX-.5f)*DistrictScale.SizeMeters(d.Width),(p.NormalizedZ-.5f)*DistrictScale.SizeMeters(d.Height));
            _terraformZoomLevel=close?DistrictZoomLevel.LOD0:DistrictZoomLevel.LOD2;
            _districtWorld.SetPan(_terraformPanOffset);_districtWorld.SetZoom(_terraformZoomLevel);
            if(close)_districtWorld.WorldCamera.orthographicSize=26;
        }
        public void CheckCoalQa()
        {
            var d=FindSelectedRegionTile();if(d?.Name!="Little River Bend")throw new Exception("Wrong coal review district");
            if(d.ResourceDeposits.Count<1||d.ResourceDeposits.Count>2)throw new Exception("Expected 1–2 deposits");
            var before=JsonUtility.ToJson(d);var inventory=JsonUtility.ToJson(d.ResourceInventory);
            var fixture=JsonUtility.FromJson<RegionCityTile>(before);fixture.Hills.HeightMeters=0;foreach(var deposit in fixture.ResourceDeposits)deposit.MineBuilt=false;
            DistrictNaturalResources.Ensure(fixture,new DistrictElevation(fixture));
            if(fixture.ResourceDeposits.Count!=0)throw new Exception("Coal on flat terrain");
            fixture=JsonUtility.FromJson<RegionCityTile>(before);fixture.Flora.Clear();
            DistrictNaturalResources.Ensure(fixture,new DistrictElevation(fixture));
            if(!fixture.ResourceDeposits.Select(JsonUtility.ToJson).SequenceEqual(d.ResourceDeposits.Select(JsonUtility.ToJson)))throw new Exception("Flora edit moved saved deposits");
            CheckLittleRiverBendHillsQa();d=FindSelectedRegionTile();
            if(JsonUtility.ToJson(d)!=before || JsonUtility.ToJson(d.ResourceInventory)!=inventory)throw new Exception("Coal or inventory changed on reload");
            var visible=_districtWorld.GetComponentsInChildren<DistrictCoalPresentation>().Single().GetComponentsInChildren<SpriteRenderer>();
            if(visible.Length!=d.ResourceDeposits.Count(p=>!p.MineBuilt)*3)throw new Exception("Expected three coal piles per deposit");
            foreach(var r in visible)
            {
                var p=r.transform.localPosition;
                if(Mathf.Abs(p.y-_districtWorld.TerrainElevation(p.x,p.z)-.19f)>.01f)throw new Exception("Coal grounding failed");
                if(_districtWorld.SampleRiverSurface(r.transform.position).HasValue)throw new Exception("Coal in water");
                if(Quaternion.Angle(r.transform.rotation,_districtWorld.WorldCamera.transform.rotation)>.1f)throw new Exception("Saved coal camera alignment failed");
                if(r.sprite==null||r.sharedMaterial.shader.name!="CityForgeV3/LitShadowReceivingSprite")throw new Exception("Coal presentation missing");
            }
            FocusCoalQa(false);
            Debug.Log("COAL QA PASS savedReload=true flatTerrain=true stableAfterFloraEdit=true inventoryUnchanged=true grounded=true count="+d.ResourceDeposits.Count+" deposits="+string.Join(";",d.ResourceDeposits.Select(JsonUtility.ToJson)));
        }
    }
}
#endif

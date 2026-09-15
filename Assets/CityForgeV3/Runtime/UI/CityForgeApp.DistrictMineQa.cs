#if UNITY_EDITOR
using System;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;
using CityForgeV3.World;
namespace CityForgeV3.UI
{
    public sealed partial class CityForgeApp
    {
        public void RemakeMountainDistrictQa()
        {
            OpenLittleRiverBendHillsQa();var d=FindSelectedRegionTile();
            if(!System.IO.File.Exists("/Users/joelinstrum/dev/CityForgeMCP/artifacts/terrain/mountains-v01/before-region.json"))System.IO.File.WriteAllText("/Users/joelinstrum/dev/CityForgeMCP/artifacts/terrain/mountains-v01/before-region.json",JsonUtility.ToJson(_openRegion,true));
            d.ResourceDeposits.Clear();d.NaturalResourceGenerationKey="";
            ApplyDistrictHills(new DistrictHillSettings{Mountains=true,Seed=1209,HeightMeters=180,Coverage=.45f});
            d=FindSelectedRegionTile();EnsureDistrictWorld(d);
            if(d.ResourceDeposits.Count==0)throw new Exception("No suitable mountain coal sites");
            if(!DistrictCoalMine.Build(d,d.ResourceDeposits[0]))throw new Exception("Mountain mine rejected");
            SaveDistrictEdit();_districtWorldCompositionKey="";EnsureDistrictWorld(d);Show(AppScreen.DistrictTerraform);
            CheckMineQa();
            var terrain=new DistrictElevation(FindSelectedRegionTile());
            foreach(var p in FindSelectedRegionTile().ResourceDeposits)
                if(!DistrictCoalMine.SuitableMountainSite(FindSelectedRegionTile(),terrain,p.NormalizedX,p.NormalizedZ,out _))throw new Exception("Coal at unsuitable mountain face");
            var gentle=JsonUtility.FromJson<RegionCityTile>(JsonUtility.ToJson(FindSelectedRegionTile()));gentle.Hills.Mountains=false;gentle.Hills.HeightMeters=45;
            if(DistrictNaturalResources.Generate(gentle,new DistrictElevation(gentle)).Count!=0)throw new Exception("Gentle hills produced coal");
            Debug.Log("MOUNTAIN QA PASS savedReload=true steepSites=true gentleHillsNoCoal=true count="+FindSelectedRegionTile().ResourceDeposits.Count);
        }
        public void OpenMineQa(){OpenLittleRiverBendHillsQa();_root.schedule.Execute(ComposeDistrictIndustryModal).StartingIn(500);}
        public void ShowMineMenuQa()=>ComposeDistrictIndustryModal();
        public void FocusMineButtonQa()=>_root.Q<Button>("district-industry-menu")?.Focus();
        public void CheckMineQa()
        {
            var d=FindSelectedRegionTile();if(d?.Name!="Little River Bend")throw new Exception("Wrong district");
            var mine=d.ResourceDeposits.FirstOrDefault(p=>p.MineBuilt);if(mine==null)throw new Exception("Build a mine through Industry first");
            var fixture=JsonUtility.FromJson<RegionCityTile>(JsonUtility.ToJson(d));var p=fixture.ResourceDeposits.First(x=>x.Id==mine.Id);
            if(DistrictCoalMine.Build(fixture,p))throw new Exception("Duplicate mine allowed");
            p.MineBuilt=false;fixture.Hills.HeightMeters=0;
            if(DistrictCoalMine.Build(fixture,p))throw new Exception("Mine on flat land allowed");
            fixture=JsonUtility.FromJson<RegionCityTile>(JsonUtility.ToJson(d));fixture.Hills.HeightMeters=0;
            DistrictNaturalResources.Ensure(fixture,new DistrictElevation(fixture));
            if(!fixture.ResourceDeposits.Any(x=>x.Id==mine.Id&&x.MineBuilt))throw new Exception("Terrain edit deleted mine");
            var before=JsonUtility.ToJson(d);CheckLittleRiverBendHillsQa();d=FindSelectedRegionTile();if(JsonUtility.ToJson(d)!=before)throw new Exception("Mine save changed");
            var visible=_districtWorld.GetComponentsInChildren<DistrictCoalMinePresentation>();
            if(visible.Length!=d.ResourceDeposits.Count(x=>x.MineBuilt))throw new Exception("Mine not rendered");
            foreach(var model in visible)
            {
                var position=model.transform.localPosition;var rear=-(model.transform.localRotation*Vector3.forward);
                float back=_districtWorld.TerrainElevation(position.x+rear.x*12,position.z+rear.z*12),front=_districtWorld.TerrainElevation(position.x-rear.x*12,position.z-rear.z*12);
                if(back<=front+.1f)throw new Exception("Mine rear does not point uphill");
                if(model.GetComponentsInChildren<MeshRenderer>().Length==0)throw new Exception("No mine geometry");
            }
            FocusDistrictMine(d.ResourceDeposits.First(x=>x.MineBuilt));_districtWorld.WorldCamera.orthographicSize=12;
            Debug.Log("MINE QA PASS savedReload=true uphill=true duplicateRejected=true flatRejected=true terrainEditPreserved=true count="+visible.Length);
        }
        public void UndoMineQa()
        {
            var d=FindSelectedRegionTile();int count=d.ResourceDeposits.Count(x=>x.MineBuilt);
            if(!UndoDistrictEdit()||d.ResourceDeposits.Count(x=>x.MineBuilt)!=count-1)throw new Exception("Mine undo failed");
            Debug.Log("MINE UNDO QA PASS");ComposeDistrictIndustryModal();
        }
    }
}
#endif

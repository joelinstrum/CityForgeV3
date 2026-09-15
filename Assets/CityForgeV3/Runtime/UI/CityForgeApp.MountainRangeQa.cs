#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using CityForgeV3.World;
using UnityEngine;
namespace CityForgeV3.UI
{
    public sealed partial class CityForgeApp
    {
        public void UpgradeMountainRangeQa()
        {
            OpenLittleRiverBendHillsQa();var d=FindSelectedRegionTile();
            string backup="/Users/joelinstrum/dev/CityForgeMCP/artifacts/terrain/mountain-ranges-v02/before-region.json";
            if(!File.Exists(backup))File.WriteAllText(backup,JsonUtility.ToJson(_openRegion,true));
            var old=new DistrictElevation(d);d.Hills.Version=2;d.Hills.PreserveLegacyCoalSites=true;var next=new DistrictElevation(d);
            foreach(var deposit in d.ResourceDeposits.Where(p=>p.MineBuilt))
            {
                float x=(deposit.NormalizedX-.5f)*old.Width,z=(deposit.NormalizedZ-.5f)*old.Depth;
                for(int dz=-30;dz<=30;dz+=5)for(int dx=-30;dx<=30;dx+=5)
                    if(Mathf.Abs(old.Sample(x+dx,z+dz)-next.Sample(x+dx,z+dz))>.001f)throw new Exception("Developed mine terrain changed");
            }
            SaveDistrictEdit();_districtWorldCompositionKey="";EnsureDistrictWorld(d);Show(AppScreen.DistrictTerraform);SetDistrictSimulationPaused(true);
        }
        public void MountainRangeOverviewQa()
        {
            _districtEdgePanDirection=Vector2Int.zero;_terraformPanOffset=Vector2.zero;_terraformZoomLevel=DistrictZoomLevel.LOD3;
            _districtWorld.SetZoom(_terraformZoomLevel);_districtWorld.SetPan(_terraformPanOffset);
        }
    }
}
#endif

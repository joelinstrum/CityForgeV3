using CityForgeV3.World;
using NUnit.Framework;
using UnityEngine;
namespace CityForgeV3.Tests.EditMode
{
    public class DistrictElevationTests
    {
        [Test] public void OldDistrictIsFlatAndSavedHillsAreDeterministic()
        {
            var old=JsonUtility.FromJson<RegionCityTile>("{\"Width\":2,\"Height\":2}");
            Assert.That(new DistrictElevation(old).Heights,Is.All.EqualTo(0));
            old.Hills=new(){Seed=123,HeightMeters=35,Coverage=.7f};var original=new DistrictElevation(old);
            var reload=new DistrictElevation(JsonUtility.FromJson<RegionCityTile>(JsonUtility.ToJson(old)));
            CollectionAssert.AreEqual(original.Heights,reload.Heights);Assert.Greater(Mathf.Max(original.Heights),10);
            old.Hills.Seed++;CollectionAssert.AreNotEqual(original.Heights,new DistrictElevation(old).Heights);
        }
        [Test] public void WaterRoadsAndDistrictEdgesStayLevel()
        {
            var d=new RegionCityTile{Hills=new(){HeightMeters=60,Coverage=1}};
            d.Rivers.Add(new(){WidthMeters=30,Points=new(){new(){X=0,Z=.5f},new(){X=1,Z=.5f}}});
            d.Roads.Add(new(){GridX=64,GridZ=80});var h=new DistrictElevation(d);
            for(float x=-600;x<=600;x+=25)Assert.AreEqual(0,h.Sample(x,0),.001);
            Assert.AreEqual(0,h.Sample(5,165),.001);Assert.AreEqual(0,h.Sample(-640,80),.001);
        }
        [Test] public void RenderedVerticesMatchSamplerAndGenerationDoesNotChangeUnityRandom()
        {
            Random.InitState(9182);var expected=Random.value;Random.InitState(9182);
            var d=new RegionCityTile{Hills=new(){HeightMeters=35}};var h=new DistrictElevation(d);var mesh=h.CreateMesh();
            try{foreach(var p in mesh.vertices)Assert.AreEqual(p.y,h.Sample(p.x,p.z),.0001);Assert.AreEqual(expected,Random.value);}
            finally{Object.DestroyImmediate(mesh);}
        }
        [TestCase(123),TestCase(1209),TestCase(42)]
        public void RollingHillsKeepLongSlopesAndFlatBreathingRoom(int seed)
        {
            var d=new RegionCityTile{Hills=new(){Seed=seed,HeightMeters=35,Coverage=.7f}};
            var h=new DistrictElevation(d);
            int flat=0,interior=0;float largestStep=0;
            for(int z=20;z<h.Rows-20;z++)for(int x=20;x<h.Columns-20;x++)
            {
                int i=z*(h.Columns+1)+x;float elevation=h.Heights[i];
                if(elevation<1)flat++;
                interior++;
                largestStep=Mathf.Max(largestStep,Mathf.Abs(elevation-h.Heights[i+1]));
            }
            Debug.Log($"ROLLING HILLS seed={seed} samples={interior} flat={flat} maxStep={largestStep:F3} peak={Mathf.Max(h.Heights):F2}");
            Assert.That(flat,Is.GreaterThan(interior/12),"rolling terrain needs nearly flat space");
            Assert.That(largestStep,Is.LessThan(2f),"adjacent five metre samples should form gentle slopes");
            Assert.That(Mathf.Max(h.Heights),Is.GreaterThan(8f));
        }
    }
}

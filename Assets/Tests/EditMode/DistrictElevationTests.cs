using CityForgeV3.World;
using NUnit.Framework;
using UnityEngine;
namespace CityForgeV3.Tests.EditMode
{
    public class DistrictElevationTests
    {
        [Test] public void RollingHillsRoundTheCornerJoinWithoutFlatteningThePeak()
        {
            var district=new RegionCityTile{Width=2,Height=2,
                Hills=new DistrictHillSettings{Version=2,Seed=1209,HeightMeters=45,Coverage=.6f}};
            var elevation=new DistrictElevation(district);
            float ridgeExcess=0;int samples=0;
            for(float distance=100;distance<=240;distance+=20)
            {
                float coordinate=elevation.Width*.5f-distance;
                float center=elevation.Sample(coordinate,coordinate);
                float left=elevation.Sample(coordinate+25,coordinate-25);
                float right=elevation.Sample(coordinate-25,coordinate+25);
                ridgeExcess+=center-(left+right)*.5f;
                samples++;
            }
            Assert.That(ridgeExcess/samples,Is.InRange(-.5f,.05f),
                "The front corner should roll across the diagonal without a pointed ridge or trough.");
            Assert.That(Mathf.Max(elevation.Heights),
                Is.EqualTo(45f*DistrictElevation.RollingHillVerticalScale).Within(.01f));
        }
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
        public void ContinuousRollingFieldKeepsLongSlopesAndCalmAreas(int seed)
        {
            var d=new RegionCityTile{Hills=new(){Seed=seed,HeightMeters=35,Coverage=.7f}};
            var h=new DistrictElevation(d);
            int gentle=0,raised=0,interior=0,maxX=0,maxZ=0;float largestStep=0;
            for(int z=20;z<h.Rows-20;z++)for(int x=20;x<h.Columns-20;x++)
            {
                int i=z*(h.Columns+1)+x;
                float step=Mathf.Max(Mathf.Abs(h.Heights[i]-h.Heights[i+1]),
                    Mathf.Abs(h.Heights[i]-h.Heights[i+h.Columns+1]));
                if(step<.25f)gentle++;
                if(h.Heights[i]>1f)raised++;
                interior++;
                if(step>largestStep){largestStep=step;maxX=x;maxZ=z;}
            }
            Debug.Log($"ROLLING FIELD seed={seed} samples={interior} gentle={gentle} raised={raised} maxStep={largestStep:F3} at=({maxX},{maxZ}) peak={Mathf.Max(h.Heights):F2}");
            Assert.That(gentle,Is.GreaterThan(interior/5),"rolling terrain needs substantial gently sloped space");
            Assert.That(raised,Is.GreaterThan(interior*2/3),"broad rises should remain connected through shallow valleys");
            Assert.That(largestStep,Is.LessThan(2f*DistrictElevation.RollingHillVerticalScale),"adjacent five metre samples should form gentle slopes");
            Assert.That(Mathf.Max(h.Heights),Is.GreaterThan(8f));
        }
        [TestCase(123),TestCase(1209),TestCase(42)]
        public void CoverageChangesBroadReliefPrevalenceWithoutChangingPeak(int seed)
        {
            var district=new RegionCityTile{Hills=new(){Seed=seed,HeightMeters=80,Coverage=.2f}};
            var sparse=new DistrictElevation(district);
            district.Hills.Coverage=.8f;
            var prevalent=new DistrictElevation(district);
            float sparseMean=0,prevalentMean=0;
            for(int i=0;i<sparse.Heights.Length;i++)
            {
                sparseMean+=sparse.Heights[i];prevalentMean+=prevalent.Heights[i];
            }
            Assert.That(prevalentMean,Is.GreaterThan(sparseMean));
            Assert.That(Mathf.Max(sparse.Heights),Is.EqualTo(80*DistrictElevation.RollingHillVerticalScale).Within(.01f));
            Assert.That(Mathf.Max(prevalent.Heights),Is.EqualTo(80*DistrictElevation.RollingHillVerticalScale).Within(.01f));
        }
        [Test] public void Seed150CoverageControlsOccupiedTerrainArea()
        {
            var district=new RegionCityTile{Hills=new(){Seed=150,HeightMeters=35,
                Coverage=.1f,VerticalReliefScale=1f}};
            var low=new DistrictElevation(district);
            district.Hills.Coverage=.4f;
            var moderate=new DistrictElevation(district);
            int lowRaised=0,moderateRaised=0,lowFlat=0,moderateFlat=0;
            for(int i=0;i<low.Heights.Length;i++)
            {
                if(low.Heights[i]>5)lowRaised++;
                if(moderate.Heights[i]>5)moderateRaised++;
                if(low.Heights[i]<.1f)lowFlat++;
                if(moderate.Heights[i]<.1f)moderateFlat++;
            }
            float count=low.Heights.Length;
            Assert.That(lowRaised/count,Is.LessThan(.25f));
            Assert.That(lowFlat/count,Is.GreaterThan(.6f));
            Assert.That(moderateRaised/count,Is.GreaterThan(.4f));
            Assert.That(moderateRaised-lowRaised,Is.GreaterThan(count*.2f));
            Assert.That(moderateFlat/count,Is.GreaterThan(.2f));
            Assert.That(Mathf.Max(low.Heights),Is.EqualTo(35*DistrictElevation.RollingHillVerticalScale).Within(.01f));
            Assert.That(Mathf.Max(moderate.Heights),Is.EqualTo(35*DistrictElevation.RollingHillVerticalScale).Within(.01f));
        }
        [TestCase(2),TestCase(4)]
        public void RollingHillVerticalScaleOnlyExaggeratesMeshY(int districtSize)
        {
            var district=new RegionCityTile{Width=districtSize,Height=districtSize,
                Hills=new(){Seed=1209,HeightMeters=80,Coverage=.4f,VerticalReliefScale=1f}};
            var elevation=new DistrictElevation(district);var mesh=elevation.CreateMesh();
            try
            {
                Assert.That(mesh.bounds.min.y,Is.EqualTo(0).Within(.001f));
                Assert.That(mesh.bounds.size.y,Is.EqualTo(80*DistrictElevation.RollingHillVerticalScale).Within(.01f));
                district.Hills.VerticalReliefScale=1.5f;
                var scaled=new DistrictElevation(district);var scaledMesh=scaled.CreateMesh();
                try
                {
                    Assert.That(scaledMesh.bounds.size.y,Is.EqualTo(120*DistrictElevation.RollingHillVerticalScale).Within(.02f));
                    var original=mesh.vertices;var exaggerated=scaledMesh.vertices;
                    for(int i=0;i<original.Length;i+=Mathf.Max(1,original.Length/256))
                    {
                        Assert.That(exaggerated[i].x,Is.EqualTo(original[i].x));
                        Assert.That(exaggerated[i].z,Is.EqualTo(original[i].z));
                        Assert.That(exaggerated[i].y,Is.EqualTo(original[i].y*1.5f).Within(.001f));
                    }
                }
                finally{Object.DestroyImmediate(scaledMesh);}
                district.Hills.HeightMeters=60;district.Hills.VerticalReliefScale=1f;
                var sixtyMesh=new DistrictElevation(district).CreateMesh();
                try
                {
                    Assert.That(sixtyMesh.bounds.size.y,Is.EqualTo(60*DistrictElevation.RollingHillVerticalScale).Within(.02f));
                    var eighty=mesh.vertices;var sixty=sixtyMesh.vertices;
                    for(int i=0;i<eighty.Length;i+=Mathf.Max(1,eighty.Length/256))
                        Assert.That(eighty[i].y,Is.EqualTo(sixty[i].y*(80f/60f)).Within(.001f));
                }
                finally{Object.DestroyImmediate(sixtyMesh);}
            }
            finally{Object.DestroyImmediate(mesh);}
        }
        [Test] public void OlderSavedHillsDefaultToOneTimesVerticalScale()
        {
            var district=JsonUtility.FromJson<RegionCityTile>(
                "{\"Width\":2,\"Height\":2,\"Hills\":{\"Seed\":1209,\"HeightMeters\":80,\"Coverage\":0.4}}");
            var mesh=new DistrictElevation(district).CreateMesh();
            try{Assert.That(mesh.bounds.size.y,Is.EqualTo(80*DistrictElevation.RollingHillVerticalScale).Within(.01f));}
            finally{Object.DestroyImmediate(mesh);}
        }
        [Test] public void DistrictBuildPublishesVerticallyExaggeratedMesh()
        {
            var host=new GameObject("Relief pipeline test");
            try
            {
                var district=new RegionCityTile{TileId="relief-pipeline",Width=2,Height=2,
                    Founded=true,Hills=new(){Seed=1209,HeightMeters=80,Coverage=.4f}};
                host.AddComponent<DistrictWorldController>().RebuildEntireDistrict(district,
                    DistrictBulkRebuildReason.TestFixture);
                Mesh ground=null;
                foreach(var filter in host.GetComponentsInChildren<MeshFilter>())
                    if(filter.sharedMesh?.name=="District Elevation")ground=filter.sharedMesh;
                Assert.That(ground,Is.Not.Null);
                Assert.That(ground.bounds.size.y,Is.EqualTo(80*DistrictElevation.RollingHillVerticalScale).Within(.01f));
            }
            finally{Object.DestroyImmediate(host);}
        }
    }
}

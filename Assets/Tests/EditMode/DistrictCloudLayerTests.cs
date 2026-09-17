using CityForgeV3.World;
using NUnit.Framework;
using UnityEngine;
namespace CityForgeV3.Tests
{
    public sealed class DistrictCloudLayerTests
    {
        [Test]
        public void SnowFallsTenSecondsHoldsTenAndMeltsFive()
        {
            Assert.That(DistrictRainStorm.EvaluateSnow(-1),Is.EqualTo(Vector2.zero));
            var falling=DistrictRainStorm.EvaluateSnow(5);
            Assert.That(falling.x,Is.GreaterThan(0));Assert.That(falling.y,Is.EqualTo(.5f).Within(.001f));
            foreach(float time in new[]{10f,15f,20f})
                Assert.That(DistrictRainStorm.EvaluateSnow(time),Is.EqualTo(new Vector2(0,1)));
            Assert.That(DistrictRainStorm.EvaluateSnow(22.5f),Is.EqualTo(new Vector2(0,.5f)));
            Assert.That(DistrictRainStorm.EvaluateSnow(25),Is.EqualTo(Vector2.zero));
            Assert.That(DistrictRainStorm.EvaluateSnow(30),Is.EqualTo(Vector2.zero));
        }
        [Test]
        public void SnowCoverReusesTerrainAndSwitchingWeatherResetsIt()
        {
            var go=new GameObject("Snow test");var eye=new GameObject("Snow camera");var terrain=new GameObject("Snow terrain");var mesh=new Mesh();
            try
            {
                var source=terrain.AddComponent<MeshFilter>();source.sharedMesh=mesh;
                var storm=go.AddComponent<DistrictRainStorm>();storm.Initialize(eye.AddComponent<Camera>(),640,640,0,null,source);
                storm.Begin(true);
                var cover=go.transform.Find("Temporary district snow cover");
                Assert.That(cover.GetComponent<MeshFilter>().sharedMesh,Is.SameAs(mesh));
                storm.Begin();storm.Clear();
                Assert.That(storm.SnowAccumulation,Is.Zero);
                Assert.That(cover.GetComponent<MeshRenderer>().enabled,Is.False);
                Assert.That(go.GetComponentsInChildren<MeshRenderer>(true).Length,Is.EqualTo(3));
                Assert.That(UnityEditor.ShaderUtil.ShaderHasError(Shader.Find("CityForgeV3/DistrictSnowCover")),Is.False);
            }
            finally{Object.DestroyImmediate(go);Object.DestroyImmediate(eye);Object.DestroyImmediate(terrain);Object.DestroyImmediate(mesh);}
        }
        [Test]
        public void RainMistBuildsDuringRainAndLingersAfterDropsStop()
        {
            Assert.That(DistrictRainStorm.EvaluateMist(4.9f),Is.Zero);
            Assert.That(DistrictRainStorm.EvaluateMist(5),Is.Zero);
            Assert.That(DistrictRainStorm.EvaluateMist(6),Is.InRange(.01f,.99f));
            Assert.That(DistrictRainStorm.EvaluateMist(7),Is.EqualTo(1));
            Assert.That(DistrictRainStorm.Evaluate(15).y,Is.Zero);
            Assert.That(DistrictRainStorm.EvaluateMist(15),Is.EqualTo(1));
            Assert.That(DistrictRainStorm.EvaluateMist(17),Is.EqualTo(.5f).Within(.001f));
            Assert.That(DistrictRainStorm.EvaluateMist(19),Is.Zero);
            Assert.That(DistrictRainStorm.EvaluateMist(25),Is.Zero);
        }
        [Test]
        public void StormRainWaitsForCompleteCoverAndEndsAfterTenSeconds()
        {
            for(float time=0;time<5;time+=.1f)
                Assert.That(DistrictRainStorm.Evaluate(time).y, Is.Zero);
            for(float time=5.1f;time<15;time+=.1f)
            {
                var state=DistrictRainStorm.Evaluate(time);
                Assert.That(state.x,Is.EqualTo(1));
                Assert.That(state.y,Is.GreaterThan(0));
            }
            Assert.That(DistrictRainStorm.Evaluate(15).y,Is.Zero);
            Assert.That(DistrictRainStorm.Evaluate(17).x,Is.InRange(.01f,.99f));
            Assert.That(DistrictRainStorm.Evaluate(19),Is.EqualTo(Vector2.zero));
        }
        [Test]
        public void StormCancelRestartReusesPresentation()
        {
            var go=new GameObject("Storm test"); var eye=new GameObject("Storm camera");
            try
            {
                var storm=go.AddComponent<DistrictRainStorm>();
                storm.Initialize(eye.AddComponent<Camera>(),640,640,0,null);
                storm.Begin(); Assert.That(storm.CurrentPhase,Is.EqualTo(DistrictRainStorm.Phase.Gathering));
                storm.Clear(); Assert.That(storm.CurrentPhase,Is.EqualTo(DistrictRainStorm.Phase.Clear));
                storm.Begin(); storm.Clear();
                Assert.That(go.GetComponentsInChildren<MeshRenderer>(true).Length,Is.EqualTo(2));
                Assert.That(storm.CurrentPhase,Is.EqualTo(DistrictRainStorm.Phase.Clear));
                foreach(var r in go.GetComponentsInChildren<MeshRenderer>(true))Assert.That(r.enabled,Is.False);
                Assert.That(UnityEditor.ShaderUtil.ShaderHasError(Shader.Find("CityForgeV3/DistrictStorm")),Is.False);
            }
            finally { Object.DestroyImmediate(go); Object.DestroyImmediate(eye); }
        }
        [TestCase(0,false)] [TestCase(1,false)] [TestCase(2,false)]
        [TestCase(3,false)] [TestCase(4,true)] [TestCase(5,true)]
        public void CloudsOnlyAtTwoFarthestZooms(int zoom,bool visible)
            => Assert.That(DistrictCloudLayer.VisibleAt((DistrictZoomLevel)zoom),Is.EqualTo(visible));
        [TestCase(640f)] [TestCase(2560f)] [TestCase(7680f)]
        public void SameCloudTravelsContinuouslyAcrossDistrictSizes(float size)
        {
            // Remain within one lifetime: movement must be translation, not respawning.
            var a=DistrictCloudLayer.EvaluateMotion(20,0,113,size,size);
            var b=DistrictCloudLayer.EvaluateMotion(25,0,113,size,size);
            float scale=size/640f;
            Assert.That(a.z,Is.GreaterThan(0));
            Assert.That(b.z,Is.GreaterThan(0));
            Assert.That(b.x-a.x,Is.EqualTo(8.25f*scale).Within(.002f));
            Assert.That(b.y-a.y,Is.EqualTo(2.625f*scale).Within(.002f));
        }
        [Test]
        public void ZoomReusesBoundedCloudMeshesAndSharedTerrain()
        {
            var root=new GameObject("Cloud test");var terrain=new GameObject("Terrain test");
            var mesh=new Mesh();
            try
            {
                var filter=terrain.AddComponent<MeshFilter>();filter.sharedMesh=mesh;
                var layer=root.AddComponent<DistrictCloudLayer>();
                layer.Initialize(2560,2560,0,Quaternion.Euler(20,45,0),filter);
                var filters=root.GetComponentsInChildren<MeshFilter>();
                Assert.That(filters.Length,Is.EqualTo(2));
                Assert.That(filters[0].sharedMesh.vertexCount,Is.EqualTo(DistrictCloudLayer.CloudCount*4));
                Assert.That(filters[1].sharedMesh,Is.SameAs(mesh));
                var body=filters[0].sharedMesh;
                for(int i=0;i<6;i++)
                {
                    layer.SetZoom((DistrictZoomLevel)i);
                    Assert.That(root.activeSelf,Is.True);
                    Assert.That(filters[0].GetComponent<MeshRenderer>().enabled,Is.EqualTo(i>=4));
                    Assert.That(filters[1].GetComponent<MeshRenderer>().enabled,Is.True);
                }
                Assert.That(filters[0].sharedMesh,Is.SameAs(body));
                Assert.That(UnityEditor.ShaderUtil.ShaderHasError(Shader.Find("CityForgeV3/DistantClouds")),Is.False);
                var texture=Resources.Load<Texture2D>(DistrictCloudLayer.TextureResource);
                Assert.That(texture.mipmapCount,Is.GreaterThan(1));
                Assert.That(texture.wrapMode,Is.EqualTo(TextureWrapMode.Clamp));
            }
            finally{Object.DestroyImmediate(root);Object.DestroyImmediate(terrain);Object.DestroyImmediate(mesh);}
        }
    }
}

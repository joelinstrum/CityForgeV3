using CityForgeV3.World;
using NUnit.Framework;
using UnityEngine;
namespace CityForgeV3.Tests
{
    public sealed class DistrictCloudLayerTests
    {
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

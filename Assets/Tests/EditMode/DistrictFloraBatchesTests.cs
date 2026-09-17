using System.Linq;
using CityForgeV3.World;
using NUnit.Framework;
using UnityEngine;

public class DistrictFloraBatchesTests
{
    GameObject root; Texture2D texture; Sprite sprite; Material material;
    [SetUp] public void SetUp()
    {
        root = new GameObject("Batch test"); texture = new Texture2D(4, 4);
        sprite = Sprite.Create(texture, new Rect(0, 0, 4, 4), Vector2.zero, 1, 0, SpriteMeshType.FullRect);
        material = new Material(Shader.Find("CityForgeV3/LitShadowReceivingSprite"));
    }
    [TearDown] public void TearDown()
    {
        Object.DestroyImmediate(root); Object.DestroyImmediate(sprite);
        Object.DestroyImmediate(texture); Object.DestroyImmediate(material);
    }
    SpriteRenderer Tree(float x)
    {
        var go = new GameObject("Tree"); go.transform.SetParent(root.transform); go.transform.localPosition = new Vector3(x, 0, 0);
        var r = go.AddComponent<SpriteRenderer>(); r.sprite = sprite; r.sharedMaterial = material;
        FloraTreeRepairs.Apply(r, "vendor-willow");
        go.AddComponent<DistrictSelectable>().Configure(new DistrictSelectionRef(DistrictSelectionKind.Flora, x.ToString()), "Tree", "", new Renderer[] { r }, false);
        return r;
    }
    MeshRenderer[] Batches() => root.GetComponentsInChildren<MeshRenderer>().Where(r => r.name == "Flora batch").ToArray();
    [TestCase(0)] [TestCase(1)] [TestCase(2)] [TestCase(3)] [TestCase(4)]
    public void ClusterShadowsUseFiveDistinctGroundContactsAndSoftEdges(int variant)
    {
        texture.name = ForestClusterCatalog.Id(variant) + "-summer";
        var tree = Tree(0); tree.transform.rotation = Quaternion.Euler(35, 45, 0);
        var item = new GameObject("District Flora Shadow"); item.transform.SetParent(tree.transform, false);
        var mesh = new Mesh(); item.AddComponent<MeshFilter>().sharedMesh = mesh;
        item.AddComponent<DistrictFloraShadowMesh>();
        var shadow = item.AddComponent<MeshRenderer>();
        var contacts = new System.Collections.Generic.List<Vector3>();
        Assert.True(ForestClusterShadows.Update(tree, shadow, new Vector3(.3f,-1,.2f).normalized,
            _ => 0, foot => { contacts.Add(foot); foot.y = 0; return foot; }));
        Assert.AreEqual(5, contacts.Distinct().Count());
        Assert.True(mesh.colors.Any(c => c.r == 0), "Feathered canopy boundary");
        Assert.True(mesh.colors.Any(c => c.r > .5f), "Visible shadow interior");
        Assert.True(mesh.vertices.All(v => Mathf.Abs(shadow.transform.TransformPoint(v).y - .031f) < .001f));
        var first = mesh.vertices;
        ForestClusterShadows.Update(tree, shadow, new Vector3(-.3f,-1,-.2f).normalized,
            _ => 0, foot => { foot.y = 0; return foot; });
        Assert.AreNotEqual(first[6], mesh.vertices[6], "Canopies follow the sun");
    }
    [Test] public void NearbyCopiesShareOneMeshAndRemainPickable()
    {
        var a = Tree(10); var b = Tree(20); var batches = root.AddComponent<DistrictFloraBatches>(); batches.Build(new[] { a, b });
        Assert.AreEqual(1, Batches().Length); Assert.AreEqual(sprite.vertices.Length * 2, Batches()[0].GetComponent<MeshFilter>().sharedMesh.vertexCount);
        Assert.True(a.forceRenderingOff); Assert.True(a.enabled);
        Assert.True(a.GetComponent<DistrictSelectable>().Hit(new Ray(new Vector3(11, 1, -10), Vector3.forward), out _));
        var properties = new MaterialPropertyBlock(); Batches()[0].GetPropertyBlock(properties);
        Assert.AreSame(texture, properties.GetTexture("_MainTex")); Assert.AreEqual(1.4f, properties.GetFloat("_FloraSaturation"));
    }
    [Test] public void HarvestOrMoveRebuildsOnlyTheAffectedCell()
    {
        var a = Tree(10); var b = Tree(20); var far = Tree(600);
        var batches = root.AddComponent<DistrictFloraBatches>(); batches.Build(new[] { a, b, far });
        var untouched = Batches().Single(r => r.bounds.center.x > 500);
        batches.Remove(a);
        Assert.False(a.forceRenderingOff); Assert.True(b.forceRenderingOff); Assert.True(far.forceRenderingOff);
        Assert.AreSame(untouched, Batches().Single(r => r.bounds.center.x > 500)); Assert.AreEqual(2, Batches().Length);
        Assert.AreEqual(sprite.vertices.Length, Batches().Single(r => r.bounds.center.x < 100).GetComponent<MeshFilter>().sharedMesh.vertexCount);
        batches.Remove(a); Assert.AreEqual(2, Batches().Length);
        batches.Add(a); Assert.True(a.forceRenderingOff); Assert.AreEqual(2, Batches().Length);
        Assert.AreSame(untouched, Batches().Single(r => r.bounds.center.x > 500));
    }
    [Test] public void HarvestStateDoesNotInvalidateSpatialComposition()
    {
        var d = new RegionCityTile(); var tree = new PlacedDistrictFlora { InstanceId = "tree", FloraId = "cilician-fir" }; d.Flora.Add(tree);
        var method = typeof(CityForgeV3.UI.CityForgeApp).GetMethod("DistrictCompositionKey", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);
        var before = method.Invoke(null, new object[] { d });
        tree.HarvestState = DistrictTreeHarvestState.Stump; tree.HarvestDirection = 3; tree.RemainingWood = 0;
        Assert.AreEqual(before, method.Invoke(null, new object[] { d }));
        tree.NormalizedX += .1f;
        Assert.AreNotEqual(before, method.Invoke(null, new object[] { d }));
    }
    [Test] public void LightingRebuildUpdatesTintWithoutAccumulatingMeshes()
    {
        var a = Tree(10); var batches = root.AddComponent<DistrictFloraBatches>(); batches.Build(new[] { a });
        a.color = Color.red; batches.Rebuild(); batches.Rebuild();
        Assert.AreEqual(1, Batches().Length);
        Assert.True(Batches()[0].GetComponent<MeshFilter>().sharedMesh.colors.All(c => c == Color.red));
    }
}

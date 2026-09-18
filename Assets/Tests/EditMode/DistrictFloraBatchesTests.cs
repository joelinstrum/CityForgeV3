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
        // Both axis-aligned and noon sun must retain two-dimensional shadows.
        foreach (var light in new[] { Vector3.down, new Vector3(1,-1,0).normalized, new Vector3(0,-1,1).normalized })
        {
            ForestClusterShadows.Update(tree, shadow, light, _ => 0, foot => { foot.y = 0; return foot; });
            var points = mesh.vertices.Select(shadow.transform.TransformPoint).ToArray();
            // Per tree: 17 contact vertices, 4 trunk vertices, then canopy center/rings.
            var center = points[21]; float area = 0;
            for (int i=0;i<16;i++) area += Vector3.Cross(points[22+i]-center, points[22+(i+1)%16]-center).magnitude;
            Assert.Greater(area, .01f, "Canopy must not collapse along the sun axis");
        }
        var first = mesh.vertices;
        ForestClusterShadows.Update(tree, shadow, new Vector3(-.3f,-1,-.2f).normalized,
            _ => 0, foot => { foot.y = 0; return foot; });
        Assert.True(first.Where((v, i) => (v - mesh.vertices[i]).sqrMagnitude > .001f).Any(), "Canopies follow the sun");
    }
    [Test] public void AllSavedClusterIdsResolveTwoSeasonalPalettesWithRealAlpha()
    {
        foreach (var season in new[] { SeasonPreset.Summer, SeasonPreset.Autumn, SeasonPreset.Winter })
        {
            var paths = Enumerable.Range(0, 5).Select(i => LotWorldController.ResolveFloraResourcePath(ForestClusterCatalog.Id(i), season)).ToArray();
            Assert.AreEqual(2, paths.Distinct().Count(), "Saved IDs reuse two textures, not five duplicates");
            foreach (var path in paths.Distinct())
            {
                var art = Resources.Load<Texture2D>(path);
                Assert.NotNull(art, path); Assert.AreEqual(1254, art.width); Assert.AreEqual(1254, art.height);
                Assert.Greater(art.mipmapCount, 1); Assert.AreEqual(TextureWrapMode.Clamp, art.wrapMode);
                Assert.True(ForestClusterCatalog.IsTexture(art.name));
                Assert.AreEqual(ForestClusterCatalog.Pivot, LotWorldController.FloraPivot(art.name));
                var pixels = art.GetPixels32();
                Assert.True(pixels.Any(c => c.a == 0)); Assert.True(pixels.Any(c => c.a > 250));
            }
        }
        Assert.AreEqual(ForestClusterCatalog.ResourcePath(ForestClusterCatalog.Id(0)),
            ForestClusterCatalog.ResourcePath(ForestClusterCatalog.Id(0), SeasonPreset.Spring));
        CollectionAssert.AreEqual(new[] { SeasonPreset.Summer, SeasonPreset.Autumn, SeasonPreset.Winter, SeasonPreset.Spring, SeasonPreset.Summer },
            Enumerable.Range(0, 5).Select(ForestClusterCatalog.SeasonForIndex));
    }
    [Test] public void WinterShadowsRetainFirButOpenDeciduousCanopies()
    {
        texture.name = "forest-cluster-01-summer";
        var tree = Tree(0);
        var item = new GameObject("District Flora Shadow"); item.transform.SetParent(tree.transform, false);
        var mesh = new Mesh(); item.AddComponent<MeshFilter>().sharedMesh = mesh;
        item.AddComponent<DistrictFloraShadowMesh>(); var shadow = item.AddComponent<MeshRenderer>();
        ForestClusterShadows.Update(tree, shadow, Vector3.down, p => 0, p => new Vector3(p.x, 0, p.z));
        int summerVertices = mesh.vertexCount;
        texture.name = "forest-cluster-01-winter";
        int contacts = 0;
        Assert.True(ForestClusterShadows.Update(tree, shadow, Vector3.down, p => 0,
            p => { contacts++; return new Vector3(p.x, 0, p.z); }));
        Assert.AreEqual(5, contacts); Assert.Less(mesh.vertexCount, summerVertices);
        Assert.True(mesh.colors.Any(c => c.r == .8f), "Fir retains opaque canopy proxy");
        Assert.True(mesh.colors.Any(c => c.r == .1f), "Bare branches and light contact shade");
    }
    [Test] public void CilicianFirUsesTheRealisticEvergreenArtworkInEverySeason()
    {
        foreach (var season in new[] { SeasonPreset.Spring, SeasonPreset.Summer,
            SeasonPreset.Autumn, SeasonPreset.Winter })
        {
            var path = LotWorldController.ResolveFloraResourcePath("cilician-fir", season);
            Assert.AreEqual(FloraTreeRepairs.RealisticCilicianRoot + "cilician-fir-" +
                season.ToString().ToLowerInvariant(), path);
            var art = Resources.Load<Texture2D>(path);
            Assert.NotNull(art, path);
            Assert.AreEqual(1024, art.width); Assert.AreEqual(1536, art.height);
            Assert.Greater(art.mipmapCount, 1); Assert.AreEqual(TextureWrapMode.Clamp, art.wrapMode);
            Assert.True(art.GetPixels32().Any(pixel => pixel.a == 0));
            Assert.True(art.GetPixels32().Any(pixel => pixel.a > 250));
            Assert.AreEqual(new Vector2(.5f, 0f), LotWorldController.FloraPivot(art.name));
            Assert.AreEqual(105f, LotWorldController.FloraPixelsPerUnit("cilician-fir", art.name));
        }
    }
    [Test] public void LondonPlaneAUsesTheRealisticSeasonalArtwork()
    {
        foreach (var season in new[] { SeasonPreset.Spring, SeasonPreset.Summer,
            SeasonPreset.Autumn, SeasonPreset.Winter })
        {
            var path = LotWorldController.ResolveFloraResourcePath("london-plane-a", season);
            Assert.AreEqual(FloraTreeRepairs.RealisticLondonPlaneRoot + "london-plane-a-" +
                season.ToString().ToLowerInvariant(), path);
            var art = Resources.Load<Texture2D>(path);
            Assert.NotNull(art, path);
            Assert.AreEqual(1024, art.width); Assert.AreEqual(1536, art.height);
            Assert.Greater(art.mipmapCount, 1); Assert.AreEqual(TextureWrapMode.Clamp, art.wrapMode);
            Assert.True(art.GetPixels32().Any(pixel => pixel.a == 0));
            Assert.True(art.GetPixels32().Any(pixel => pixel.a > 250));
            var sprite = Sprite.Create(art, new Rect(0, 0, art.width, art.height),
                LotWorldController.FloraPivot(art.name), 96f);
            var pixels = art.GetPixels32();
            int firstOpaque = System.Array.FindIndex(pixels, p => p.a > 128);
            float footY = firstOpaque / art.width;
            Assert.That(Mathf.Abs(footY - sprite.pivot.y), Is.LessThanOrEqualTo(1f),
                "Visible trunk must begin at the shared tree/selection/shadow origin, within one texel.");
            Object.DestroyImmediate(sprite);
            Assert.AreEqual(96f, LotWorldController.FloraPixelsPerUnit("london-plane-a", art.name));
        }
    }
    [Test] public void LondonPlaneBUsesTheHighIsometricArtBeforeWinter()
    {
        foreach (var season in new[] { SeasonPreset.Spring, SeasonPreset.Summer,
            SeasonPreset.Autumn })
        {
            var path = LotWorldController.ResolveFloraResourcePath("london-plane-b", season);
            Assert.AreEqual(FloraTreeRepairs.RealisticLondonPlaneRoot + "london-plane-b-" +
                season.ToString().ToLowerInvariant(), path);
            var art = Resources.Load<Texture2D>(path);
            Assert.NotNull(art, path); Assert.AreEqual(1024, art.width);
            Assert.AreEqual(1536, art.height); Assert.AreEqual(TextureWrapMode.Clamp, art.wrapMode);
            Assert.AreEqual(new Vector2(.5f, .065f), LotWorldController.FloraPivot(art.name));
            Assert.AreEqual(96f, LotWorldController.FloraPixelsPerUnit("london-plane-b", art.name));
        }
        Assert.AreEqual("CityForgeV3/Flora/LegacyTreesV01/london-plane-b-winter",
            LotWorldController.ResolveFloraResourcePath("london-plane-b", SeasonPreset.Winter));
    }
    [Test] public void BatchedSeasonSwapRetainsUnrelatedCellAndSelectionHandle()
    {
        var a = Tree(10); var b = Tree(20); var far = Tree(600);
        var batches = root.AddComponent<DistrictFloraBatches>(); batches.Build(new[] { a, b, far });
        var untouched = Batches().Single(r => r.bounds.center.x > 500);
        var winter = Sprite.Create(texture, new Rect(0,0,4,4), Vector2.zero, 2);
        batches.BeginChanges();
        foreach (var tree in new[] { a, b }) { batches.Remove(tree); tree.sprite = winter; batches.Add(tree); }
        batches.EndChanges();
        Assert.AreSame(untouched, Batches().Single(r => r.bounds.center.x > 500));
        Assert.AreEqual(2, Batches().Length); Assert.True(a.forceRenderingOff);
        Assert.NotNull(a.GetComponent<DistrictSelectable>());
        Object.DestroyImmediate(winter);
    }
    [Test] public void SeasonWorkIsBoundedAndNewSeasonSupersedesPendingWork()
    {
        texture.name = "forest-cluster-01-summer";
        var world = root.AddComponent<DistrictWorldController>();
        var flags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic;
        void Set(string name, object value) => typeof(DistrictWorldController).GetField(name, flags).SetValue(world, value);
        var d = new RegionCityTile { Labor = new DistrictLaborState { SeasonIndex = 1 } };
        Set("_content", root.transform); Set("_terrainDistrict", d);
        var trees = Enumerable.Range(0, 37).Select(i => Tree(i)).ToArray();
        var registry = (System.Collections.Generic.Dictionary<string, SpriteRenderer>)typeof(DistrictWorldController)
            .GetField("_forestClusters", flags).GetValue(world);
        for (int i=0;i<trees.Length;i++) registry[i.ToString()] = trees[i];
        string before = JsonUtility.ToJson(d);
        world.SyncForestSeason();
        Assert.True(world.ForestSeasonPending);
        Assert.AreEqual(DistrictWorldController.ForestSeasonFrameBudget, trees.Count(r=>r.sprite.texture.name.EndsWith("-autumn")));
        Assert.AreEqual(before, JsonUtility.ToJson(d));
        d.Labor.SeasonIndex = 2; // A reload/undo/skip while the prior transition is pending.
        trees[20].gameObject.SetActive(false); registry.Remove("20");
        int guard=0;
        do { world.SyncForestSeason(); Assert.Less(++guard, 10); } while (world.ForestSeasonPending);
        Assert.True(registry.Values.All(r=>r.sprite.texture.name.EndsWith("-winter")));
        var sprites = registry.Values.Select(r=>r.sprite).ToArray();
        world.SyncForestSeason(); CollectionAssert.AreEqual(sprites, registry.Values.Select(r=>r.sprite));
    }
    [Test] public void NearbyCopiesShareOneMeshAndRemainPickable()
    {
        var a = Tree(10); var b = Tree(20); var batches = root.AddComponent<DistrictFloraBatches>(); batches.Build(new[] { a, b });
        Assert.AreEqual(1, Batches().Length); Assert.AreEqual(sprite.vertices.Length * 2, Batches()[0].GetComponent<MeshFilter>().sharedMesh.vertexCount);
        Assert.True(a.forceRenderingOff); Assert.True(a.enabled);
        Assert.True(a.GetComponent<DistrictSelectable>().Hit(new Ray(new Vector3(11, 1, -10), Vector3.forward), out _));
        var properties = new MaterialPropertyBlock(); Batches()[0].GetPropertyBlock(properties);
        Assert.AreSame(texture, properties.GetTexture("_MainTex")); Assert.AreEqual(1f, properties.GetFloat("_FloraSaturation"));
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

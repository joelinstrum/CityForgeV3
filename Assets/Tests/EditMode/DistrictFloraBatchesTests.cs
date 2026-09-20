using System.Linq;
using CityForgeV3.World;
using NUnit.Framework;
using UnityEngine;

public class DistrictFloraBatchesTests
{
    [Test]
    public void TimeOfDayChangesStageDenseFloraWithoutReplacingTrees()
    {
        var host = new GameObject("Staged flora lighting test");
        try
        {
            var district = new RegionCityTile
            {
                TileId = "staged-flora-light", Width = 2, Height = 2,
                Founded = true, TimeOfDay = TimeOfDayPreset.Morning
            };
            for (var i = 0; i < 64; i++)
                district.Flora.Add(new PlacedDistrictFlora
                {
                    InstanceId = "staged-" + i,
                    FloraId = i % 4 == 0 ? "forest-cluster-01" : "cilician-fir",
                    NormalizedX = .08f + (i % 8) * .12f,
                    NormalizedZ = .08f + (i / 8) * .12f
                });
            var world = host.AddComponent<DistrictWorldController>();
            world.RebuildEntireDistrict(district,
                DistrictBulkRebuildReason.TestFixture);
            var before = host.GetComponentsInChildren<SpriteRenderer>(true)
                .Where(renderer => renderer.name.StartsWith("District Flora —"))
                .OrderBy(renderer => renderer.GetComponent<DistrictSelectable>()?
                    .Identity.Id).ToArray();

            world.SetTimeOfDay(TimeOfDayPreset.Afternoon);

            Assert.That(world.TimeOfDayPresentationPending, Is.True);
            var slices = 0;
            while (world.TimeOfDayPresentationPending && slices++ < 100)
                world.SyncTimeOfDayPresentation();
            Assert.That(slices, Is.GreaterThan(1));
            Assert.That(slices, Is.LessThan(100));
            var after = host.GetComponentsInChildren<SpriteRenderer>(true)
                .Where(renderer => renderer.name.StartsWith("District Flora —"))
                .OrderBy(renderer => renderer.GetComponent<DistrictSelectable>()?
                    .Identity.Id).ToArray();
            CollectionAssert.AreEqual(before, after,
                "Lighting changes must retain selectable flora presentations.");
        }
        finally { Object.DestroyImmediate(host); }
    }

    [Test]
    public void IncrementalFloraInsertionKeepsExistingPresentations()
    {
        var root = new GameObject("Incremental flora test");
        try
        {
            var district = new RegionCityTile
            {
                TileId = "incremental-flora", Width = 1, Height = 1
            };
            for (var i = 0; i < 400; i++)
                district.Flora.Add(new PlacedDistrictFlora
                {
                    InstanceId = "existing-" + i,
                    FloraId = "cilician-fir",
                    NormalizedX = .1f + (i % 20) * .04f,
                    NormalizedZ = .1f + (i / 20) * .04f
                });
            var world = root.AddComponent<DistrictWorldController>();
            world.RebuildEntireDistrict(district, DistrictBulkRebuildReason.TestFixture);
            var existing = root.GetComponentsInChildren<SpriteRenderer>(true)
                .First(renderer => renderer.name ==
                    "District Flora — cilician-fir");
            var additions = Enumerable.Range(0, 12).Select(i =>
                new PlacedDistrictFlora
                {
                    InstanceId = "added-" + i,
                    FloraId = i % 5 == 0 ? "cilician-fir" : "american-elm",
                    NormalizedX = .45f + i * .002f,
                    NormalizedZ = .55f
                }).ToList();
            foreach (var tree in additions)
            {
                district.Flora.Add(tree);
                DistrictHarvestIndex.Changed(district, tree);
            }

            world.AddDistrictFloraPresentations(additions,
                additions[additions.Count - 1].InstanceId);

            Assert.That(existing, Is.SameAs(root
                .GetComponentsInChildren<SpriteRenderer>(true)
                .First(renderer => renderer.name ==
                    "District Flora — cilician-fir")),
                "Adding a group must not rebuild existing flora objects.");
            Assert.That(root.GetComponentsInChildren<SpriteRenderer>(true)
                .Count(renderer => renderer.name.StartsWith(
                    "District Flora —")), Is.EqualTo(412));
        }
        finally { Object.DestroyImmediate(root); }
    }

    [Test]
    public void MovingFloraKeepsUnchangedPresentationsAndRejoinsItsBatch()
    {
        var host = new GameObject("Incremental flora move test");
        try
        {
            var district = new RegionCityTile
            {
                TileId = "incremental-flora-move", Width = 1, Height = 1
            };
            var moving = new PlacedDistrictFlora
            {
                InstanceId = "moving", FloraId = "cilician-fir",
                NormalizedX = .25f, NormalizedZ = .25f
            };
            var unchanged = new PlacedDistrictFlora
            {
                InstanceId = "unchanged", FloraId = "american-elm",
                NormalizedX = .75f, NormalizedZ = .75f
            };
            district.Flora.Add(moving); district.Flora.Add(unchanged);
            var world = host.AddComponent<DistrictWorldController>();
            world.RebuildEntireDistrict(district, DistrictBulkRebuildReason.TestFixture);
            var renderers = host.GetComponentsInChildren<SpriteRenderer>(true)
                .Where(renderer => renderer.name.StartsWith("District Flora —"))
                .ToArray();
            var movingRenderer = renderers.First(renderer =>
                renderer.GetComponent<DistrictSelectable>()?.Identity.Id == "moving");
            var unchangedRenderer = renderers.First(renderer =>
                renderer.GetComponent<DistrictSelectable>()?.Identity.Id == "unchanged");

            moving.NormalizedX = .35f;
            moving.NormalizedZ = .4f;
            world.MoveDistrictFloraPresentations(new[] { moving }, "moving");

            Assert.That(host.GetComponentsInChildren<SpriteRenderer>(true)
                .First(renderer => renderer.GetComponent<DistrictSelectable>()?
                    .Identity.Id == "moving"), Is.SameAs(movingRenderer));
            Assert.That(host.GetComponentsInChildren<SpriteRenderer>(true)
                .First(renderer => renderer.GetComponent<DistrictSelectable>()?
                    .Identity.Id == "unchanged"), Is.SameAs(unchangedRenderer));
            Assert.That(movingRenderer.forceRenderingOff, Is.True,
                "Moved flora must rejoin the spatial render batch.");
        }
        finally { Object.DestroyImmediate(host); }
    }
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
    [Test] public void FamilyClustersResolveCompactAndLargeSeasonalArtwork()
    {
        foreach (var family in FloraFamilies.Names)
        foreach (var large in new[] { false, true })
        {
            var id = ForestClusterCatalog.Id(family, large);
            foreach (var season in new[] { SeasonPreset.Summer, SeasonPreset.Autumn, SeasonPreset.Winter })
            {
                var path = ForestClusterCatalog.ResourcePath(id, season);
                var art = Resources.Load<Texture2D>(path);
                Assert.NotNull(art, path); Assert.AreEqual(1254, art.width); Assert.AreEqual(1254, art.height);
                Assert.True(ForestClusterCatalog.IsTexture(art.name));
            }
            Assert.AreEqual(large ? ForestClusterCatalog.LargePixelsPerUnit :
                ForestClusterCatalog.CompactPixelsPerUnit,
                LotWorldController.FloraPixelsPerUnit(id, id));
        }
        Assert.AreEqual(ForestClusterCatalog.ResourcePath("forest-tropical-large", SeasonPreset.Summer),
            ForestClusterCatalog.ResourcePath("forest-tropical-large", SeasonPreset.Winter));
    }
    [Test] public void DeciduousSummerUsesDepthShadedV02PreviewOnly()
    {
        foreach (var id in new[] { "forest-deciduous-compact", "forest-deciduous-large" })
        {
            StringAssert.Contains("/ForestClustersFamilyMixV02/",
                ForestClusterCatalog.ResourcePath(id, SeasonPreset.Summer));
            Assert.AreEqual(ForestClusterCatalog.ResourcePath(id, SeasonPreset.Summer),
                ForestClusterCatalog.ResourcePath(id, SeasonPreset.Spring));
            StringAssert.Contains("/ForestClustersFamilyMixV01/",
                ForestClusterCatalog.ResourcePath(id, SeasonPreset.Autumn));
            StringAssert.Contains("/ForestClustersFamilyMixV01/",
                ForestClusterCatalog.ResourcePath(id, SeasonPreset.Winter));
        }
    }
    [Test] public void LargeFamilyClusterShadowsUseNineGroundContacts()
    {
        texture.name = "forest-deciduous-large-winter";
        var tree = Tree(0);
        var item = new GameObject("District Flora Shadow"); item.transform.SetParent(tree.transform, false);
        item.AddComponent<MeshFilter>().sharedMesh = new Mesh();
        item.AddComponent<DistrictFloraShadowMesh>(); var shadow = item.AddComponent<MeshRenderer>();
        int contacts = 0;
        Assert.True(ForestClusterShadows.Update(tree, shadow, Vector3.down, _ => 0,
            p => { contacts++; p.y = 0; return p; }));
        Assert.AreEqual(9, contacts);
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
    [Test] public void HostedLotOrbitKeepsCachedDistrictTreesFacingCamera()
    {
        var view = new GameObject("Shared district camera");
        view.transform.SetParent(root.transform);
        var camera = view.AddComponent<Camera>();
        camera.transform.rotation = Quaternion.Euler(35f, 45f, 0f);
        var tree = Tree(10);
        tree.transform.rotation = camera.transform.rotation;
        var batches = root.AddComponent<DistrictFloraBatches>();
        batches.Build(new[] { tree }, camera);
        var renderer = Batches().Single();
        var mesh = renderer.GetComponent<MeshFilter>().sharedMesh;
        var offsets = new System.Collections.Generic.List<Vector3>();
        mesh.GetUVs(2, offsets);
        Assert.AreEqual(mesh.vertexCount, offsets.Count);
        Assert.True(offsets.Any(offset => offset.sqrMagnitude > 1f));
        var block = new MaterialPropertyBlock();
        renderer.GetPropertyBlock(block);
        Assert.AreEqual(1f, block.GetFloat("_DistrictFloraBatch"));

        camera.transform.rotation = Quaternion.Euler(35f, 135f, 0f);
        Assert.AreSame(mesh, Batches().Single().GetComponent<MeshFilter>().sharedMesh,
            "A camera orbit must not rebuild district forest cells");
        var corners = offsets.Select(offset =>
            camera.transform.InverseTransformDirection(camera.transform.rotation * offset)).ToArray();
        Assert.Less(corners.Max(corner => Mathf.Abs(corner.z)), .0001f,
            "The cached tree quad must remain flat to the new camera view");
        Assert.Greater(corners.Max(corner => corner.y) - corners.Min(corner => corner.y), 3f,
            "Orbiting must keep the billboard upright and full height");
    }
    [Test] public void CachedTreeStillRendersAfterNinetyDegreeCameraOrbit()
    {
        texture.SetPixels(Enumerable.Repeat(Color.white, 16).ToArray());
        texture.Apply();
        var tree = Tree(0);
        var view = new GameObject("Shared camera");
        view.transform.SetParent(root.transform);
        var camera = view.AddComponent<Camera>();
        camera.orthographic = true;
        camera.orthographicSize = 4f;
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = Color.black;
        camera.cullingMask = 1 << 28;
        camera.enabled = false;
        tree.transform.rotation = Quaternion.Euler(35f, 45f, 0f);
        camera.transform.rotation = tree.transform.rotation;
        var batches = root.AddComponent<DistrictFloraBatches>();
        batches.Build(new[] { tree }, camera);
        var renderer = Batches().Single();
        renderer.gameObject.layer = 28;
        var cachedMesh = renderer.GetComponent<MeshFilter>().sharedMesh;
        var target = new RenderTexture(128, 128, 16);
        var image = new Texture2D(128, 128, TextureFormat.RGBA32, false);
        var previousTarget = RenderTexture.active;
        try
        {
            camera.targetTexture = target;
            int VisiblePixels(float yaw)
            {
                camera.transform.rotation = Quaternion.Euler(35f, yaw, 0f);
                var center = tree.transform.position + camera.transform.rotation *
                    new Vector3(2f, 2f, 0f);
                camera.transform.position = center - camera.transform.forward * 20f;
                camera.Render();
                RenderTexture.active = target;
                image.ReadPixels(new Rect(0, 0, 128, 128), 0, 0);
                image.Apply();
                return image.GetPixels32().Count(pixel => pixel.r > 30);
            }
            var before = VisiblePixels(45f);
            var after = VisiblePixels(135f);
            Assert.Greater(before, 2000, "Control view must show the test tree");
            Assert.Greater(after, before * .8f,
                "The cached billboard must not turn edge-on after a Lot orbit");
            Assert.AreSame(cachedMesh, renderer.GetComponent<MeshFilter>().sharedMesh);
        }
        finally
        {
            camera.targetTexture = null;
            RenderTexture.active = previousTarget;
            Object.DestroyImmediate(target);
            Object.DestroyImmediate(image);
        }
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

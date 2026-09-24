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
            Assert.That(world.TimeOfDay, Is.EqualTo(TimeOfDayPreset.Morning));
            Assert.That(world.TimeOfDayPresentationPending, Is.False,
                "Initial paint must already use the saved environment.");
            var initialShadow = host.GetComponentsInChildren<MeshRenderer>(true)
                .First(renderer => renderer.name == "Flora shadow batch");
            var initialProperties = new MaterialPropertyBlock();
            initialShadow.GetPropertyBlock(initialProperties);
            var expectedRay = TimeOfDayLighting.SunRotation(
                TimeOfDayPreset.Morning) * Vector3.forward;
            Assert.That(Vector3.Distance(initialProperties.GetVector("_SunRay"),
                    expectedRay.normalized), Is.LessThan(.001f),
                "First paint must project flora with the saved preset sun.");
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
            Assert.That(root.GetComponentsInChildren<MeshRenderer>(true)
                .Where(renderer => renderer.name == "Flora shadow batch")
                .All(renderer => renderer.GetComponent<MeshFilter>()
                    .sharedMesh.bounds.size.y < .1f), Is.True,
                "Incremental shadows must be projected onto the ground before batching.");
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
    [Test] public void SharedIndividualFloraKeepsEstablishedCutoutAndProjectedTexture()
    {
        var district = new RegionCityTile
        {
            TileId = "flora-alpha-coverage", Width = 1, Height = 1,
            Founded = true, TimeOfDay = TimeOfDayPreset.Noon
        };
        district.Flora.Add(new PlacedDistrictFlora
        {
            InstanceId = "coverage-tree", FloraId = "mature-oak",
            NormalizedX = .5f, NormalizedZ = .5f
        });
        var world = root.AddComponent<DistrictWorldController>();
        world.RebuildEntireDistrict(district,
            DistrictBulkRebuildReason.TestFixture);
        var flora = root.GetComponentsInChildren<MeshRenderer>(true)
            .Single(renderer => renderer.name == "Flora batch");
        var shadow = root.GetComponentsInChildren<MeshRenderer>(true)
            .Single(renderer => renderer.name == "Flora shadow batch");
        Assert.That(flora.sharedMaterial.GetFloat("_Cutoff"),
            Is.EqualTo(.02f).Within(.001f));
        Assert.That(shadow.sharedMaterial.GetFloat("_Cutoff"),
            Is.EqualTo(.02f).Within(.001f));
        var properties = new MaterialPropertyBlock();
        shadow.GetPropertyBlock(properties);
        var source = root.GetComponentsInChildren<SpriteRenderer>(true)
            .Single(renderer => renderer.name.StartsWith("District Flora —"));
        Assert.AreSame(source.sprite.texture,
            properties.GetTexture("_MainTex"),
            "The batched projection must sample the tree cutout, not a white card.");
    }
    [TestCase(0)] [TestCase(1)] [TestCase(2)] [TestCase(3)] [TestCase(4)]
    public void ClusterShadowsUseOneArtworkRootAndDefinedCanopyFootprint(int variant)
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
        Assert.AreEqual(1, contacts.Count,
            "A cluster is one composition and must not use hidden per-tree coordinates.");
        Assert.AreEqual(200, mesh.vertexCount,
            "Four crown silhouettes and their small trunk contacts share one mesh.");
        Assert.True(mesh.colors.Any(c => c.r == 0), "Feathered canopy boundary");
        Assert.True(mesh.colors.Any(c => c.r > .5f), "Visible shadow interior");
        Assert.That(mesh.colors.Skip(1).Take(20).All(c =>
            Mathf.Abs(c.r - mesh.colors[0].r) < .001f), Is.True,
            "The canopy remains defined to its inner ring before the short fade.");
        var crownCenters = new[] { 0, 50, 100, 150 }
            .Select(i => shadow.transform.TransformPoint(mesh.vertices[i])).ToArray();
        Assert.AreEqual(4, crownCenters.Select(p => Mathf.RoundToInt(p.x * 100))
            .Distinct().Count(), "Distinct crowns should not collapse into one oval.");
        Assert.True(mesh.vertices.All(v => Mathf.Abs(shadow.transform.TransformPoint(v).y - .031f) < .001f));
        // Both axis-aligned and noon sun must retain two-dimensional shadows.
        foreach (var light in new[] { Vector3.down, new Vector3(1,-1,0).normalized, new Vector3(0,-1,1).normalized })
        {
            ForestClusterShadows.Update(tree, shadow, light, _ => 0, foot => { foot.y = 0; return foot; });
            var points = mesh.vertices.Select(shadow.transform.TransformPoint).ToArray();
            var firstTriangle = mesh.triangles.Take(3).ToArray();
            var area = Vector3.Cross(points[firstTriangle[1]] - points[firstTriangle[0]],
                points[firstTriangle[2]] - points[firstTriangle[0]]).magnitude;
            Assert.Greater(area, .01f,
                "The artwork axis must never collapse a crown fan along the sun ray.");
            Assert.Greater(points.Max(p => p.x) - points.Min(p => p.x), .1f);
            Assert.Greater(points.Max(p => p.z) - points.Min(p => p.z), .1f,
                "Canopy must not collapse along the sun axis");
        }
        var first = mesh.vertices;
        ForestClusterShadows.Update(tree, shadow, new Vector3(-.3f,-1,-.2f).normalized,
            _ => 0, foot => { foot.y = 0; return foot; });
        Assert.True(first.Where((v, i) => (v - mesh.vertices[i]).sqrMagnitude > .001f).Any(), "Canopies follow the sun");
    }
    [Test] public void ElmShadowHasGroundContactAndLongerRoundedCenter()
    {
        texture.name = "american-elm-summer";
        var tree = Tree(0);
        var item = new GameObject("District Flora Shadow");
        item.transform.SetParent(tree.transform, false);
        var mesh = new Mesh(); item.AddComponent<MeshFilter>().sharedMesh = mesh;
        item.AddComponent<DistrictFloraShadowMesh>();
        var shadow = item.AddComponent<MeshRenderer>();
        var ray = new Vector3(.82f, -.67f, 0f).normalized;
        Assert.True(ForestClusterShadows.Update(tree, shadow, ray,
            _ => 0f, _ => throw new System.Exception(
                "The elm shadow must use its trunk, not a distant raycast anchor.")));
        var direction = Vector3.ProjectOnPlane(ray, Vector3.up).normalized;
        float LobeReach(int start)
        {
            var center = shadow.transform.TransformPoint(mesh.vertices[start]);
            return mesh.vertices.Skip(start + 1).Take(40)
                .Max(vertex => Vector3.Dot(
                    shadow.transform.TransformPoint(vertex) - center,
                    direction));
        }
        Assert.That(LobeReach(50), Is.GreaterThan(LobeReach(0) * 1.35f));
        Assert.That(LobeReach(50), Is.LessThan(LobeReach(0) * 1.65f));
        Assert.That(mesh.vertices.All(vertex => Mathf.Abs(
            shadow.transform.TransformPoint(vertex).y - .031f) < .001f), Is.True);
    }
    [Test]
    public void DistrictTreeShadowRayStaysBehindCameraAtDaylightPresets()
    {
        var cameraForward = new Vector3(1f, -.36f, 1f).normalized;
        var behind = Vector3.ProjectOnPlane(cameraForward, Vector3.up).normalized;
        foreach (var preset in new[] { TimeOfDayPreset.Morning,
            TimeOfDayPreset.Noon, TimeOfDayPreset.Afternoon })
        {
            var sunRay = TimeOfDayLighting.SunRotation(preset) * Vector3.forward;
            var projected = ForestClusterShadows.BehindCameraRay(
                sunRay, cameraForward);
            Assert.That(Vector3.Dot(Vector3.ProjectOnPlane(projected,
                    Vector3.up).normalized, behind),
                Is.GreaterThan(.9f), preset.ToString());
            Assert.That(projected.y, Is.EqualTo(sunRay.y).Within(.0001f));
            Assert.That(Vector3.ProjectOnPlane(projected, Vector3.up).magnitude,
                Is.EqualTo(Vector3.ProjectOnPlane(sunRay, Vector3.up).magnitude)
                    .Within(.0001f));
        }
    }
    [Test]
    public void DistrictTreeFamiliesKeepShadowMassBehindTheirTrunks()
    {
        var host = new GameObject("Behind-tree district shadow test");
        try
        {
            var district = new RegionCityTile
            {
                TileId = "behind-tree-shadows", Width = 1, Height = 1,
                Founded = true, TimeOfDay = TimeOfDayPreset.Noon
            };
            var ids = new[] { "american-elm", "london-plane-a",
                "angel-oak-spanish-moss", "forest-deciduous-compact",
                "cilician-fir", "mature-oak" };
            for (var index = 0; index < ids.Length; index++)
                district.Flora.Add(new PlacedDistrictFlora
                {
                    InstanceId = "behind-" + index, FloraId = ids[index],
                    NormalizedX = .12f + index * .15f,
                    NormalizedZ = .5f
                });
            var world = host.AddComponent<DistrictWorldController>();
            world.RebuildEntireDistrict(district,
                DistrictBulkRebuildReason.TestFixture);
            var behind = Vector3.ProjectOnPlane(world.WorldCamera.transform.forward,
                Vector3.up).normalized;
            foreach (var preset in new[] { TimeOfDayPreset.Noon,
                TimeOfDayPreset.Morning, TimeOfDayPreset.Afternoon })
            {
                world.SetTimeOfDay(preset);
                var slices = 0;
                while (world.TimeOfDayPresentationPending && slices++ < 100)
                    world.SyncTimeOfDayPresentation();
                Assert.Less(slices, 100);
                var trees = host.GetComponentsInChildren<SpriteRenderer>(true)
                    .Where(renderer => renderer.name.StartsWith("District Flora —"));
                foreach (var tree in trees)
                {
                    var filter = tree.transform.Find("District Flora Shadow")?
                        .GetComponent<MeshFilter>();
                    Assert.NotNull(filter, tree.name);
                    var mesh = filter.sharedMesh;
                    Assert.Greater(mesh.vertexCount, 0, tree.name);
                    var opaque = mesh.vertices.Where((_, i) =>
                        mesh.colors[i].r > .2f).ToArray();
                    var average = opaque.Aggregate(Vector3.zero,
                        (sum, point) => sum + filter.transform.TransformPoint(point)) /
                        opaque.Length;
                    Assert.That(Vector3.Dot(average - tree.transform.position, behind),
                        Is.GreaterThan(0f), tree.name + " at " + preset);
                }
            }
        }
        finally { Object.DestroyImmediate(host); }
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
    [Test] public void FamilySeasonsUseDepthStaggeredV03AndV04Artwork()
    {
        foreach (var family in FloraFamilies.Names)
        foreach (var large in new[] { false, true })
        {
            var id = ForestClusterCatalog.Id(family, large);
            StringAssert.Contains("/ForestClustersFamilyMixV03/",
                ForestClusterCatalog.ResourcePath(id, SeasonPreset.Summer));
            Assert.AreEqual(ForestClusterCatalog.ResourcePath(id, SeasonPreset.Summer),
                ForestClusterCatalog.ResourcePath(id, SeasonPreset.Spring));
            if (family != FloraFamilies.Tropical)
            {
                StringAssert.Contains("/ForestClustersFamilyMixV04/",
                    ForestClusterCatalog.ResourcePath(id, SeasonPreset.Autumn));
                StringAssert.Contains("/ForestClustersFamilyMixV04/",
                    ForestClusterCatalog.ResourcePath(id, SeasonPreset.Winter));
            }
        }
    }
    [Test] public void DepthShadedFamilyClustersUseOneSharedFringeCutoff()
    {
        var apply = typeof(DistrictWorldController).GetMethod(
            "ApplyForestSeasonCutoff",
            System.Reflection.BindingFlags.Static |
            System.Reflection.BindingFlags.NonPublic);
        Assert.NotNull(apply);
        var tree = Tree(0);
        var block = new MaterialPropertyBlock();

        texture.name = "forest-deciduous-large-summer";
        apply.Invoke(null, new object[] { tree });
        tree.GetPropertyBlock(block);
        Assert.That(block.GetFloat("_Cutoff"), Is.EqualTo(.5f).Within(.001f));
        Assert.True(ForestClusterCatalog.UsesDepthShadedCutout(texture.name));

        texture.name = "forest-deciduous-large-autumn";
        apply.Invoke(null, new object[] { tree });
        tree.GetPropertyBlock(block);
        Assert.That(block.GetFloat("_Cutoff"), Is.EqualTo(.5f).Within(.001f));
        Assert.True(ForestClusterCatalog.UsesDepthShadedCutout(texture.name));

        texture.name = "forest-deciduous-large-winter";
        apply.Invoke(null, new object[] { tree });
        tree.GetPropertyBlock(block);
        Assert.That(block.GetFloat("_Cutoff"), Is.EqualTo(.12f).Within(.001f));

        texture.name = "forest-cluster-01-summer";
        apply.Invoke(null, new object[] { tree });
        tree.GetPropertyBlock(block);
        Assert.That(block.GetFloat("_Cutoff"), Is.EqualTo(.02f).Within(.001f));
    }
    [Test] public void LargeFamilyClusterShadowUsesOneSharedGroundContact()
    {
        texture.name = "forest-deciduous-large-winter";
        var tree = Tree(0);
        var item = new GameObject("District Flora Shadow"); item.transform.SetParent(tree.transform, false);
        item.AddComponent<MeshFilter>().sharedMesh = new Mesh();
        item.AddComponent<DistrictFloraShadowMesh>(); var shadow = item.AddComponent<MeshRenderer>();
        int contacts = 0;
        Assert.True(ForestClusterShadows.Update(tree, shadow, Vector3.down, _ => 0,
            p => { contacts++; p.y = 0; return p; }));
        Assert.AreEqual(1, contacts);
        Assert.AreEqual(42, shadow.GetComponent<MeshFilter>().sharedMesh.vertexCount);
    }
    [TestCase("forest-deciduous-compact-summer", 150)]
    [TestCase("forest-deciduous-large-summer", 250)]
    public void LeafedFamilyShadowUsesCrownLobesWithoutExtraGroundQueries(
        string textureName, int expectedVertices)
    {
        texture.name = textureName;
        var tree = Tree(0);
        var item = new GameObject("District Flora Shadow");
        item.transform.SetParent(tree.transform, false);
        var mesh = new Mesh(); item.AddComponent<MeshFilter>().sharedMesh = mesh;
        item.AddComponent<DistrictFloraShadowMesh>();
        var shadow = item.AddComponent<MeshRenderer>();
        int contacts = 0;
        Assert.True(ForestClusterShadows.Update(tree, shadow,
            new Vector3(.3f, -1f, .2f).normalized, _ => 0,
            foot => { contacts++; foot.y = 0; return foot; }));
        Assert.AreEqual(1, contacts);
        Assert.AreEqual(expectedVertices, mesh.vertexCount);
        Assert.That(mesh.colors.Count(color => color.r > .55f),
            Is.GreaterThan(expectedVertices / 4),
            "Most of each crown should remain defined, not a broad blur.");
    }
    [Test] public void WinterShadowsRetainFirButOpenDeciduousCanopies()
    {
        texture.name = "forest-cluster-01-summer";
        var tree = Tree(0);
        var item = new GameObject("District Flora Shadow"); item.transform.SetParent(tree.transform, false);
        var mesh = new Mesh(); item.AddComponent<MeshFilter>().sharedMesh = mesh;
        item.AddComponent<DistrictFloraShadowMesh>(); var shadow = item.AddComponent<MeshRenderer>();
        ForestClusterShadows.Update(tree, shadow, Vector3.down, p => 0, p => new Vector3(p.x, 0, p.z));
        float summerOpacity = mesh.colors.Max(color => color.r);
        Assert.AreEqual(200, mesh.vertexCount);
        texture.name = "forest-cluster-01-winter";
        int contacts = 0;
        Assert.True(ForestClusterShadows.Update(tree, shadow, Vector3.down, p => 0,
            p => { contacts++; return new Vector3(p.x, 0, p.z); }));
        Assert.AreEqual(1, contacts);
        Assert.AreEqual(42, mesh.vertexCount);
        Assert.Less(mesh.colors.Max(color => color.r), summerOpacity,
            "Leafless compositions retain a lighter shared footprint.");
    }
    [Test] public void ElmUsesNewSeasonalArtworkAndSummerForSpring()
    {
        foreach (var season in new[] { SeasonPreset.Spring, SeasonPreset.Summer,
            SeasonPreset.Autumn, SeasonPreset.Winter })
        {
            var expectedSeason = season == SeasonPreset.Spring ? "summer" :
                season.ToString().ToLowerInvariant();
            var path = LotWorldController.ResolveFloraResourcePath("american-elm", season);
            Assert.AreEqual(FloraTreeRepairs.ElmTrueAngleRoot + "american-elm-" +
                expectedSeason, path);
            var art = Resources.Load<Texture2D>(path);
            Assert.NotNull(art, path);
            Assert.AreEqual(1312, art.width);
            Assert.AreEqual(1199, art.height);
            Assert.AreEqual(72f, LotWorldController.FloraPixelsPerUnit("american-elm", art.name));
            Assert.Greater(LotWorldController.FloraPivot(art.name).y, 0f);
        }
        Assert.That(LotWorldController.FloraPivot("american-elm-summer").y,
            Is.EqualTo(187f / 1199f).Within(.0001f),
            "The elm selection root belongs at the trunk foot, not the lowest leaves.");
    }
    [Test] public void SpanishMossUsesOneNewBillboardAtEverySeason()
    {
        foreach (var season in new[] { SeasonPreset.Spring, SeasonPreset.Summer,
            SeasonPreset.Autumn, SeasonPreset.Winter })
        {
            var path = LotWorldController.ResolveFloraResourcePath(
                "angel-oak-spanish-moss", season);
            Assert.AreEqual(FloraTreeRepairs.SpanishMossTrueAngleRoot +
                "angel-oak-spanish-moss", path);
            var art = Resources.Load<Texture2D>(path);
            Assert.NotNull(art, path);
            Assert.AreEqual(1312, art.width);
            Assert.AreEqual(1199, art.height);
            Assert.AreEqual(80f, LotWorldController.FloraPixelsPerUnit(
                "angel-oak-spanish-moss", art.name));
            Assert.That(LotWorldController.FloraPivot(art.name).y,
                Is.EqualTo(178f / 1199f).Within(.0001f));
        }
        Assert.False(PlaneUkFloraPresentation.IsTree("angel-oak-spanish-moss"),
            "The old close-up mesh must not hide the replacement cutout.");
    }
    [Test] public void DistrictElmChangesWithCalendarWithoutReplacingItsRenderer()
    {
        var host = new GameObject("Seasonal elm district test");
        try
        {
            var district = new RegionCityTile
            {
                TileId = "elm-season-test", Width = 1, Height = 1,
                Founded = true,
                Labor = new DistrictLaborState { SeasonIndex = 0 }
            };
            district.Flora.Add(new PlacedDistrictFlora
            {
                InstanceId = "elm", FloraId = "american-elm",
                NormalizedX = .5f, NormalizedZ = .5f
            });
            var world = host.AddComponent<DistrictWorldController>();
            world.RebuildEntireDistrict(district,
                DistrictBulkRebuildReason.TestFixture);
            var renderer = host.GetComponentsInChildren<SpriteRenderer>(true)
                .Single(r => r.name == "District Flora — american-elm");
            Assert.AreEqual("american-elm-summer", renderer.sprite.texture.name);
            district.Labor.SeasonIndex = 1;
            world.SyncForestSeason();
            Assert.AreEqual("american-elm-autumn", renderer.sprite.texture.name);
            district.Labor.SeasonIndex = 2;
            world.SyncForestSeason();
            Assert.AreEqual("american-elm-winter", renderer.sprite.texture.name);
            district.Labor.SeasonIndex = 3;
            world.SyncForestSeason();
            Assert.AreEqual("american-elm-summer", renderer.sprite.texture.name);
        }
        finally { Object.DestroyImmediate(host); }
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
    [Test] public void PlaneVariantsUseOneAmericanSycamoreInEverySeason()
    {
        foreach (var id in new[] { "london-plane-a", "london-plane-b",
            "plane-uk-3d-a", "plane-uk-3d-b", "london-plane-c" })
        {
            foreach (var season in new[] { SeasonPreset.Spring, SeasonPreset.Summer,
                SeasonPreset.Autumn, SeasonPreset.Winter })
            {
                var expected = season == SeasonPreset.Spring ? "summer" :
                    season.ToString().ToLowerInvariant();
                var path = LotWorldController.ResolveFloraResourcePath(id, season);
                Assert.AreEqual(FloraTreeRepairs.AmericanSycamoreRoot +
                    "american-sycamore-" + expected, path, id);
                var art = Resources.Load<Texture2D>(path);
                Assert.NotNull(art, path);
                Assert.AreEqual(1312, art.width);
                Assert.AreEqual(1199, art.height);
                Assert.AreEqual(TextureWrapMode.Clamp, art.wrapMode);
                Assert.AreEqual(72f, LotWorldController.FloraPixelsPerUnit(id, art.name));
                Assert.Greater(LotWorldController.FloraPivot(art.name).y, 0f);
            }
        }
    }
    [Test] public void DistrictPlaneVariantsShareSpritesAndFollowSeasons()
    {
        var host = new GameObject("Seasonal sycamore district test");
        try
        {
            var district = new RegionCityTile
            {
                TileId = "sycamore-season-test", Width = 1, Height = 1,
                Founded = true,
                Labor = new DistrictLaborState { SeasonIndex = 0 }
            };
            foreach (var id in new[] { "london-plane-a", "london-plane-b" })
                district.Flora.Add(new PlacedDistrictFlora
                {
                    InstanceId = id, FloraId = id,
                    NormalizedX = id == "london-plane-a" ? .3f : .7f,
                    NormalizedZ = .5f
                });
            var world = host.AddComponent<DistrictWorldController>();
            world.RebuildEntireDistrict(district,
                DistrictBulkRebuildReason.TestFixture);
            var trees = host.GetComponentsInChildren<SpriteRenderer>(true)
                .Where(r => r.name.StartsWith("District Flora — london-plane-"))
                .ToArray();
            Assert.AreEqual(2, trees.Length);
            Assert.AreSame(trees[0].sprite, trees[1].sprite);
            foreach (var (index, name) in new[] { (1, "autumn"),
                (2, "winter"), (3, "summer") })
            {
                district.Labor.SeasonIndex = index;
                var slices = 0;
                while (true)
                {
                    world.SyncForestSeason();
                    if (!world.ForestSeasonPending) break;
                    Assert.Less(++slices, 10);
                }
                Assert.True(trees.All(r => r.sprite.texture.name ==
                    "american-sycamore-" + name));
                Assert.AreSame(trees[0].sprite, trees[1].sprite);
            }
        }
        finally { Object.DestroyImmediate(host); }
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
        do { world.SyncForestSeason(); Assert.Less(++guard, 20); } while (world.ForestSeasonPending);
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

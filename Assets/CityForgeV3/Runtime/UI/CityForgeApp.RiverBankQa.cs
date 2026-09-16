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
        System.Collections.IEnumerator ProfileClouds(bool close = false)
        {
            RiverBankQa("bank-clouds");
            if(close) { _terraformZoomLevel=DistrictZoomLevel.LOD0; _districtWorld.SetZoom(_terraformZoomLevel); }
            var layer=_districtWorld.GetComponentInChildren<DistrictCloudLayer>(true);
            var renderers=layer.GetComponentsInChildren<Renderer>(true);
            var district=FindSelectedRegionTile();
            string path=close ? "/tmp/cityforge-cloud-profile-close.txt" : "/tmp/cityforge-cloud-profile.txt";
            File.WriteAllText(path,$"{DateTime.UtcNow:o} {district.Name}; flora={district.Flora.Count}; lots={district.Lots.Count}; two cloud slots; two renderers\n");
            try
            {
                for(int pass=0;pass<2;pass++)
                {
                    for(int r=0;r<renderers.Length;r++) renderers[r].enabled=pass==1 && (r==1 || !close);
                    for(int warm=0;warm<45;warm++)yield return null;
                    var frames=new float[120];long draws=0,triangles=0;
                    for(int i=0;i<frames.Length;i++)
                    {
                        yield return null;frames[i]=Time.unscaledDeltaTime*1000;
                        draws+=UnityEditor.UnityStats.drawCalls;triangles+=UnityEditor.UnityStats.triangles;
                    }
                    Array.Sort(frames);
                    File.AppendAllText(path,$"clouds={(pass==1)} median frame={frames[60]:F2}ms p95={frames[114]:F2}ms mean draws={draws/120f:F1} triangles={triangles/120f:F0}\n");
                }
                long allocated=GC.GetAllocatedBytesForCurrentThread();
                var watch=System.Diagnostics.Stopwatch.StartNew();
                for(int i=0;i<10000;i++)layer.SetZoom(DistrictZoomLevel.LOD5Billboard);
                watch.Stop();allocated=GC.GetAllocatedBytesForCurrentThread()-allocated;
                File.AppendAllText(path,$"10000 unchanged cloud zoom calls: {watch.Elapsed.TotalMilliseconds:F3}ms, {allocated} bytes (includes Stopwatch allocation).\nDONE\n");
            }
            finally{foreach(var r in renderers)if(r!=null)r.enabled=true;layer.SetZoom(_terraformZoomLevel);}
        }
        System.Collections.IEnumerator ProfileHillMeadow(Material material)
        {
            string path="/tmp/cityforge-hill-profile.txt";
            var district=FindSelectedRegionTile();
            File.WriteAllText(path,$"{DateTime.UtcNow:o} {district.Name}; flora={district.Flora.Count}; lots={district.Lots.Count}\n");
            try
            {
                for(int pass=0;pass<2;pass++)
                {
                    if(pass==0)material.DisableKeyword("HILL_MEADOW");else material.EnableKeyword("HILL_MEADOW");
                    for(int warm=0;warm<45;warm++)yield return null;
                    var frames=new float[120];long draws=0,triangles=0;
                    for(int i=0;i<120;i++)
                    {
                        yield return null;frames[i]=Time.unscaledDeltaTime*1000;
                        draws+=UnityEditor.UnityStats.drawCalls;triangles+=UnityEditor.UnityStats.triangles;
                    }
                    Array.Sort(frames);
                    File.AppendAllText(path,$"hill meadow={pass==1} median={frames[60]:F2}ms p95={frames[114]:F2}ms draws={draws/120f:F1} triangles={triangles/120f:F0}\n");
                }
                File.AppendAllText(path,"DONE\n");
            }
            finally{if(material!=null)material.EnableKeyword("HILL_MEADOW");}
        }
        void RiverBankQa(string command)
        {
            if(command.StartsWith("bank-hill-"))
            {
                var ground=_districtWorld.GetComponentsInChildren<MeshRenderer>().First(r=>r.name.StartsWith("District Ground"));
                var material=ground.sharedMaterial;
                if(command=="bank-hill-before")material.DisableKeyword("HILL_MEADOW");
                else if(command=="bank-hill-after")material.EnableKeyword("HILL_MEADOW");
                else if(command=="bank-hill-profile")StartCoroutine(ProfileHillMeadow(material));
                else if(command=="bank-hill-zoom-check")
                {
                    var original=material.mainTexture;
                    var hillTexture=material.GetTexture("_HillTex");
                    var mesh=ground.GetComponent<MeshFilter>().sharedMesh;
                    var before=JsonUtility.ToJson(FindSelectedRegionTile());
                    for(int i=0;i<6;i++)
                    {
                        var level=(DistrictZoomLevel)i;
                        _districtWorld.SetZoom(level);
                        float metres=DistrictWorldController.DistrictGrassWorldSizeForZoom(level);
                        if(i>=2 && !Mathf.Approximately(metres,40))throw new Exception("Far grass scale changed");
                        if(i<2)
                        {
                            float nextCamera=DistrictWorldController.OrthographicSize((DistrictZoomLevel)(i+1),1,1,1);
                            float camera=DistrictWorldController.OrthographicSize(level,1,1,1);
                            if(!Mathf.Approximately(metres/camera,40/nextCamera))throw new Exception("Near grass apparent scale differs");
                        }
                        // This isolated fixture is 640m square.
                        if(Vector2.Distance(material.mainTextureScale,Vector2.one*(640/metres))>.001f)
                            throw new Exception("Ground material did not inherit zoom scale");
                        if(material.mainTexture!=original || material.GetTexture("_HillTex")!=hillTexture ||
                            !material.IsKeywordEnabled("HILL_MEADOW") || ground.GetComponent<MeshFilter>().sharedMesh!=mesh)
                            throw new Exception("Zoom changed hill artwork or rebuilt ground");
                    }
                    _districtWorld.SetZoom(_terraformZoomLevel);
                    if(before!=JsonUtility.ToJson(FindSelectedRegionTile()))throw new Exception("Grass zoom changed saved state");
                    File.WriteAllText("/tmp/cityforge-hill-zoom-check.txt",DateTime.UtcNow.ToString("o")+" PASS six zoom scales, hill artwork, mesh identity and district data\n");
                }
                else if(command=="bank-hill-view")
                {
                    var overlay=_districtWorld.GetComponentInChildren<DistrictHillGroundOverlay>();
                    _terraformPanOffset=overlay!=null?overlay.ReviewPoint:Vector2.zero;
                    _terraformZoomLevel=DistrictZoomLevel.LOD2;_districtEdgePanDirection=Vector2Int.zero;
                    _districtWorld.SetPan(_terraformPanOffset);_districtWorld.SetZoom(_terraformZoomLevel);
                }
                else throw new Exception("Unknown hill command");
                return;
            }
            if (command == "bank-cloud-clock")
            {
                var layer = _districtWorld.GetComponentInChildren<DistrictCloudLayer>(true);
                var materials = layer.GetComponentsInChildren<Renderer>(true).Select(r=>r.sharedMaterial).ToArray();
                var bodyMotion=materials[0].GetVectorArray("_CloudMotion");
                var shadowMotion=materials[1].GetVectorArray("_CloudMotion");
                if(!bodyMotion.SequenceEqual(shadowMotion)) throw new Exception("Cloud/shadow motion differs");
                File.AppendAllText("/tmp/cityforge-cloud-clock.txt", $"{DateTime.UtcNow:o} real={Time.realtimeSinceStartupAsDouble} scale={Time.timeScale} slot0={bodyMotion[0].ToString("F3")} slot1={bodyMotion[1].ToString("F3")} shadows identical\n");
                return;
            }
            if (command == "bank-cloud-dense")
            {
                var region = Directory.GetFiles(RegionSaveStore.DefaultRoot,"*.json")
                    .Select(p => JsonUtility.FromJson<RegionSaveData>(File.ReadAllText(p)))
                    .OrderByDescending(r => r.Tiles.Sum(t => t.Flora.Count)).First();
                region.RegionId = "cloud-review"; _openRegion = region;
                _districtUndoQaSaveRoot = Path.Combine(Path.GetTempPath(),"CityForgeCloudQa");
                SelectRegionTile(region.Tiles.OrderByDescending(t=>t.Flora.Count).First().TileId);
                return;
            }
            if (command == "bank-cloud-profile" || command == "bank-cloud-profile-close")
            {
                StartCoroutine(ProfileClouds(command.EndsWith("-close"))); return;
            }
            if (command == "bank-cloud-check")
            {
                var layer = _districtWorld.GetComponentInChildren<DistrictCloudLayer>(true);
                if(layer == null) throw new Exception("Missing clouds");
                var shader = Shader.Find("CityForgeV3/DistantClouds");
                if(shader == null || UnityEditor.ShaderUtil.ShaderHasError(shader)) throw new Exception("Cloud shader errors");
                var before = JsonUtility.ToJson(FindSelectedRegionTile());
                var meshes = layer.GetComponentsInChildren<MeshFilter>(true).Select(m=>m.sharedMesh).ToArray();
                for(int i=0;i<=5;i++)
                {
                    _districtWorld.SetZoom((DistrictZoomLevel)i);
                    var renderers=layer.GetComponentsInChildren<MeshRenderer>(true);
                    if(!layer.gameObject.activeSelf || renderers[0].enabled != (i>=4) || !renderers[1].enabled) throw new Exception("Cloud/shadow visibility at LOD"+i);
                }
                if(before != JsonUtility.ToJson(FindSelectedRegionTile())) throw new Exception("Cloud zoom changed district data");
                if(!meshes.SequenceEqual(layer.GetComponentsInChildren<MeshFilter>(true).Select(m=>m.sharedMesh))) throw new Exception("Cloud zoom rebuilt meshes");
                _terraformZoomLevel=DistrictZoomLevel.LOD5Billboard;
                File.AppendAllText("/tmp/cityforge-cloud-checks.txt", DateTime.UtcNow.ToString("o")+" PASS all six zoom stops; unchanged meshes and district; shader compiled\n");
                return;
            }
            if (command == "bank-clouds")
            {
                _districtEdgePanDirection=Vector2Int.zero;
                _terraformPanOffset=Vector2.zero;_terraformZoomLevel=DistrictZoomLevel.LOD5Billboard;
                _districtWorld.SetPan(_terraformPanOffset);_districtWorld.SetZoom(_terraformZoomLevel);return;
            }
            if (command == "bank-fixture")
            {
                var tile = _openRegion.Tiles[0];
                tile.Width = tile.Height = 1;
                tile.Hills = new();
                tile.Biome = RegionBiome.Grassland;
                tile.Rivers.Clear();
                var river = new PlacedDistrictRiver { InstanceId = "bank-main", WidthMeters = 64, Depth = DistrictRiverDepth.Deep };
                for (int i = 0; i <= 64; i++)
                {
                    float t = i / 64f;
                    river.Points.Add(new(t, .5f + Mathf.Sin(t * Mathf.PI * 2) * .18f));
                }
                tile.Rivers.Add(river);
                tile.Rivers.Add(new() { InstanceId = "bank-tributary", WidthMeters = 18, Depth = DistrictRiverDepth.Shallow,
                    Points = new() { new(.5f, 0), new(.55f, .15f), new(.52f, .3f), new(.5f, .5f) } });
                _districtUndoQaSaveRoot = Path.Combine(Path.GetTempPath(), "CityForgeMapLayersQa");
                SelectRegionTile(tile.TileId);
                return;
            }
            var district = FindSelectedRegionTile();
            if (command == "bank-capture")
            {
                Directory.CreateDirectory("QA/RiverBanks");
                var view = UnityEditor.EditorWindow.GetWindow(typeof(UnityEditor.EditorWindow).Assembly.GetType("UnityEditor.GameView"));
                if (view.maximized) throw new Exception("Restore normal windowed Game view before capture");
                // Capture the actual running Game view at its current windowed
                // resolution. No substitute camera or offscreen render target.
                ScreenCapture.CaptureScreenshot("QA/RiverBanks/" + district.Name.Replace(" ", "-") + "-" + DateTime.UtcNow.ToString("HHmmssfff") + ".png");
                return;
            }
            if (command == "bank-repeat")
            {
                for (int attempt = 0; attempt < 2; attempt++)
                {
                    var selected = district.Rivers.OrderByDescending(r => r.Points.Count).First();
                    float width = selected.WidthMeters; var depth = selected.Depth;
                    var points = selected.Points;
                    Vector2 P(int index) => new(points[index].X, points[index].Z);
                    var start = P(points.Count / 3); var end = P(points.Count * 2 / 3);
                    var middle = P(points.Count / 2);
                    var direction = (end - start).normalized;
                    middle += new Vector2(-direction.y / DistrictScale.SizeMeters(district.Width),
                        direction.x / DistrictScale.SizeMeters(district.Height)) * (attempt == 0 ? 12f : -12f);
                    var before = JsonUtility.ToJson(district);
                    CommitDistrictRiverSculpt(district, DistrictRiverSculpt.Redraw(district, selected,
                        new[] { start, middle, end }, width), validateBuildings: true);
                    if (JsonUtility.ToJson(district) == before) throw new Exception("Saved-layout redraw did not commit");
                    var shaped = JsonUtility.ToJson(district);
                    RepairDistrictRivers();
                    if (!district.Rivers.Any(r => r.WidthMeters == width && r.Depth == depth))
                        throw new Exception("Shape/Repair lost channel profile");
                    var loaded = RegionSaveStore.Load(_openRegion.RegionId, _districtUndoQaSaveRoot)
                        .Tiles.Find(t => t.TileId == district.TileId);
                    if (JsonUtility.ToJson(loaded) != JsonUtility.ToJson(district)) throw new Exception("Shape/Repair save/reload mismatch");
                    RiverBankQa("bank-check");
                    if (shaped != JsonUtility.ToJson(district))
                    {
                        if (!UndoDistrictEdit()) throw new Exception("Repair undo failed");
                        district = FindSelectedRegionTile();
                        if (JsonUtility.ToJson(district) != shaped) throw new Exception("Repair undo changed shaped layout");
                        RepairDistrictRivers();
                    }
                }
                File.AppendAllText("/tmp/cityforge-river-bank-validation.txt",
                    $"{DateTime.UtcNow:o} {district.Name}: PASS two Shape -> Repair cycles, width/depth, save/reload, repair undo\n");
                return;
            }
            if (command == "bank-view" || command == "bank-wide" || command == "bank-border" || command == "bank-detail" || command == "bank-bend")
            {
                var points = district.Rivers[0].Points;
                int index = command == "bank-border" ? 0 :
                    command == "bank-bend" || command == "bank-detail" ? points.Count / 4 : points.Count / 2;
                var point = points[index];
                _districtEdgePanDirection = Vector2Int.zero;
                _terraformZoomLevel = command == "bank-wide" ? DistrictZoomLevel.LOD4 :
                    command == "bank-detail" ? DistrictZoomLevel.LOD0 : DistrictZoomLevel.LOD1;
                _terraformPanOffset = new((point.X - .5f) * DistrictScale.SizeMeters(district.Width),
                    (point.Z - .5f) * DistrictScale.SizeMeters(district.Height));
                if (command == "bank-detail")
                {
                    var a = points[Mathf.Max(0, index - 1)]; var b = points[Mathf.Min(points.Count - 1, index + 1)];
                    var tangent = new Vector2((b.X - a.X) * DistrictScale.SizeMeters(district.Width),
                        (b.Z - a.Z) * DistrictScale.SizeMeters(district.Height)).normalized;
                    _terraformPanOffset += new Vector2(-tangent.y, tangent.x) * district.Rivers[0].WidthMeters * .51f;
                }
                _districtWorld.SetZoom(_terraformZoomLevel);
                _districtWorld.SetPan(_terraformPanOffset);
                return;
            }
            if (command == "bank-check")
            {
                var meadow = Shader.Find("CityForgeV3/MeadowGroundSurface");
                if (meadow == null || UnityEditor.ShaderUtil.ShaderHasError(meadow))
                    throw new Exception("Meadow shader failed to compile");
                var meadowTexture = Resources.Load<Texture2D>(DistrictWorldController.DistrictGrassResource);
                if (meadowTexture == null || meadowTexture.mipmapCount < 2 || meadowTexture.wrapMode != TextureWrapMode.Repeat)
                    throw new Exception("Meadow texture import invalid");
                string before = JsonUtility.ToJson(district);
                int revision = _districtWorld.SurfaceCacheRevision;
                var beforeMeshes = _districtWorld.GetComponentsInChildren<MeshFilter>()
                    .Where(f => f.name.StartsWith("River")).ToDictionary(f => f.name,
                        f => JsonUtility.ToJson(new MeshReview { Vertices = f.sharedMesh.vertices,
                            UV = f.sharedMesh.uv, Triangles = f.sharedMesh.triangles }));
                _districtWorld.RefreshRivers(district, preservePresentations: true);
                if (JsonUtility.ToJson(district) != before || revision != _districtWorld.SurfaceCacheRevision)
                    throw new Exception("Material refresh changed saved data or invalidated surface cache");
                int banks = 0; float minBend = 0, maxBend = 0;
                foreach (var filter in _districtWorld.GetComponentsInChildren<MeshFilter>())
                {
                    if (!filter.name.StartsWith("River")) continue;
                    var mesh = filter.sharedMesh;
                    var signature = JsonUtility.ToJson(new MeshReview { Vertices = mesh.vertices,
                        UV = mesh.uv, Triangles = mesh.triangles });
                    if (beforeMeshes[filter.name] != signature) throw new Exception("Non-deterministic river rebuild");
                    foreach (var v in mesh.vertices)
                        if (Mathf.Abs(v.x) > DistrictScale.SizeMeters(district.Width) * .5f + .01f ||
                            Mathf.Abs(v.z) > DistrictScale.SizeMeters(district.Height) * .5f + .01f)
                            throw new Exception("Bank extends past district border");
                    var material = filter.GetComponent<Renderer>().sharedMaterial;
                    if (material.shader.name != "CityForgeV3/RiverBankSurface") continue;
                    banks++;
                    if (UnityEditor.ShaderUtil.ShaderHasError(material.shader)) throw new Exception("Bank shader error");
                    foreach (var property in new[] { "_MainTex", "_GravelTex", "_EarthTex" })
                        if (material.GetTexture(property) == null) throw new Exception("Missing bank artwork " + property);
                    if (mesh.uv2.Length != mesh.vertexCount) throw new Exception("Missing bend weights after clipping");
                    foreach (var weight in mesh.uv2)
                    {
                        if (float.IsNaN(weight.x) || Mathf.Abs(weight.x) > 1.001f) throw new Exception("Invalid bend weight");
                        minBend = Mathf.Min(minBend, weight.x); maxBend = Mathf.Max(maxBend, weight.x);
                    }
                }
                if (banks == 0) throw new Exception("No new banks rendered");
                File.AppendAllText("/tmp/cityforge-river-bank-validation.txt",
                    $"{DateTime.UtcNow:o} {district.Name}: PASS {banks} bank bands, bend {minBend:F3}..{maxBend:F3}, textures/shader, border bounds, deterministic rebuild, saved data/cache unchanged\n");
                return;
            }
            throw new ArgumentException("Unknown bank QA command: " + command);
        }
    }
}
#endif

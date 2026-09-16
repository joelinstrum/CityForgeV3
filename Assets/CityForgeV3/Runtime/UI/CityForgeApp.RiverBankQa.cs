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
        void RiverBankQa(string command)
        {
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

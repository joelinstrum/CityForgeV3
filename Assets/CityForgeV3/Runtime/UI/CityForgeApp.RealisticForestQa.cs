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
        System.Collections.IEnumerator ReviewStagedForestSeason()
        {
            var d = FindSelectedRegionTile();
            var state = DistrictLabor.State(d); int original = state.SeasonIndex;
            string before = JsonUtility.ToJson(d); int frames=0;
            try
            {
                state.SeasonIndex = 2;
                do { yield return null; frames++; } while (_districtWorld.ForestSeasonPending || _districtWorld.ForestSeason != SeasonPreset.Winter);
                var clusters = _districtWorld.GetComponentsInChildren<SpriteRenderer>()
                    .Where(r=>ForestClusterCatalog.IsTexture(r.sprite?.texture?.name)).ToArray();
                if (clusters.Any(r=>!r.sprite.texture.name.EndsWith("-winter") || !r.forceRenderingOff)) throw new Exception("Staged winter incomplete");
                File.AppendAllText("/tmp/cityforge-realistic-forest.txt", $"Actual Update loop: {clusters.Length} clusters completed winter in {frames} frames, all batched.\n");
            }
            finally { state.SeasonIndex=original; }
            do { yield return null; } while (_districtWorld.ForestSeasonPending || _districtWorld.ForestSeason != ForestClusterCatalog.SeasonForIndex(original));
            if (before!=JsonUtility.ToJson(d)) throw new Exception("Staged transition mutated district");
            File.AppendAllText("/tmp/cityforge-realistic-forest.txt", "Staged fixture season restored. DONE\n");
        }

        void ReviewRealisticForest(string command)
        {
            var d = FindSelectedRegionTile();
            SetDistrictSimulationPaused(true);
            const string report = "/tmp/cityforge-realistic-forest.txt";
            if (command == "forest-realistic-staged") { StartCoroutine(ReviewStagedForestSeason()); return; }
            if (command == "forest-realistic-check")
            {
                var state = DistrictLabor.State(d);
                int originalSeason = state.SeasonIndex;
                string original = JsonUtility.ToJson(d);
                int revision = _districtWorld.SurfaceCacheRevision;
                var renderers = _districtWorld.GetComponentsInChildren<SpriteRenderer>()
                    .Where(r => r.name.StartsWith("District Flora — ")).ToArray();
                var clusters = renderers.Where(r => ForestClusterCatalog.IsTexture(r.sprite.texture.name)).ToArray();
                if (clusters.Length == 0) throw new Exception("No realistic clusters in fixture");
                var positions = clusters.Select(r => r.transform.position).ToArray();
                var untouched = renderers.Except(clusters).Select(r => r.sprite).ToArray();
                File.WriteAllText(report, $"{DateTime.UtcNow:o} {d.Name}: {d.Flora.Count} records, {clusters.Length} clusters\n");
                try
                {
                    foreach (int index in new[] { 0, 1, 2, 3, 4 })
                    {
                        state.SeasonIndex = index;
                        string before = JsonUtility.ToJson(d);
                        var timer = System.Diagnostics.Stopwatch.StartNew();
                        double maxSlice = 0; int slices = 0;
                        do
                        {
                            var slice = System.Diagnostics.Stopwatch.StartNew();
                            _districtWorld.SyncForestSeason(); slice.Stop();
                            maxSlice = Math.Max(maxSlice, slice.Elapsed.TotalMilliseconds); slices++;
                        } while (_districtWorld.ForestSeasonPending);
                        timer.Stop();
                        var expected = index == 1 ? "autumn" : index == 2 ? "winter" : "summer";
                        if (clusters.Any(r => !r.sprite.texture.name.EndsWith(expected) || !r.forceRenderingOff))
                            throw new Exception("Season sprite or batching mismatch");
                        if (!positions.SequenceEqual(clusters.Select(r => r.transform.position))) throw new Exception("Season moved trees");
                        if (!untouched.SequenceEqual(renderers.Except(clusters).Select(r => r.sprite))) throw new Exception("Season changed unrelated flora");
                        if (revision != _districtWorld.SurfaceCacheRevision || before != JsonUtility.ToJson(d)) throw new Exception("Presentation changed terrain or model");
                        File.AppendAllText(report, $"season={index} transition={timer.Elapsed.TotalMilliseconds:F2}ms slices={slices} maxSlice={maxSlice:F2}ms textures={clusters.Select(r=>r.sprite.texture).Distinct().Count()}\n");
                    }
                    var meshes = _districtWorld.GetComponentsInChildren<MeshFilter>().Select(f => f.sharedMesh).ToArray();
                    var idle = System.Diagnostics.Stopwatch.StartNew();
                    for (int i=0; i<10000; i++) _districtWorld.SyncForestSeason();
                    idle.Stop();
                    if (!meshes.SequenceEqual(_districtWorld.GetComponentsInChildren<MeshFilter>().Select(f=>f.sharedMesh))) throw new Exception("Unchanged season rebuilt meshes");
                    File.AppendAllText(report, $"10000 unchanged season checks={idle.Elapsed.TotalMilliseconds:F3}ms. PASS identities, positions, batching, unrelated flora, terrain cache and JSON retained.\n");
                }
                finally { state.SeasonIndex = originalSeason; do { _districtWorld.SyncForestSeason(); } while (_districtWorld.ForestSeasonPending); }
                if (original != JsonUtility.ToJson(d)) throw new Exception("Season fixture restoration failed");
                return;
            }
            int season = command.Contains("autumn") ? 1 : command.Contains("winter") ? 2 : 0;
            DistrictLabor.State(d).SeasonIndex = season;
            _districtWorld.SyncForestSeason();
            _districtWorld.SetTimeOfDay(TimeOfDayPreset.Afternoon);
            _districtEdgePanDirection = Vector2Int.zero;
            _terraformZoomLevel = command.EndsWith("far") ? DistrictZoomLevel.LOD4 : DistrictZoomLevel.LOD1;
            var cluster = d.Flora.Where(t => ForestClusterCatalog.IsCluster(t.FloraId))
                .OrderBy(t => Mathf.Abs(t.NormalizedX-.5f)+Mathf.Abs(t.NormalizedZ-.5f)).First();
            _terraformPanOffset = command.EndsWith("far") ? Vector2.zero : DistrictLabor.TreePoint(d, cluster);
            _districtWorld.SetPan(_terraformPanOffset); _districtWorld.SetZoom(_terraformZoomLevel);
        }
    }
}
#endif

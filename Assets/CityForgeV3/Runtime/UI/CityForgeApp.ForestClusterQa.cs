#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CityForgeV3.World;
using UnityEngine;

namespace CityForgeV3.UI
{
    public sealed partial class CityForgeApp
    {
        IEnumerator ReviewForestClusters()
        {
            const string report = "/tmp/cityforge-forest-clusters.txt";
            var district = FindSelectedRegionTile();
            var original = district.Flora;
            bool paused = _districtSimulationPaused;
            SetDistrictSimulationPaused(true);
            File.WriteAllText(report, DateTime.UtcNow.ToString("o") + " " + district.Name +
                $" {DistrictScale.SizeMeters(district.Width)}x{DistrictScale.SizeMeters(district.Height)}m original={original.Count}\n");
            try
            {
                _terraformZoomLevel = DistrictZoomLevel.LOD3;
                _districtWorld.SetZoom(_terraformZoomLevel);
                for (int pass = 0; pass < 3; pass++)
                {
                    {
                        // Explicit regeneration of the isolated save copy. Mark its
                        // standing scenery generated so this compares coverage, not
                        // layering another forest over manually planted originals.
                        var copy = JsonUtility.FromJson<RegionCityTile>(JsonUtility.ToJson(district));
                        copy.Flora = JsonUtility.FromJson<RegionCityTile>(JsonUtility.ToJson(new RegionCityTile { Flora = original })).Flora;
                        foreach (var tree in copy.Flora)
                            if (tree.HarvestState == DistrictTreeHarvestState.Standing && !tree.WoodCredited) tree.GeneratedByRegion = true;
                        long allocation = GC.GetAllocatedBytesForCurrentThread();
                        var timer = System.Diagnostics.Stopwatch.StartNew();
                        district.Flora = pass == 0
                            ? ForestLegacyGeneratorQa.Generate(copy, RegionClimate.Temperate, RegionTreeCoverage.Wooded, 193)
                            : RegionFloraGenerator.Generate(copy, RegionClimate.Temperate, pass == 1 ? RegionTreeCoverage.Wooded : RegionTreeCoverage.Heavy, 193);
                        timer.Stop();
                        File.AppendAllText(report, $"generate={timer.Elapsed.TotalMilliseconds:F2}ms allocation={GC.GetAllocatedBytesForCurrentThread()-allocation}B\n");
                        DistrictHarvestIndex.Invalidate(district);
                    }
                    long startAllocation = GC.GetAllocatedBytesForCurrentThread();
                    var refresh = System.Diagnostics.Stopwatch.StartNew();
                    int revision = _districtWorld.SurfaceCacheRevision;
                    _districtWorld.RefreshFlora(district);
                    refresh.Stop();
                    File.AppendAllText(report, $"pass={pass} records={district.Flora.Count} clusters={district.Flora.Count(t=>ForestClusterCatalog.IsCluster(t.FloraId))} harvestable={district.Flora.Count(DistrictTreeHarvest.CanFell)} refresh={refresh.Elapsed.TotalMilliseconds:F2}ms allocation={GC.GetAllocatedBytesForCurrentThread()-startAllocation}B\n");
                    if (revision != _districtWorld.SurfaceCacheRevision) throw new Exception("Flora rebuilt terrain cache");
                    for (int i = 0; i < 60; i++) yield return null;
                    var samples = new float[180]; long draws = 0, triangles = 0;
                    for (int i = 0; i < samples.Length; i++)
                    {
                        yield return null; samples[i] = Time.unscaledDeltaTime * 1000;
                        draws += UnityEditor.UnityStats.drawCalls; triangles += UnityEditor.UnityStats.triangles;
                    }
                    Array.Sort(samples);
                    File.AppendAllText(report, $"pass={pass} median={samples[90]:F2}ms p95={samples[171]:F2}ms max={samples[179]:F2}ms draws={draws/180f:F1} triangles={triangles/180f:F0}\n");
                    RiverBankQa("bank-capture");
                    for (int i=0;i<5;i++) yield return null;
                }
                var sprites = _districtWorld.GetComponentsInChildren<SpriteRenderer>(true)
                    .Where(r=>r.name.StartsWith("District Flora — forest-cluster-")).ToArray();
                if (sprites.Length != district.Flora.Count(t=>ForestClusterCatalog.IsCluster(t.FloraId))) throw new Exception("Missing cluster presentations");
                if (sprites.Any(r=>!r.forceRenderingOff)) throw new Exception("Clusters did not enter shared batches");
                var fir = district.Flora.First(DistrictTreeHarvest.CanFell);
                if (!DistrictTreeHarvest.Fell(fir, 1)) throw new Exception("Cannot fell separate fir");
                _districtWorld.RefreshHarvestTrees(district, new HashSet<string>{fir.InstanceId});
                _districtWorld.PlayTreeFalls(district, new HashSet<string>{fir.InstanceId});
                for (int i=0;i<180;i++) yield return null;
                if (_districtWorld.IsTreeFalling(fir.InstanceId)) throw new Exception("Fir animation did not finish");
                DistrictTreeHarvest.TakeWood(fir, DistrictTreeHarvest.PrototypeWoodYield);
                _districtWorld.RefreshHarvestTrees(district, new HashSet<string>{fir.InstanceId});
                if (sprites.Any(r=>r==null)) throw new Exception("Harvest replaced an unrelated cluster");
                _terraformZoomLevel = DistrictZoomLevel.LOD2;
                _districtWorld.SetZoom(_terraformZoomLevel);
                for(int i=0;i<30;i++)yield return null;
                RiverBankQa("bank-capture");
                for (int i=0;i<5;i++) yield return null;
                File.AppendAllText(report,"PASS: clusters batched, terrain cache retained, separate fir falls and yields wood, cluster objects retained.\n");
            }
            finally
            {
                district.Flora=original; DistrictHarvestIndex.Invalidate(district);
                _districtWorld.RefreshFlora(district); SetDistrictSimulationPaused(paused);
                File.AppendAllText(report,"Fixture flora restored. DONE\n");
            }
        }
    }
}
#endif

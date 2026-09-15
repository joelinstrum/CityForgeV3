#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Diagnostics;
using CityForgeV3.World;
using UnityEngine;
using UnityEngine.UIElements;
namespace CityForgeV3.UI
{
    public sealed partial class CityForgeApp
    {
        void CheckDistrictDeletionPerformanceQa()
        {
            var d=FindSelectedRegionTile(); RemoveDocumentModal();
            d.Flora.Add(new(){InstanceId="delete-one",FloraId="cilician-fir",NormalizedX=.25f,NormalizedZ=.25f});
            d.Flora.Add(new(){InstanceId="keep-one",FloraId="cilician-fir",NormalizedX=.28f,NormalizedZ=.25f});
            _districtWorld.RefreshFlora(d);_districtWorldCompositionKey=DistrictCompositionKey(d);SaveDistrictEdit();
            var keep=_districtWorld.GetComponentsInChildren<SpriteRenderer>().First(r=>r.name.StartsWith("District Flora —"));
            // Retain references to all unrelated scene objects and the existing UI screen.
            var terrain=_districtWorld.GetComponentsInChildren<MeshCollider>().Select(c=>c.sharedMesh).ToArray();
            var trees=_districtWorld.GetComponentsInChildren<SpriteRenderer>().Where(r=>r.name.StartsWith("District Flora —")).ToArray();
            var screen=_root.Q<VisualElement>(className:"district-terraform-screen");
            _districtSelection.Clear();_districtSelection.Add(new(DistrictSelectionKind.Flora,"delete-one"));
            var clock=Stopwatch.StartNew();if(!DeleteDistrictSelection())throw new Exception("Delete failed");clock.Stop();
            var deleteMs=clock.Elapsed.TotalMilliseconds;
            if(d.Flora.Any(f=>f.InstanceId=="delete-one")||d.Flora.Count!=1)throw new Exception("Wrong tree deleted");
            if(!ReferenceEquals(screen,_root.Q<VisualElement>(className:"district-terraform-screen"))||_root.Q("district-loading-screen")!=null)throw new Exception("Delete recreated UI or showed loader");
            if(!terrain.SequenceEqual(_districtWorld.GetComponentsInChildren<MeshCollider>().Select(c=>c.sharedMesh)))throw new Exception("Delete rebuilt terrain");
            if(trees.Count(t=>t!=null&&t.gameObject.activeSelf)!=1)throw new Exception("Unrelated tree replaced");
            // Compare the former hot path with cached navigation for a 5,000-tree district.
            var forest=new RegionCityTile{Width=1,Height=1};
            for(int i=0;i<5000;i++)forest.Flora.Add(new(){InstanceId="tree"+i,FloraId="cilician-fir",NormalizedX=.2f,NormalizedZ=.2f});
            clock.Restart();for(int i=0;i<30;i++)DistrictCompositionKey(forest);clock.Stop();var oldMs=clock.Elapsed.TotalMilliseconds;
            LaborNavigation(forest);clock.Restart();for(int i=0;i<30;i++)LaborNavigation(forest);clock.Stop();var newMs=clock.Elapsed.TotalMilliseconds;
            _laborNavigation=null;
            if(!UndoDistrictEdit()||d.Flora.Count!=2)throw new Exception("Deletion undo failed");
            Directory.CreateDirectory("/tmp/cityforge-deletion-review");
            File.WriteAllText("/tmp/cityforge-deletion-review/verified.txt",$"PASS: single-tree delete kept terrain meshes, remaining tree, and UI screen; no loader; undo restored tree. Delete including save: {deleteMs:F2} ms. 30 navigation checks / 5,000 trees: former full-key work {oldMs:F2} ms, cached {newMs:F3} ms. This is a CPU hot-path benchmark, not overall FPS.");
        }
    }
}
#endif

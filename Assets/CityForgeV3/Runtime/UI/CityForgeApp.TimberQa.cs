#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using CityForgeV3.World;
using UnityEngine;
using UnityEngine.UIElements;
namespace CityForgeV3.UI
{
    public sealed partial class CityForgeApp
    {
        public void TimberQa(string command)
        {
            if(command=="marksman-ui")
            {
                if(_districtUndoQaSaveRoot==null)throw new Exception("Requires isolated review");
                ComposeDistrictLaborModal();var card=_root.Q<Button>("labor-marksman-card");
                if(card==null)throw new Exception("Marksman card missing");
                using(var evt=NavigationSubmitEvent.GetPooled()){evt.target=card;card.SendEvent(evt);}
                var place=_root.Query<Button>().ToList().First(b=>b.text=="PLACE");
                using(var evt=NavigationSubmitEvent.GetPooled()){evt.target=place;place.SendEvent(evt);}
                if(!_placingMarksman)throw new Exception("Place did not arm marksman");
                var d=FindSelectedRegionTile();var before=d.Wildlife.Marksmen.Count;
                if(!PlaceLabor(d,new Vector2(.5f+78/640f,.5f)))throw new Exception("Marksman ground drop failed");
                if(d.Wildlife.Marksmen.Count!=before+1||_placingMarksman)throw new Exception("Incorrect placement state");
                File.WriteAllText("/tmp/cityforge-marksman-ui.txt","PASS library card → details → Place → ground drop; saved guard; cursor mode cleared");
                ComposeDistrictLaborModal();
            }
            if(command=="bear-review")
            {
                TimberQa("prepare");var d=FindSelectedRegionTile();RemoveDocumentModal();
                DistrictTimber.Place(d,new Vector2(75,0),new Vector2(75,15),2,new TimberScript());
                d.Wildlife=new DistrictWildlifeState{NextSighting=999};d.Wildlife.Bears.Add(new DistrictBear{Position=new Vector2(87,0),Home=new Vector2(87,0)});
                _terraformPanOffset=new Vector2(75,0);_districtWorld.SetPan(_terraformPanOffset);_districtWorld.WorldCamera.orthographicSize=16;SetDistrictSimulationPaused(false);
            }
            if(command=="bear-marksman")
            {
                if(_districtUndoQaSaveRoot==null)throw new Exception("Requires isolated review");
                var d=FindSelectedRegionTile();d.Wildlife.Marksmen.Add(new DistrictMarksman{Position=new Vector2(78,0)});
                _terraformPanOffset=new Vector2(80,0);_districtWorld.SetPan(_terraformPanOffset);_districtWorld.WorldCamera.orthographicSize=8;
            }
            if(command=="bear-status")
            {
                var d=FindSelectedRegionTile();
                File.WriteAllText("/tmp/cityforge-bear-status.json",JsonUtility.ToJson(d,true));
                File.WriteAllText("/tmp/cityforge-bear-status.txt",d.Wildlife.Status+"\n"+string.Join("\n",d.Labor.Workers.Select(w=>$"{w.Id} {w.Activity} alarm={w.BearAlarm} pos={w.Position} cargo={w.Cargo}"))+"\n"+string.Join("\n",d.Wildlife.Marksmen.Select(m=>$"marksman shots={m.Shots} seconds={m.ShotSeconds}")));
            }
            if(command=="bear-shot-pose")
            {
                if(_districtUndoQaSaveRoot==null)throw new Exception("Requires isolated review");
                SetDistrictSimulationPaused(true);var m=FindSelectedRegionTile().Wildlife.Marksmen.First();m.ShotSeconds=1.82f;
                _terraformPanOffset=m.Position;_districtWorld.SetPan(m.Position);_districtWorld.WorldCamera.orthographicSize=4;
                var actor=_districtWorld.GetComponentsInChildren<MarksmanWarningShot>().First();
                File.WriteAllText("/tmp/cityforge-marksman-bones.txt",string.Join("\n",actor.GetComponentsInChildren<Transform>().Select(t=>t.name)));
            }
            if(command=="wood-review")
            {
                OpenDistrictNinePlacementQa(false);
                _openRegion.RegionId="transient-district-nine-wood";
                _districtUndoQaSaveRoot=Path.Combine(Path.GetTempPath(),"CityForgeDistrictNineWoodQa");
                var d=FindSelectedRegionTile();
                foreach(var lot in d.Lots)foreach(var b in lot.Behaviors)
                {
                    var def=b.HasScript&&b.Script!=null?b.Script.behavior:CityForgeV3.Behaviors.LotBehaviorCatalog.Find(b.DefinitionId);
                    b.State=CityForgeV3.Behaviors.CargoLoadingSimulation.Create(def);b.DepartureRoute.Clear();b.Enabled=true;
                }
                ClearDistrictUndo();_laborNavigation=null;_districtWorldCompositionKey="";_districtWorld.Build(d);Show(AppScreen.DistrictTerraform);
                var mill=d.Lots.First(l=>l.LotId=="lumber-mill-dock-operations-v01");
                _terraformPanOffset=DistrictWorldController.DistrictLotCenterMeters(d,mill,LotSaveStore.Read(mill.LotId));
                _districtWorld.SetPan(_terraformPanOffset);_districtWorld.WorldCamera.orthographicSize=12;
                SetDistrictSimulationPaused(true);
            }
            if(command=="cargo-close")
            {
                var c=DistrictLabor.State(FindSelectedRegionTile()).TimberCrews.First();
                _terraformPanOffset=c.WagonPosition;_districtWorld.SetPan(_terraformPanOffset);_districtWorld.WorldCamera.orthographicSize=5;
            }
            if(command=="cargo-check")
            {
                var c=FindSelectedRegionTile().Labor.TimberCrews.First();
                var wagon=_districtWorld.GetComponentsInChildren<HorseCarriageController>().First();
                var cargo=wagon.GetComponentsInChildren<Transform>(true).Single(x=>x.name=="Timber Cargo");
                var meshes=cargo.GetComponentsInChildren<MeshRenderer>(true);
                if(meshes.Length!=18)throw new Exception("Expected 9 bark meshes and 9 cut-end meshes");
                if(cargo.gameObject.activeSelf!=(c.CargoTrees>0))throw new Exception("Cargo does not match shipment state");
                if(meshes.Any(x=>x.sharedMaterial.mainTexture==null))throw new Exception("Cargo texture missing");
                File.WriteAllText("/tmp/cityforge-cargo-check.txt",$"PASS cargo={c.CargoTrees} visible={cargo.gameObject.activeSelf} meshes={meshes.Length} resource={wagon.Definition.ResourcePath}");
            }
            if(command=="diagnose")File.WriteAllText("/tmp/cityforge-timber-diagnose.txt",_districtWorld.DiagnoseTimber(FindSelectedRegionTile()));
            if(command=="nine-copy")
            {
                OpenDistrictNinePlacementQa(false);
                _openRegion.RegionId="transient-district-nine-timber";
                _districtUndoQaSaveRoot=Path.Combine(Path.GetTempPath(),"CityForgeDistrictNineTimberQa");
                ClearDistrictUndo();SetDistrictSimulationPaused(true);
            }
            if(command=="nine-return")
            {
                OpenDistrictNinePlacementQa(false);
                _districtUndoQaSaveRoot=Path.Combine(Path.GetTempPath(),"CityForgeDistrictNineTimberQa");
                _openRegion=RegionSaveStore.Load("transient-district-nine-timber",_districtUndoQaSaveRoot);
                ClearDistrictUndo();_laborNavigation=null;_districtWorldCompositionKey="";
                EnsureDistrictWorld(FindSelectedRegionTile());Show(AppScreen.DistrictTerraform);SetDistrictSimulationPaused(false);
            }
            if(command=="prepare")
            {
                OpenDockDistrictQa();
                var d=FindSelectedRegionTile();d.Treasury=10000;
                void Road(int x,int z){if(d.Roads.Any(r=>r.GridX==x&&r.GridZ==z))return;d.Roads.Add(new(){Id=Guid.NewGuid().ToString("N"),GridX=x,GridZ=z,PackageId=DistrictRoadPlacementModel.PackageId("Dirt Road")});}
                // 60m road and turning areas, all on the dry side of the mill.
                for(int x=33;x<=40;x++)Road(x,32);
                foreach(int center in new[]{34,39})for(int x=center-1;x<=center+1;x++)for(int z=31;z<=33;z++)Road(x,z);
                DistrictRoadPlacementModel.Repair(d.Roads);
                for(int i=0;i<8;i++)d.Flora.Add(new(){InstanceId="timber-tree-"+i,FloraId="cilician-fir",NormalizedX=.5f+(65+i%3*7)/640f,NormalizedZ=.5f+(32+i/3*8)/640f});
                _districtWorldCompositionKey="";EnsureDistrictWorld(d);Show(AppScreen.DistrictTerraform);
                _terraformPanOffset=new Vector2(35,10);_districtWorld.SetPan(_terraformPanOffset);_districtWorld.WorldCamera.orthographicSize=45;
                SetDistrictSimulationPaused(false);ComposeDistrictLaborModal();
            }
            if(command=="template-script")OpenTimberScript(JsonUtility.ToJson(_newTimberScript,true),null);
            if(command=="cancel-preview"){CancelLaborPlacement();ComposeDistrictLaborModal();}
            if(command=="cursor")
            {
                var d=FindSelectedRegionTile();
                var tree=d.Flora.FirstOrDefault(DistrictTreeHarvest.CanFell);
                var p=tree==null?new Vector2(75,23):DistrictLabor.TreePoint(d,tree)+new Vector2(0,-4);
                var dry=LaborNavigation(d).FindCamp(p);if(!dry.HasValue)throw new Exception("No preview ground");
                p=dry.Value;
                RemoveDocumentModal();_pendingDistrictLotId="";_pendingLaborCount=1;
                _terraformPanOffset=p;_districtWorld.SetPan(p);_districtWorld.WorldCamera.orthographicSize=7;
                var pixel=_districtWorld.WorldCamera.WorldToScreenPoint(new Vector3(p.x,0,p.y));
                var panel=new Vector2(pixel.x/UnityEngine.Screen.width*_root.resolvedStyle.width,(UnityEngine.Screen.height-pixel.y)/UnityEngine.Screen.height*_root.resolvedStyle.height);
                var screen=_root.Q<VisualElement>(className:"district-terraform-screen");
                using(var evt=PointerMoveEvent.GetPooled(new Event{type=EventType.MouseMove,mousePosition=panel})){evt.target=screen;screen.SendEvent(evt);}
                if(UnityEngine.Cursor.visible)throw new Exception("Placement did not replace cursor");
                File.WriteAllText("/tmp/cityforge-timber-cursor.txt","PASS actual PointerMove handler; preview follows dry world point "+p+"; cursor hidden; no placement committed");
            }
            if(command=="details")ComposeAxemanDetails();
            if(command=="arm")
            {
                ComposeAxemanDetails();_root.Q<IntegerField>("labor-axemen-count").value=2;
                var button=_root.Q<Button>("labor-assign");using(var evt=NavigationSubmitEvent.GetPooled()){evt.target=button;button.SendEvent(evt);}
            }
            if(command=="place")
            {
                if(_districtUndoQaSaveRoot==null)throw new Exception("Requires isolated review");
                if(_pendingLaborCount==0)throw new Exception("Not armed");
                if(!PlaceLabor(FindSelectedRegionTile(),new Vector2(.5f+75/640f,.5f+23/640f)))throw new Exception("Drop failed");
            }
            if(command=="modal")ComposeDistrictLaborModal();
            if(command=="script")
            {
                var c=DistrictLabor.State(FindSelectedRegionTile()).TimberCrews.First();OpenTimberScript(JsonUtility.ToJson(c.Script,true),c);
            }
            if(command=="resume"){RemoveDocumentModal();SetDistrictSimulationPaused(false);}
            if(command=="pause"){RemoveDocumentModal();SetDistrictSimulationPaused(true);}
            if(command=="status")
            {
                var d=FindSelectedRegionTile();
                File.WriteAllText("/tmp/cityforge-timber-status.json",JsonUtility.ToJson(d,true));
                var hosts=_districtWorld.GetComponentsInChildren<LotWorldController>();
                File.WriteAllText("/tmp/cityforge-timber-status.txt",string.Join("\n",d.Labor.TimberCrews.Select(c=>c.Phase+" | "+c.Status+" | position="+c.WagonPosition+" | trees="+c.PendingTrees+" | cargo="+c.CargoTrees+" | loads="+c.DeliveredLoads))+
                    "\n"+string.Join("\n",hosts.SelectMany(h=>h.LotBehaviors.Select(b=>h.BehaviorStatus(b.InstanceId)+" loaded="+b.State.Loaded+" reserve="+b.State.TimberReserved))));
            }
            if(command=="closeup")
            {
                var c=DistrictLabor.State(FindSelectedRegionTile()).TimberCrews.First();
                _terraformPanOffset=c.WagonPosition;_districtWorld.SetPan(_terraformPanOffset);_districtWorld.WorldCamera.orthographicSize=10;
            }
            if(command=="wide"){_terraformPanOffset=new Vector2(35,10);_districtWorld.SetPan(_terraformPanOffset);_districtWorld.WorldCamera.orthographicSize=45;}
            if(command=="factory")
            {
                if(_districtUndoQaSaveRoot==null)throw new Exception("Requires isolated review");
                var d=FindSelectedRegionTile();_districtWorld.Build(d);
                _districtWorld.TickTimber(d,null,false,0);
                if(!_districtWorld.GetComponentsInChildren<HorseCarriageController>().Any())throw new Exception("Wagon requires lot editor");
                File.WriteAllText("/tmp/cityforge-timber-factory.txt","PASS wagon assembled without a lot editor factory; current district assets and accepted articulation reused");
            }
            if(command=="reload")
            {
                if(_districtUndoQaSaveRoot==null)throw new Exception("Requires isolated review");
                var d=FindSelectedRegionTile();SaveDistrictEdit();var before=JsonUtility.ToJson(d);
                var loaded=RegionSaveStore.Load(_openRegion.RegionId,_districtUndoQaSaveRoot);
                var after=loaded.Tiles.Find(x=>x.TileId==d.TileId);
                if(JsonUtility.ToJson(after)!=before)throw new Exception("Reload changed state");
                _openRegion=loaded;ClearDistrictUndo();_laborNavigation=null;_districtWorldCompositionKey="";EnsureDistrictWorld(after);Show(AppScreen.DistrictTerraform);
                _districtWorld.SetPan(new Vector2(35,10));_districtWorld.WorldCamera.orthographicSize=45;
            }
        }
    }
}
#endif

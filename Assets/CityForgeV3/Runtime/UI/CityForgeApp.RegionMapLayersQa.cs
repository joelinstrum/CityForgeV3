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
        [Serializable] sealed class MeshReview { public Vector3[] Vertices;public Vector2[] UV;public int[] Triangles; }
        RegionSaveData _mapQaReturn;AppScreen _mapQaScreen;string _mapQaTile;bool _mapQaActive;
        Vector2 _mapQaScroll;bool _mapQaScrollInitialized;
        public void RegionMapLayersQa(string command)
        {
            if(command=="prepare")
            {
                if(_mapQaActive)throw new Exception("Fixture already active");
                _mapQaActive=true;_mapQaReturn=_openRegion;_mapQaScreen=_currentScreen;_mapQaTile=_selectedRegionTileId;
                _mapQaScroll=_regionMapScrollOffset;_mapQaScrollInitialized=_regionMapScrollInitialized;
                var region=new RegionSaveData{Name="Region map layers review",RegionId="map-layers-review",Width=12,Height=8};
                for(int i=0;i<6;i++)
                {
                    var tile=new RegionCityTile{TileId="map-review-"+i,Name=new[]{"Pine Ridge","Highlands","Dry Valley","Greenfield","Westhaven","Snowcap"}[i],X=i%3*4,Y=i/3*4,Width=4,Height=4,Designation=i%2==0?RegionPlaceDesignation.Town:RegionPlaceDesignation.District};
                    tile.Biome=i==2?RegionBiome.Desert:i==0?RegionBiome.Forest:i==5?RegionBiome.Snow:RegionBiome.Grassland;
                    if(i==1||i==5)tile.Hills=new(){Mountains=true,Version=2,HeightMeters=210,Coverage=.8f,Seed=240+i};
                    if(i==0||i==3)tile.Hills=new(){HeightMeters=55,Coverage=.8f};
                    tile.Rivers.Add(new(){Points=new(){new(){X=0,Z=.6f},new(){X=.3f,Z=.45f},new(){X=.7f,Z=.65f},new(){X=1,Z=.6f}},Depth=DistrictRiverDepth.Deep});
                    region.Tiles.Add(tile);
                }
                region.TransportRoutes.Add(new(){Kind=RegionTransportKind.Highway,Points=new(){new(.3f,1),new(3,2),new(6,1),new(9,2),new(11.5f,1)}});
                region.TransportRoutes.Add(new(){Kind=RegionTransportKind.Rail,Points=new(){new(.4f,6),new(4,5),new(7,6),new(11.5f,5)}});
                _openRegion=region;_selectedRegionTileId="";_regionMapScrollInitialized=false;Show(AppScreen.RegionEditor);
                _root.Q<Button>("region-save-button")?.SetEnabled(false);return;
            }
            if(!_mapQaActive)throw new Exception("Prepare isolated map fixture first");
            if(command=="restore")
            {_districtUndoQaSaveRoot=null;_openRegion=_mapQaReturn;_selectedRegionTileId=_mapQaTile;_regionMapScrollOffset=_mapQaScroll;_regionMapScrollInitialized=_mapQaScrollInitialized;_mapQaActive=false;Show(_mapQaScreen);return;}
            if(command.StartsWith("bank-")){RiverBankQa(command);return;}
            if(command.StartsWith("forest-realistic-")){ReviewRealisticForest(command);return;}
            if(command=="forest-clear-review"){ReviewClearForest();return;}
            if(command=="forest-style"){ReviewForestStyle();return;}
            if(command=="forest-review"){StartCoroutine(ReviewForestClusters());return;}
            if(command=="river-perf-prepare" || command=="river-gap-prepare")
            {
                var candidates=Directory.GetFiles(RegionSaveStore.DefaultRoot,"*.json").Select(p=>JsonUtility.FromJson<RegionSaveData>(File.ReadAllText(p))).ToList();
                var region=candidates.OrderByDescending(r=>r.Tiles.Sum(t=>t.Rivers.Sum(v=>v.Points.Count))).First();
                region.RegionId="river-perf-review";_openRegion=region;
                _districtUndoQaSaveRoot=Path.Combine(Path.GetTempPath(),"CityForgeMapLayersQa");
                var tile=region.Tiles.OrderByDescending(t=>t.RiversEditedLocally).ThenByDescending(t=>t.Rivers.Sum(v=>v.Points.Count)).First();
                if(command=="river-gap-prepare")tile=region.Tiles.Find(t=>t.Name=="City 051")??tile;
                SelectRegionTile(tile.TileId);return;
            }
            if(command=="river-perf-refresh")
            {
                var tile=FindSelectedRegionTile();var timer=System.Diagnostics.Stopwatch.StartNew();
                _districtWorld.RefreshRivers(tile,preservePresentations:true);
                File.AppendAllText("/tmp/cityforge-river-performance.txt",$"{tile.Name}: {tile.Rivers.Count} rivers, {tile.Rivers.Sum(r=>r.Points.Count)} points, refresh {timer.ElapsedMilliseconds} ms\n");return;
            }
            if(command=="surface-cache-roads")
            {
                var tile=FindSelectedRegionTile();int treasury=tile.Treasury;
                DistrictRoadPlacementModel.TryPlace(tile.Roads,2,2,DistrictScale.Columns(tile.Width),DistrictScale.Columns(tile.Height),DistrictRoadPlacementModel.DirtFamily,ref treasury);
                _districtWorld.RefreshRoads(tile);
                var field=typeof(DistrictWorldController).GetField("_roadsByCell",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance);
                var roads=(System.Collections.Generic.Dictionary<Vector2Int,GameObject>)field.GetValue(_districtWorld);
                var first=roads[new Vector2Int(2,2)];int revision=_districtWorld.SurfaceCacheRevision;
                DistrictRoadPlacementModel.TryPlace(tile.Roads,7,7,DistrictScale.Columns(tile.Width),DistrictScale.Columns(tile.Height),DistrictRoadPlacementModel.DirtFamily,ref treasury);
                _districtWorld.RefreshRoads(tile,deferSurfaceRefresh:true);
                if(_districtWorld.SurfaceCacheRevision!=revision)throw new Exception("Road drag flushed surface cache");
                _districtWorld.CommitSurfaceChanges();
                if(_districtWorld.SurfaceCacheRevision!=revision+1 || roads[new Vector2Int(2,2)]!=first)throw new Exception("Road edit invalidated unrelated state");
                _districtWorld.RefreshRoads(tile);
                if(_districtWorld.SurfaceCacheRevision!=revision+1 || roads[new Vector2Int(2,2)]!=first)throw new Exception("No-op road refresh rebuilt objects");
                File.AppendAllText("/tmp/cityforge-river-performance.txt","surface-cache-roads: PASS deferred surface commit, retained road objects, no-op refresh\n");return;
            }
            if(command=="surface-cache-hills")
            {
                var tile=FindSelectedRegionTile();tile.Hills=new(){HeightMeters=45,Coverage=.8f};
                _districtWorld.CommitSurfaceChanges();return;
            }
            if(command=="surface-cache-check")
            {
                var tile=FindSelectedRegionTile();var decoration=_districtWorld.GetComponentInChildren<DistrictGroundDecals>();
                var meshes=_districtWorld.GetComponentsInChildren<MeshFilter>().Where(f=>f.name.StartsWith("District Leaves")).ToDictionary(f=>f.name,f=>f.sharedMesh.GetInstanceID());
                int revision=_districtWorld.SurfaceCacheRevision;
                _districtWorld.CommitSurfaceChanges();
                if(_districtWorld.SurfaceCacheRevision!=revision)throw new Exception("No-op changed cache revision");
                foreach(var filter in decoration.GetComponentsInChildren<MeshFilter>())if(filter.name.StartsWith("District Leaves") && meshes[filter.name]!=filter.sharedMesh.GetInstanceID())throw new Exception("No-op rebuilt decoration");
                // Compare incremental output to a full deterministic rebuild, not just its count.
                string Signature(Mesh mesh)=>JsonUtility.ToJson(new MeshReview{Vertices=mesh.vertices,UV=mesh.uv,Triangles=mesh.triangles});
                var incremental=decoration.GetComponentsInChildren<MeshFilter>().Where(f=>f.name.StartsWith("District Leaves") || f.name.StartsWith("Hill detail")).ToDictionary(f=>f.name,f=>Signature(f.sharedMesh));
                int patches=decoration.PatchCount;
                decoration.Rebuild(_districtWorld,tile,DistrictScale.SizeMeters(tile.Width),DistrictScale.SizeMeters(tile.Height));
                if(patches!=decoration.PatchCount)throw new Exception("Incremental patch count differs from full rebuild");
                foreach(var filter in decoration.GetComponentsInChildren<MeshFilter>())if(filter.gameObject.activeInHierarchy && (filter.name.StartsWith("District Leaves") || filter.name.StartsWith("Hill detail")))
                    if(!incremental.TryGetValue(filter.name,out var expected) || expected!=Signature(filter.sharedMesh))throw new Exception("Incremental decoration differs from full rebuild");
                File.AppendAllText("/tmp/cityforge-river-performance.txt","surface-cache-check: PASS no-op retention and full-rebuild geometry equivalence\n");return;
            }
            if(command=="repair-prepare")
            {
                var tile=_openRegion.Tiles[0];tile.Hills=new();
                tile.Rivers=new(){new(){WidthMeters=128,Depth=DistrictRiverDepth.Deep,Points=new(){new(0,.5f),new(.35f,.5f),new(.37f,.51f),new(.355f,.5f),new(.5f,.5f),new(.515f,.55f),new(.8f,.575f),new(1,.575f)}}};
                _districtUndoQaSaveRoot=Path.Combine(Path.GetTempPath(),"CityForgeMapLayersQa");
                SelectRegionTile(tile.TileId);return;
            }
            if(command=="repair-click")
            {
                var tile=FindSelectedRegionTile();var before=JsonUtility.ToJson(tile.Rivers[0]);
                _districtInterfaceVisible=true;_terraformCategory="Water";Show(AppScreen.DistrictTerraform);
                var button=_root.Q<Button>("district-tool-repair-river");
                if(button==null)throw new Exception("Repair button missing");
                int revision=_districtWorld.SurfaceCacheRevision;
                button.Focus();using(var submit=NavigationSubmitEvent.GetPooled())button.SendEvent(submit);
                if(JsonUtility.ToJson(tile.Rivers[0])==before || !tile.RiversEditedLocally)throw new Exception("Repair click did not change river");
                if(_districtWorld.SurfaceCacheRevision!=revision+1)throw new Exception("Repair must commit cache once");
                var loaded=RegionSaveStore.Load(_openRegion.RegionId,_districtUndoQaSaveRoot).Tiles.Find(t=>t.TileId==tile.TileId);
                if(JsonUtility.ToJson(loaded.Rivers[0])!=JsonUtility.ToJson(tile.Rivers[0]))throw new Exception("Repair save mismatch");
                File.WriteAllText("/tmp/cityforge-repair-before.txt",before);
                return;
            }
            if(command=="repair-undo")
            {
                if(!UndoDistrictEdit())throw new Exception("Repair undo unavailable");
                if(JsonUtility.ToJson(FindSelectedRegionTile().Rivers[0])!=File.ReadAllText("/tmp/cityforge-repair-before.txt"))throw new Exception("Repair undo geometry mismatch");
                return;
            }
            if(command=="sculpt-prepare")
            {
                var tile=_openRegion.Tiles[0];tile.Hills=new();
                tile.Rivers=new(){new(){WidthMeters=64,Depth=DistrictRiverDepth.Deep,Points=new(){new(0,.35f),new(.5f,.5f),new(1,.65f)}}};
                _districtUndoQaSaveRoot=Path.Combine(Path.GetTempPath(),"CityForgeMapLayersQa");
                SelectRegionTile(tile.TileId);return;
            }
            if(command=="sculpt-arm")
            {
                _terraformCategory="Water";Show(AppScreen.DistrictTerraform);
                if(_root.Query<Button>().ToList().Any(b=>b.text=="GENERATE RIVER"))throw new Exception("District generator still exposed");
                var button=_root.Q<Button>("district-tool-shape-river");button.Focus();using(var e=NavigationSubmitEvent.GetPooled())button.SendEvent(e);
                return;
            }
            if(command=="sculpt-undo")
            {
                if(!UndoDistrictEdit())throw new Exception("Sculpt undo unavailable");
                var tile=FindSelectedRegionTile();
                if(tile.Rivers.Count!=1 || !tile.RiversEditedLocally)throw new Exception("Undo did not restore shaped channel");
                var loaded=RegionSaveStore.Load(_openRegion.RegionId,_districtUndoQaSaveRoot).Tiles[0];
                if(loaded.Rivers.Count!=1 || !loaded.RiversEditedLocally)throw new Exception("Undo not persisted");
                File.AppendAllText("/tmp/cityforge-river-sculpt-verified.txt","sculpt-undo: PASS restored shaped river and persisted undo.\n");
                return;
            }
            if(command=="sculpt-menu"){ComposeDistrictRiverModal();return;}
            if(command=="sculpt-shape" || command=="sculpt-erase")
            {
                var tile=FindSelectedRegionTile();var before=JsonUtility.ToJson(tile);
                _terraformTool=command=="sculpt-shape"?"Shape River":"Erase River";
                var screen=_root.Q<VisualElement>(className:"district-terraform-screen");
                void Send(Vector2 normalized,EventType type)
                {
                    var world=new Vector3((normalized.x-.5f)*DistrictScale.SizeMeters(tile.Width),0,(normalized.y-.5f)*DistrictScale.SizeMeters(tile.Height));
                    var pixel=_districtWorld.WorldCamera.WorldToScreenPoint(world);
                    var panel=new Vector2(pixel.x/UnityEngine.Screen.width*_root.resolvedStyle.width,(UnityEngine.Screen.height-pixel.y)/UnityEngine.Screen.height*_root.resolvedStyle.height);
                    var native=new Event{type=type,mousePosition=panel,button=0};
                    if(type==EventType.MouseDown){using(var e=PointerDownEvent.GetPooled(native)){e.target=screen;screen.SendEvent(e);}}
                    else if(type==EventType.MouseUp){using(var e=PointerUpEvent.GetPooled(native)){e.target=screen;screen.SendEvent(e);}}
                    else{using(var e=PointerMoveEvent.GetPooled(native)){e.target=screen;screen.SendEvent(e);}}
                }
                int meshesBefore=_districtWorld.GetComponentsInChildren<MeshFilter>().Length;
                var decalsBefore=_districtWorld.GetComponentsInChildren<MeshFilter>().Where(f=>f.name.StartsWith("District Leaves")).ToDictionary(f=>f.name,f=>f.sharedMesh.GetInstanceID());
                int cacheRevisionBefore=_districtWorld.SurfaceCacheRevision;
                var riverMeshesBefore=_districtWorld.GetComponentsInChildren<MeshFilter>().Where(f=>f.name.StartsWith("River")).Select(f=>f.sharedMesh.GetInstanceID()).ToArray();
                var start=command=="sculpt-shape"?new Vector2(.3f,.44f):new Vector2(.5f,.5f);
                Send(start,EventType.MouseDown);
                Send(new(.5f,.535f),EventType.MouseDrag);
                if(JsonUtility.ToJson(tile)!=before)throw new Exception("Dragging changed saved river data");
                if(_districtWorld.SurfaceCacheRevision!=cacheRevisionBefore)throw new Exception("Dragging updated district cache");
                var riverMeshesDuring=_districtWorld.GetComponentsInChildren<MeshFilter>().Where(f=>f.name.StartsWith("River")).Select(f=>f.sharedMesh.GetInstanceID()).ToArray();
                if(!riverMeshesBefore.SequenceEqual(riverMeshesDuring))throw new Exception("Dragging rebuilt river meshes");
                Send(command=="sculpt-shape"?new Vector2(.7f,.56f):new Vector2(.5f,.535f),EventType.MouseUp);
                if(JsonUtility.ToJson(tile)==before || !tile.RiversEditedLocally)throw new Exception("Sculpt gesture did not commit");
                if(_districtWorld.SurfaceCacheRevision!=cacheRevisionBefore+1)throw new Exception("Edit must update cache exactly once");
                var decoration=_districtWorld.GetComponentInChildren<DistrictGroundDecals>();
                if(decoration.LastUpdatedChunkCount>=decoration.ChunkCount)throw new Exception("Local edit rebuilt every decoration chunk");
                var afterDecals=_districtWorld.GetComponentsInChildren<MeshFilter>().Where(f=>f.name.StartsWith("District Leaves")).ToArray();
                if(!afterDecals.Any(f=>decalsBefore.TryGetValue(f.name,out var id) && id==f.sharedMesh.GetInstanceID()))throw new Exception("Local edit did not retain any chunk mesh");
                File.AppendAllText("/tmp/cityforge-river-performance.txt",$"{command}: updated {decoration.LastUpdatedChunkCount}/{decoration.ChunkCount} decoration chunks, one cache commit\n");
                if(command=="sculpt-erase" && tile.Rivers.Count<2)throw new Exception("Eraser did not split channel");
                var loaded=RegionSaveStore.Load(_openRegion.RegionId,_districtUndoQaSaveRoot).Tiles[0];
                if(!loaded.Rivers.Select(r=>JsonUtility.ToJson(r)).SequenceEqual(tile.Rivers.Select(r=>JsonUtility.ToJson(r))) || !loaded.RiversEditedLocally)throw new Exception("Sculpt save mismatch");
                foreach(var mesh in _districtWorld.GetComponentsInChildren<MeshFilter>())
                    if(mesh.name.StartsWith("River Water") || mesh.name.StartsWith("Riverbed"))
                        foreach(var v in mesh.sharedMesh.vertices)
                            if(Mathf.Abs(v.x)>DistrictScale.SizeMeters(tile.Width)*.5f+.01f || Mathf.Abs(v.z)>DistrictScale.SizeMeters(tile.Height)*.5f+.01f)throw new Exception("River extends outside district border");
                File.AppendAllText("/tmp/cityforge-river-sculpt-verified.txt",command+": PASS actual pointer input, save/reload and mesh border clipping.\n");
                return;
            }
            if(command=="river-remove")
            {
                // Cover generated, hand-drawn and district-local rivers together.
                RegionRiverGenerator.Apply(_openRegion, RegionRiverGenerator.Generate(_openRegion,
                    new RegionTerrainSettings { DeepRivers=RegionWaterAmount.Few }, 123));
                RegionMapLayersQa("river-draw-major");
                CancelNationalPike();
                var roads=JsonUtility.ToJson(_openRegion.TransportRoutes[0]);
                ComposeRegionTerrainModal();
                var remove=_root.Q<Button>("remove-region-rivers");
                if(remove==null)throw new Exception("Missing Remove Rivers button");
                remove.Focus();using(var e=NavigationSubmitEvent.GetPooled())remove.SendEvent(e);
                if(_root.Q("region-terrain-modal")!=null)throw new Exception("River removal failed");
                var loaded=RegionSaveStore.Load(_openRegion.RegionId,Path.Combine(Path.GetTempPath(),"CityForgeMapLayersQa"));
                if(_openRegion.RiverPaths.Count!=0 || loaded.RiverPaths.Count!=0)throw new Exception("Region paths remain");
                foreach(var tile in loaded.Tiles)if(tile.Rivers.Count!=0)throw new Exception("District rivers remain after reload");
                if(JsonUtility.ToJson(loaded.TransportRoutes[0])!=roads)throw new Exception("Removal changed transport route");
                File.AppendAllText("/tmp/cityforge-river-drawing-verified.txt","river-remove: PASS all river types removed by button and save/reload.\n");
                return;
            }
            if(command=="river-draw-major" || command=="river-draw-large" || command=="river-draw-small")
            {
                bool deep=!command.EndsWith("small");
                var size=command.EndsWith("major")?RegionRiverSize.Major:deep?RegionRiverSize.Large:RegionRiverSize.Small;
                ComposeRegionTerrainModal();
                if(_root.Q("region-river-flow")!=null)throw new Exception("Direction selector still present");
                var button=_root.Q<Button>("create-"+size.ToString().ToLowerInvariant()+"-river");
                button.Focus();using(var e=NavigationSubmitEvent.GetPooled())button.SendEvent(e);
                if(!_drawingRiver || _root.Q("region-terrain-modal")!=null)throw new Exception("River pencil did not start");
                var overlay=_pikeOverlay;
                void Send(Vector2 point,EventType type)
                {
                    var native=new Event{type=type,mousePosition=overlay.LocalToWorld(point*RegionMapUnitPixels),button=0};
                    if(type==EventType.MouseDown){using(var e=PointerDownEvent.GetPooled(native))overlay.SendEvent(e);}
                    else if(type==EventType.MouseUp){using(var e=PointerUpEvent.GetPooled(native))overlay.SendEvent(e);}
                    else{using(var e=PointerMoveEvent.GetPooled(native))overlay.SendEvent(e);}
                }
                int count=_openRegion.RiverPaths.Count;
                Send(new(1,1),EventType.MouseDown);Send(new(1,1),EventType.MouseUp);
                if(_openRegion.RiverPaths.Count!=count)throw new Exception("Click created river");
                Send(new(1,1),EventType.MouseDown);Send(new(3,1.4f),EventType.MouseDrag);Send(new(5,1.8f),EventType.MouseUp);
                if(!_drawingRiver || !_drawingNationalPike || _openRegion.RiverPaths.Count!=count+1)throw new Exception("River release failed");
                var loaded=RegionSaveStore.Load(_openRegion.RegionId,Path.Combine(Path.GetTempPath(),"CityForgeMapLayersQa"));
                var path=loaded.RiverPaths[loaded.RiverPaths.Count-1];
                if(!path.HandDrawn || path.Depth!=(deep?DistrictRiverDepth.Deep:DistrictRiverDepth.Shallow))throw new Exception("Wrong river profile saved");
                if(path.WidthMeters!=RegionRiverDrawing.WidthMeters(size))throw new Exception("Wrong river width saved");
                if(Vector2.Distance(new(path.Points[0].X,path.Points[0].Z),new(1,1))>.001f)throw new Exception("River draw coordinate mismatch");
                File.AppendAllText("/tmp/cityforge-river-drawing-verified.txt",command+": PASS menu, blue-pencil mode, click rejection, release, save/reload, coordinates and profile.\n");
                return;
            }
            if(command=="river-continue")
            {
                if(!_drawingRiver)throw new Exception("River pencil did not remain active");
                int count=_openRegion.RiverPaths.Count;
                var overlay=_pikeOverlay;
                void Send(Vector2 point,EventType type)
                {
                    var native=new Event{type=type,mousePosition=overlay.LocalToWorld(point*RegionMapUnitPixels),button=0};
                    if(type==EventType.MouseDown){using(var e=PointerDownEvent.GetPooled(native))overlay.SendEvent(e);}
                    else if(type==EventType.MouseUp){using(var e=PointerUpEvent.GetPooled(native))overlay.SendEvent(e);}
                    else{using(var e=PointerMoveEvent.GetPooled(native))overlay.SendEvent(e);}
                }
                var previous=_openRegion.RiverPaths[count-1];
                var endpoint=previous.Points[previous.Points.Count-1];
                var join=new Vector2(endpoint.X,endpoint.Z);
                Send(join,EventType.MouseDown);Send(new(6,2.4f),EventType.MouseDrag);Send(new(7,3),EventType.MouseUp);
                if(!_drawingRiver || _openRegion.RiverPaths.Count!=count+1)throw new Exception("Continuous river tool failed");
                var loaded=RegionSaveStore.Load(_openRegion.RegionId,Path.Combine(Path.GetTempPath(),"CityForgeMapLayersQa"));
                var joined=loaded.RiverPaths[loaded.RiverPaths.Count-1];
                if(Vector2.Distance(new(joined.Points[0].X,joined.Points[0].Z),join)>.001f)throw new Exception("Continuation did not join previous endpoint");
                CancelNationalPike();
                File.AppendAllText("/tmp/cityforge-river-drawing-verified.txt","river-continue: PASS retained pencil, second stroke, connected endpoint, save/reload.\n");
                return;
            }
            if(command=="pike-draw")
            {
                ComposeRegionTerrainModal();
                var roads=_root.Q<Button>("region-terrain-roads");roads.Focus();using(var e=NavigationSubmitEvent.GetPooled())roads.SendEvent(e);
                var create=_root.Q<Button>("create-national-pike");if(create==null)throw new Exception("Missing national pike action");
                create.Focus();using(var e=NavigationSubmitEvent.GetPooled())create.SendEvent(e);
                if(_root.Q("document-modal")!=null||!_drawingNationalPike)throw new Exception("Road menu did not close for drawing");
                var overlay=_pikeOverlay;
                void Send(Vector2 unit,EventType type)
                {
                    var position=overlay.LocalToWorld(unit*RegionMapUnitPixels);
                    var native=new Event{type=type,mousePosition=position,button=0};
                    if(type==EventType.MouseDown){using(var e=PointerDownEvent.GetPooled(native))overlay.SendEvent(e);}
                    else if(type==EventType.MouseUp){using(var e=PointerUpEvent.GetPooled(native))overlay.SendEvent(e);}
                    else {using(var e=PointerMoveEvent.GetPooled(native))overlay.SendEvent(e);}
                }
                Send(new Vector2(1,1),EventType.MouseDown);Send(new Vector2(1,1),EventType.MouseUp);
                if(_pikeNaming)throw new Exception("A click created a road");
                Send(new Vector2(1,1),EventType.MouseDown);
                Send(new Vector2(2,1.8f),EventType.MouseDrag);Send(new Vector2(3,1.5f),EventType.MouseDrag);
                Send(new Vector2(5,2.8f),EventType.MouseUp);
                if(!_pikeNaming||_root.Q("region-pike-name-modal")==null)throw new Exception("Release did not open naming");
                if(Vector2.Distance(_pikeStroke.Points[0],new Vector2(1,1))>.01f||Vector2.Distance(_pikeStroke.Points[_pikeStroke.Points.Count-1],new Vector2(5,2.8f))>.01f)throw new Exception("Rotated-map drawing coordinates differ");
                if(_root.Q<Button>("region-pike-save").enabledSelf)throw new Exception("Empty name accepted");
                return;
            }
            if(command=="pike-save")
            {
                int before=_openRegion.TransportRoutes.Count;
                _root.Q<TextField>("region-pike-name").value="Cumberland Pike";
                var save=_root.Q<Button>("region-pike-save");save.Focus();using(var e=NavigationSubmitEvent.GetPooled())save.SendEvent(e);
                if(_drawingNationalPike||_root.Q("document-modal")!=null||_openRegion.TransportRoutes.Count!=before+1)throw new Exception("Pike save did not finish");
                var route=_openRegion.TransportRoutes[_openRegion.TransportRoutes.Count-1];
                if(_root.Q<Label>("region-road-label-"+route.Id)?.text!="Cumberland Pike")throw new Exception("Named road label missing");
                var loaded=RegionSaveStore.Load(_openRegion.RegionId,Path.Combine(Path.GetTempPath(),"CityForgeMapLayersQa"));
                if(loaded.TransportRoutes[loaded.TransportRoutes.Count-1].Name!="Cumberland Pike")throw new Exception("Named road not persisted");
                int roadCount=0;var mask=new NationalPikePlacement.WaterMask(loaded);
                foreach(var tile in loaded.Tiles)foreach(var road in tile.Roads)
                {
                    if(road.NationalPikeId!=route.Id)continue;roadCount++;
                    if(road.PackageId!=RoadPiecePackageCatalog.NationalPikeDirtId)throw new Exception("Pike dirt package missing");
                    if(mask.Contains(new Vector2(tile.X*640+(road.GridX+.5f)*10,tile.Y*640+(road.GridZ+.5f)*10),7.1f))throw new Exception("Pike tile overlaps river");
                }
                if(roadCount<100)throw new Exception("Pencil did not create district roads");
                File.WriteAllText("/tmp/cityforge-pike-verified.txt","PASS: Roads action closes menu; pointer drag maps correctly through rotation; click-only rejected; release opens naming; empty name rejected; named road saved and reloads; map label appears. " + roadCount + " dirt road tiles persist in districts and avoid river water.");
                return;
            }
            if(command=="pike-cancel")
            {
                int before=_openRegion.TransportRoutes.Count;RegionMapLayersQa("pike-draw");CancelNationalPike();
                if(_drawingNationalPike||_root.Q("document-modal")!=null||_openRegion.TransportRoutes.Count!=before)throw new Exception("Cancel committed a draft");
                File.AppendAllText("/tmp/cityforge-pike-verified.txt"," Cancelled naming discards the draft without adding a route.");return;
            }
            if(command=="topography-off")
            {
                var toggle=_root.Q<Toggle>("region-layer-topography");toggle.Focus();
                using(var e=NavigationSubmitEvent.GetPooled())toggle.SendEvent(e);
                if(toggle.value)throw new Exception("Topography input did not toggle off");return;
            }
            if(command=="check")
            {
                var screen=_root.Q<VisualElement>(className:"region-editor-screen");
                if(screen.Q<Toggle>("region-layer-districts").value || screen.Q("region-district-labels").style.display.value!=DisplayStyle.None)
                    throw new Exception("District names/borders must default off");
                var relief=screen.Q<VisualElement>("region-topography-map-layer");
                var scroll=screen.Q<ScrollView>("region-map-scroll");var offset=scroll.scrollOffset;
                foreach(var pair in new[]{("towns","region-place-labels"),("districts","region-district-labels"),("rivers","region-river-map-layer"),("topography","region-topography-map-layer"),("transportation","region-transportation-map-layer")})
                {
                    var toggle=screen.Q<Toggle>("region-layer-"+pair.Item1);toggle.value=false;
                    if(screen.Q(pair.Item2).style.display.value!=DisplayStyle.None)throw new Exception("Layer not hidden: "+pair.Item1);
                    toggle.value=true;
                    if(screen.Q(pair.Item2).style.display.value!=DisplayStyle.Flex)throw new Exception("Layer not restored: "+pair.Item1);
                }
                if(!ReferenceEquals(relief,screen.Q("region-topography-map-layer"))||offset!=scroll.scrollOffset)throw new Exception("Layer toggles rebuilt map or moved scroll");
                screen.Q<Toggle>("region-layer-districts").value=false;
                var button=screen.Q<Button>("region-map-layers-button");button.Focus();using(var e=NavigationSubmitEvent.GetPooled())button.SendEvent(e);
                if(screen.Q("region-map-layers-menu").style.display.value!=DisplayStyle.Flex)throw new Exception("Dropdown did not open");
                File.WriteAllText("/tmp/cityforge-map-layers-verified.txt","PASS: five independent layer toggles; district names and borders default off, dropdown button, preserved relief instance and scroll position.");
            }
        }
    }
}
#endif

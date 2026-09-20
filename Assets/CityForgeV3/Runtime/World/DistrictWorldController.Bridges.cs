using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace CityForgeV3.World
{
    public sealed partial class DistrictWorldController
    {
        [Serializable] sealed class BridgeMeshData
        { public string name; public Vector3[] vertices, normals; public Vector2[] uv; public int[] triangles; }
        [Serializable] sealed class BridgePackage
        {
            public float bayLength, capLength, rightCapLength;
            public bool singleMiddle, fixedModel;
            public float fixedLength, halfWidth;
            public float sourceMin, sourceMax, leftCut, rightCut;
            public float leftEndDeck, rightEndDeck;
            public float[] deckHeights;
            public BridgeMeshData[] modules;
        }
        sealed class BridgeAssets
        { public BridgePackage Package; public Material Material; }
        readonly Dictionary<string,BridgeAssets> _bridgeAssets=new();
        readonly Dictionary<string,GameObject> _bridgeObjects=new();
        readonly DistrictSpatialIndex<PlacedDistrictBridge> _bridgesByCell=new(32);
        readonly Dictionary<string,PlacedDistrictBridge> _bridgesById=new();
        GameObject _bridgePreview;
        readonly Dictionary<string,Material> _bridgeRampMaterials=new();

        public DistrictBridgePlanner.Surface SampleBridgeSurface(Vector2 p)
        {
            if(_content==null)return new DistrictBridgePlanner.Surface(false,false,0,0);
            var river=SampleRiverSurface(_content.TransformPoint(new Vector3(p.x,0,p.y)));
            return new DistrictBridgePlanner.Surface(river.HasValue,river?.UnderWater==true,
                TerrainElevation(p.x,p.y),river.HasValue?
                _content.InverseTransformPoint(new Vector3(0,river.Value.WaterElevation,0)).y:0);
        }
        public PlacedDistrictBridge BridgeAt(Vector2 p, float halfWidth=DistrictBridgePlanner.HalfWidth)
        {
            var candidates=_bridgesByCell.Query(p);
            for(int i=0;i<candidates.Count;i++)
            {
                var b=candidates[i];
                if(DistrictBridgePlanner.Contains(_terrainDistrict,b,p,halfWidth,out _))return b;
            }
            return null;
        }
        public float TravelElevation(Vector2 p)
        {
            var b=BridgeAt(p,DistrictBridgePlanner.TravelHalfWidth);
            if(b!=null && DistrictBridgePlanner.Contains(_terrainDistrict,b,p,DistrictBridgePlanner.TravelHalfWidth,out var along))
            {
                var height=DistrictBridgePlanner.Height(_terrainDistrict,b,along);
                if(b.StyleId=="stone" && _bridgeAssets.TryGetValue("stone",out var assets) && assets.Package.singleMiddle)
                    height+=StoneDeckOffset(assets.Package,
                        Vector2.Distance(DistrictBridgePlanner.Center(_terrainDistrict,b.Start),
                            DistrictBridgePlanner.Center(_terrainDistrict,b.End)),along);
                if(b.FixedModel && _bridgeAssets.TryGetValue(b.StyleId,out var whole) &&
                    along>=b.NearApproach && along<=b.NearApproach+whole.Package.fixedLength)
                {
                    var profile=whole.Package.deckHeights;
                    float t=Mathf.Clamp01((along-b.NearApproach)/whole.Package.fixedLength)*(profile.Length-1);
                    int i=Mathf.Min(Mathf.FloorToInt(t),profile.Length-2);
                    height=b.DeckHeight+Mathf.Lerp(profile[i],profile[i+1],t-i);
                }
                return height;
            }
            return TerrainElevation(p.x,p.y)+.02f;
        }
        // Alternating complete center sections mates identical source cut faces.
        // Odd counts retain the original left/right connector orientation at both ends.
        static int StoneBayCount(float usable,float nominal) =>
            Mathf.Clamp(2*Mathf.RoundToInt((usable/nominal-1)*.5f)+1,1,15);
        static float StoneDeckOffset(BridgePackage package,float total,float along)
        {
            float connector=DistrictBridgePlanner.RampLength+package.capLength;
            float usable=total-2*DistrictBridgePlanner.RampLength-package.capLength-package.rightCapLength;
            if(along<DistrictBridgePlanner.RampLength || along>total-DistrictBridgePlanner.RampLength || usable<=0)
                return 0;
            float sourceX,shift;
            if(along<=connector)
            {sourceX=package.sourceMin+(along-DistrictBridgePlanner.RampLength)/35f;shift=package.leftEndDeck;}
            else if(along<connector+usable)
            {
                int bays=StoneBayCount(usable,package.bayLength);
                float bay=usable/bays;
                int bayIndex=Mathf.Min(Mathf.FloorToInt((along-connector)/bay),bays-1);
                float t=(along-connector-bayIndex*bay)/bay;
                if((bayIndex&1)!=0)t=1-t;
                sourceX=Mathf.Lerp(package.leftCut,package.rightCut,t);
                shift=Mathf.Lerp(package.leftEndDeck,package.rightEndDeck,t);
            }
            else
            {sourceX=package.rightCut+(along-connector-usable)/35f;shift=package.rightEndDeck;}
            var heights=package.deckHeights;
            if(heights==null || heights.Length<2)return 0;
            float sampleX=Mathf.Clamp01((sourceX-package.sourceMin-.005f)/
                (package.sourceMax-package.sourceMin-.01f))*(heights.Length-1);
            int index=Mathf.Min(Mathf.FloorToInt(sampleX),heights.Length-2);
            return Mathf.Lerp(heights[index],heights[index+1],sampleX-index)-shift;
        }
        public bool IsBlockedByRiverForTravel(Vector2 normalized)
        {
            var p=new Vector2((normalized.x-.5f)*_widthMeters,(normalized.y-.5f)*_depthMeters);
            return BridgeAt(p,DistrictBridgePlanner.TravelHalfWidth)==null && IsUnderRiverWater(normalized);
        }
        void BuildDistrictBridges(RegionCityTile district)
        {
            _bridgesByCell.Clear();_bridgesById.Clear();
            foreach(var b in district.Bridges??new List<PlacedDistrictBridge>())AddDistrictBridge(district,b);
        }
        public void AddDistrictBridge(RegionCityTile district,PlacedDistrictBridge b)
        {
            if(b==null||string.IsNullOrEmpty(b.Id)||_bridgesById.ContainsKey(b.Id))return;
            var obj=CreateBridge(district,b,false);if(obj==null)return;
            _bridgeObjects[b.Id]=obj;_bridgesById[b.Id]=b;
            _bridgesByCell.Add(DistrictBridgePlanner.Bounds(district,b),b);
        }
        public void RemoveDistrictBridge(RegionCityTile district,PlacedDistrictBridge b)
        {
            _bridgesByCell.Remove(DistrictBridgePlanner.Bounds(district,b),b);_bridgesById.Remove(b.Id);
            if(_bridgeObjects.Remove(b.Id,out var obj))DestroyBridgeObject(obj);
        }
        public void PreviewDistrictBridge(RegionCityTile district,PlacedDistrictBridge b)
        { HideDistrictBridgePreview();_bridgePreview=CreateBridge(district,b,true); }
        public void HideDistrictBridgePreview()
        { if(_bridgePreview!=null)DestroyBridgeObject(_bridgePreview);_bridgePreview=null; }
        static void DestroyBridgeObject(GameObject obj)
        {
            if(obj==null)return;
            foreach(var filter in obj.GetComponentsInChildren<MeshFilter>())
                if(filter.sharedMesh!=null){if(Application.isPlaying)Destroy(filter.sharedMesh);else DestroyImmediate(filter.sharedMesh);}
            obj.SetActive(false);if(Application.isPlaying)Destroy(obj);else DestroyImmediate(obj);
        }
        void ClearDistrictBridges()
        {
            HideDistrictBridgePreview();
            foreach(var obj in _bridgeObjects.Values)DestroyBridgeObject(obj);
            _bridgeObjects.Clear();_bridgesByCell.Clear();_bridgesById.Clear();
            foreach(var assets in _bridgeAssets.Values)
                if(Application.isPlaying)Destroy(assets.Material);else DestroyImmediate(assets.Material);
            _bridgeAssets.Clear();
            foreach(var material in _bridgeRampMaterials.Values)
                if(Application.isPlaying)Destroy(material);else DestroyImmediate(material);
            _bridgeRampMaterials.Clear();
        }
        BridgeAssets LoadBridgeAssets(string id)
        {
            if(_bridgeAssets.TryGetValue(id,out var existing))return existing;
            var style=DistrictBridgeCatalog.Find(id);if(style==null)return null;
            var source=Resources.Load<TextAsset>(style.Resource+"/modules");if(source==null)return null;
            var shader=Shader.Find("Standard");
            var mat=new Material(shader){name=style.Name,mainTexture=Resources.Load<Texture2D>(style.Resource+"/albedo")};
            mat.SetFloat("_Smoothness",.12f);mat.SetFloat("_Glossiness",.12f);
            var assets=new BridgeAssets{Package=JsonUtility.FromJson<BridgePackage>(source.text),Material=mat};
            _bridgeAssets[id]=assets;return assets;
        }
        public bool TryFitBridgeStyle(RegionCityTile district,PlacedDistrictBridge crossing,string styleId,
            Func<Vector2,bool> occupied,out PlacedDistrictBridge fitted,out string reason)
        {
            fitted=null;reason="Bridge model is unavailable.";
            var assets=LoadBridgeAssets(styleId);if(assets==null)return false;
            if(assets.Package.fixedModel)
            {
                if(!DistrictBridgePlanner.TryFitFixed(district,crossing,assets.Package.fixedLength,assets.Package.rightEndDeck,
                    SampleBridgeSurface,occupied,out fitted,out reason))return false;
            }
            else {fitted=JsonUtility.FromJson<PlacedDistrictBridge>(JsonUtility.ToJson(crossing));reason="";}
            fitted.StyleId=styleId;return true;
        }
        GameObject CreateWholeBridge(RegionCityTile district,PlacedDistrictBridge b,BridgeAssets assets,bool preview)
        {
            if(!b.FixedModel)return null;
            var a=DistrictBridgePlanner.Center(district,b.Start);var end=DistrictBridgePlanner.Center(district,b.End);
            var axis=(end-a).normalized;float span=Vector2.Distance(a,end);
            if(Mathf.Abs(span-b.NearApproach-b.FarApproach-assets.Package.fixedLength)>.01f)return null;
            var root=new GameObject((preview?"Preview — ":"")+DistrictBridgeCatalog.Find(b.StyleId).Name);
            root.transform.SetParent(_content,false);root.transform.localPosition=new Vector3(a.x,b.DeckHeight,a.y);
            root.transform.localRotation=Quaternion.LookRotation(new Vector3(axis.x,0,axis.y));
            var source=assets.Package.modules[0];var vertices=new Vector3[source.vertices.Length];
            for(int i=0;i<vertices.Length;i++)vertices[i]=source.vertices[i]+Vector3.forward*b.NearApproach;
            var mesh=new Mesh{name="Original complete bridge",indexFormat=IndexFormat.UInt32};
            mesh.vertices=vertices;mesh.normals=source.normals;mesh.uv=source.uv;mesh.triangles=source.triangles;mesh.RecalculateBounds();
            var body=new GameObject("Bridge span");body.transform.SetParent(root.transform,false);
            body.AddComponent<MeshFilter>().sharedMesh=mesh;body.AddComponent<MeshRenderer>().sharedMaterial=assets.Material;
            AddBridgeRamps(root.transform,b,span,Mathf.Max(2.5f,assets.Package.halfWidth-.35f),assets.Package);
            AddBridgeEarthApproaches(root.transform,b,span);return root;
        }
        GameObject CreateBridge(RegionCityTile district,PlacedDistrictBridge b,bool preview)
        {
            var assets=LoadBridgeAssets(b.StyleId);if(assets==null||_content==null)return null;
            var a=DistrictBridgePlanner.Center(district,b.Start);var end=DistrictBridgePlanner.Center(district,b.End);
            var axis=(end-a).normalized;var span=Vector2.Distance(a,end);
            if(span<DistrictBridgePlanner.MinLength || span>DistrictBridgePlanner.MaxLength)return null;
            var package=assets.Package;
            if(package.fixedModel)return CreateWholeBridge(district,b,assets,preview);
            float rightCap=package.singleMiddle?package.rightCapLength:package.capLength;
            float usable=span-2*DistrictBridgePlanner.RampLength-package.capLength-rightCap;
            if(usable<=0)return null;
            int bays=package.singleMiddle?StoneBayCount(usable,package.bayLength):Mathf.Clamp(Mathf.RoundToInt(usable/package.bayLength),1,16);
            float bay=usable/bays;
            var root=new GameObject((preview?"Preview — ":"")+DistrictBridgeCatalog.Find(b.StyleId).Name);
            root.transform.SetParent(_content,false);root.transform.localPosition=new Vector3(a.x,b.DeckHeight,a.y);
            root.transform.localRotation=Quaternion.LookRotation(new Vector3(axis.x,0,axis.y));
            var vertices=new List<Vector3>();var normals=new List<Vector3>();var uv=new List<Vector2>();var triangles=new List<int>();
            void Append(BridgeMeshData module,float offset,float scale,float shiftStart=0,float shiftEnd=0)
            {
                int first=vertices.Count;
                float pierBottom=-4.275f;
                if(module.name=="Support_Pier")
                {
                    var localPoint=a+axis*offset;
                    var sample=SampleRiverSurface(_content.TransformPoint(new Vector3(localPoint.x,0,localPoint.y)));
                    float bed=sample.HasValue?_content.InverseTransformPoint(new Vector3(0,sample.Value.BedElevation,0)).y:TerrainElevation(localPoint.x,localPoint.y);
                    pierBottom=Mathf.Min(-.8f,bed-b.DeckHeight-.3f);
                }
                for(int i=0;i<module.vertices.Length;i++)
                {
                    var p=module.vertices[i];
                    float originalAlong=p.z;
                    if(package.singleMiddle)
                        p.y+=Mathf.Lerp(shiftStart,shiftEnd,
                            Mathf.Clamp01(originalAlong/package.bayLength));
                    p.z=originalAlong*scale+offset;
                    // Extend only the foot of each wooden pier; the timber deck and roof retain their shape.
                    if(module.name=="Support_Pier" && p.y<-.7f)
                    {
                        p.y=Mathf.Lerp(-.7f,pierBottom,Mathf.Clamp01((-p.y-.7f)/(4.275f-.7f)));
                    }
                    vertices.Add(p);var n=module.normals[i];
                    n.z=(n.z-(shiftEnd-shiftStart)/package.bayLength*n.y)/scale;
                    normals.Add(n.normalized);uv.Add(module.uv[i]);
                }
                for(int t=0;t<module.triangles.Length;t+=3)
                {
                    triangles.Add(first+module.triangles[t]);
                    triangles.Add(first+module.triangles[t+(scale<0?2:1)]);
                    triangles.Add(first+module.triangles[t+(scale<0?1:2)]);
                }
            }
            float connector=DistrictBridgePlanner.RampLength+package.capLength;
            foreach(var module in package.modules)
            {
                if(package.singleMiddle)
                {
                    float leftShift=-package.leftEndDeck;
                    float rightShift=-package.rightEndDeck;
                    if(module.name=="Entrance_Start")Append(module,connector,1,leftShift,leftShift);
                    else if(module.name=="Entrance_End")Append(module,connector+usable-package.bayLength,1,rightShift,rightShift);
                    else if(module.name=="Middle_Bay")
                        for(int i=0;i<bays;i++)
                        {
                            bool reverse=(i&1)!=0;
                            Append(module,connector+(i+(reverse?1:0))*bay,
                                (reverse?-1:1)*bay/package.bayLength,leftShift,rightShift);
                        }
                }
                else if(module.name=="Entrance_Start")Append(module,connector,1);
                else if(module.name=="Entrance_End")Append(module,connector+usable,1);
                else for(int i=0;i<bays;i++)Append(module,connector+(i+(module.name=="Support_Pier"?.5f:0))*bay,
                    module.name=="Support_Pier"?1:bay/package.bayLength);
            }
            var mesh=new Mesh{name="Assembled bridge",indexFormat=IndexFormat.UInt32};mesh.SetVertices(vertices);mesh.SetNormals(normals);mesh.SetUVs(0,uv);mesh.SetTriangles(triangles,0);mesh.RecalculateBounds();
            var body=new GameObject("Bridge span");body.transform.SetParent(root.transform,false);
            body.AddComponent<MeshFilter>().sharedMesh=mesh;body.AddComponent<MeshRenderer>().sharedMaterial=assets.Material;
            AddBridgeRamps(root.transform,b,span);
            AddBridgeEarthApproaches(root.transform,b,span);
            return root;
        }
        void AddBridgeRamps(Transform root,PlacedDistrictBridge b,float span,float entryHalfWidth=3.81f,BridgePackage package=null)
        {
            string surfaceId=b.RoadFamily==DistrictRoadPlacementModel.AntiqueBrickFamily?"antique-brick":"dirt";
            var surface=RoadMaterialCatalog.Resolve(surfaceId);
            if(!_bridgeRampMaterials.TryGetValue(surfaceId,out var rampMaterial))
            {
                rampMaterial=new Material(Shader.Find("CityForgeV3/BridgeApproachBlend"))
                {name="Bridge approach — "+surface.DisplayName,mainTexture=surface.LoadTexture()};
                rampMaterial.SetFloat("_MaterialTiling",surface.TilesPerTenMeters);
                _bridgeRampMaterials[surfaceId]=rampMaterial;
            }
            var verts=new List<Vector3>();var uv=new List<Vector2>();var colors=new List<Color>();var tris=new List<int>();
            float tile=surface.TilesPerTenMeters/10f;
            void Strip(float z0,float y0,float width0,float alpha0,float z1,float y1,float width1,float alpha1)
            {
                int start=verts.Count;
                verts.Add(new(-width0,y0,z0));verts.Add(new(-width1,y1,z1));verts.Add(new(width1,y1,z1));verts.Add(new(width0,y0,z0));
                for(int i=start;i<start+4;i++)
                {var world=root.TransformPoint(verts[i]);uv.Add(new(world.x*tile,world.z*tile));}
                colors.Add(new(1,1,1,alpha0));colors.Add(new(1,1,1,alpha1));
                colors.Add(new(1,1,1,alpha1));colors.Add(new(1,1,1,alpha0));
                tris.AddRange(new[]{start,start+1,start+2,start,start+2,start+3});
            }
            float near=b.FixedModel?b.NearApproach:DistrictBridgePlanner.RampLength;
            float far=b.FixedModel?b.FarApproach:DistrictBridgePlanner.RampLength;
            float blend=b.FixedModel?Mathf.Min(2f,(package?.fixedLength??4)*.12f):0;
            float NearDeck(float metres)
            {
                var profile=package?.deckHeights;if(profile==null||profile.Length<2||package.fixedLength<=0)return 0;
                float sample=Mathf.Clamp01(metres/package.fixedLength)*(profile.Length-1);
                int index=Mathf.Min(Mathf.FloorToInt(sample),profile.Length-2);
                return Mathf.Lerp(profile[index],profile[index+1],sample-index);
            }
            Strip(0,b.StartHeight-b.DeckHeight,3.81f,1,near,.025f,entryHalfWidth,1);
            if(blend>0)Strip(near,.025f,entryHalfWidth,1,near+blend,NearDeck(blend)+.025f,entryHalfWidth,0);
            float bodyEnd=span-far;
            if(blend>0)Strip(bodyEnd-blend,NearDeck(package.fixedLength-blend)+.025f,entryHalfWidth,0,
                bodyEnd,b.EndDeckOffset+.025f,entryHalfWidth,1);
            Strip(bodyEnd,b.FixedModel?b.EndDeckOffset+.025f:0,entryHalfWidth,1,span,b.EndHeight-b.DeckHeight,3.81f,1);
            var mesh=new Mesh{name="Bridge approaches"};mesh.SetVertices(verts);mesh.SetUVs(0,uv);mesh.SetColors(colors);mesh.SetTriangles(tris,0);mesh.RecalculateNormals();mesh.RecalculateBounds();
            var go=new GameObject("Approaches");go.transform.SetParent(root,false);go.AddComponent<MeshFilter>().sharedMesh=mesh;go.AddComponent<MeshRenderer>().sharedMaterial=rampMaterial;
        }

        void AddBridgeEarthApproaches(Transform root,PlacedDistrictBridge b,float span)
        {
            if(_groundRenderer==null)return;
            var verts=new List<Vector3>();var uv=new List<Vector2>();var tris=new List<int>();
            // Two small local embankments: road-width crest and 2:1 grass shoulders.
            // Fixed station count; no terrain repaint or district-wide rebuild.
            for(int end=0;end<2;end++)
            {
                int first=verts.Count;
                for(int row=0;row<=8;row++)
                {
                    float t=row/8f;
                    float ramp=b.FixedModel?(end==0?b.NearApproach:b.FarApproach):DistrictBridgePlanner.RampLength;
                    float farDeck=b.FixedModel?b.EndDeckOffset:0;
                    float along=end==0?t*ramp:span-ramp+t*ramp;
                    float crest=Mathf.Lerp(end==0?b.StartHeight-b.DeckHeight:farDeck,
                        end==0?0:b.EndHeight-b.DeckHeight,t)-.06f;
                    var center=_content.InverseTransformPoint(root.TransformPoint(new Vector3(0,crest,along)));
                    float rise=Mathf.Max(0,center.y-TerrainElevation(center.x,center.z));
                    float toe=Mathf.Min(DistrictBridgePlanner.ApproachHalfWidth,3.95f+Mathf.Max(.5f,rise*2f));
                    for(int col=0;col<4;col++)
                    {
                        float x=col==0?-toe:col==1?-3.95f:col==2?3.95f:toe;
                        var local=_content.InverseTransformPoint(root.TransformPoint(new Vector3(x,crest,along)));
                        if(col==0||col==3)
                        {
                            local.y=TerrainElevation(local.x,local.z)+.006f;
                            var foot=root.InverseTransformPoint(_content.TransformPoint(local));
                            verts.Add(new Vector3(x,foot.y,along));
                        }
                        else verts.Add(new Vector3(x,crest,along));
                        uv.Add(new Vector2(local.x/_widthMeters+.5f,local.z/_depthMeters+.5f));
                    }
                }
                for(int row=0;row<8;row++)for(int col=0;col<3;col++)
                {
                    int n=first+row*4+col;
                    tris.AddRange(new[]{n,n+4,n+5,n,n+5,n+1});
                }
            }
            var mesh=new Mesh{name="Graded bridge earth"};mesh.SetVertices(verts);mesh.SetUVs(0,uv);
            mesh.SetTriangles(tris,0);mesh.RecalculateNormals();mesh.RecalculateBounds();
            var go=new GameObject("Graded earth approaches");go.transform.SetParent(root,false);
            go.AddComponent<MeshFilter>().sharedMesh=mesh;
            go.AddComponent<MeshRenderer>().sharedMaterial=_groundRenderer.sharedMaterial;
        }
    }
}

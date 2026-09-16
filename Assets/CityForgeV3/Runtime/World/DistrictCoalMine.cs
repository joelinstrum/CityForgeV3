using System.Collections.Generic;
using UnityEngine;
using CityForgeV3.Buildings3D;
namespace CityForgeV3.World
{
    public static class DistrictCoalMine
    {
        public const string ResourcePath="CityForgeV3/Industry/CoalMineV01/CoalMine";
        // The native export's entrance/rails point along local +Z; rear points -Z.
        public static bool CanBuild(RegionCityTile district,DistrictResourceDeposit deposit,out float yaw)
        {
            yaw=0;
            if(district==null||deposit==null||deposit.Kind!="coal"||deposit.MineBuilt||district.ResourceDeposits==null||!district.ResourceDeposits.Contains(deposit))return false;
            return SuitableMountainSite(district,new DistrictElevation(district),deposit.NormalizedX,deposit.NormalizedZ,out yaw);
        }
        public static bool SuitableMountainSite(RegionCityTile district,DistrictElevation terrain,float nx,float nz,out float yaw)
        {
            yaw=0;if(district.Hills?.Mountains!=true)return false;
            float x=(nx-.5f)*terrain.Width,z=(nz-.5f)*terrain.Depth;
            float baseHeight=terrain.Sample(x,z);
            if(baseHeight<.1f||baseHeight>12)return false;
            var up=new Vector2(terrain.Sample(x+5,z)-terrain.Sample(x-5,z),terrain.Sample(x,z+5)-terrain.Sample(x,z-5));
            if(up.sqrMagnitude<.0001f)return false;up.Normalize();
            float access=terrain.Sample(x-up.x*5,z-up.y*5);
            if(baseHeight-access>3)return false;
            if(terrain.Sample(x+up.x*8,z+up.y*8)-access<8)return false;
            if(terrain.Sample(x+up.x*30,z+up.y*30)-access<28)return false;
            yaw=Mathf.Atan2(-up.x,-up.y)*Mathf.Rad2Deg;return true;
        }
        public static bool Build(RegionCityTile district,DistrictResourceDeposit deposit)
        {
            if(!CanBuild(district,deposit,out float yaw))return false;
            deposit.MineYawDegrees=yaw;deposit.MineBuilt=true;return true;
        }
    }
    public sealed class DistrictCoalMinePresentation : MonoBehaviour
    {
        private readonly List<Material> materials=new();
        private readonly List<Mesh> fittedMeshes=new();
        public int ApproachVertices {get;private set;}
        public float InsetMeters {get;private set;}
        public float RearBurialMeters {get;private set;}
        public float EntranceGroundMeters {get;private set;}
        public void Build(DistrictWorldController world,DistrictResourceDeposit deposit,float width,float depth)
        {
            var prefab=Resources.Load<GameObject>(DistrictCoalMine.ResourcePath);if(prefab==null)return;
            float x=(deposit.NormalizedX-.5f)*width,z=(deposit.NormalizedZ-.5f)*depth;
            var rotation=Quaternion.Euler(0,deposit.MineYawDegrees,0);
            // Solve along the saved uphill axis; do not change the logical resource site.
            var forward=rotation*Vector3.forward;
            float seatedX=x,seatedZ=z;
            float floor=world.TerrainElevation(x+forward.x*4.765198f,z+forward.z*4.765198f)+.03f;
            // The shed occupies the rear half of the source; rails project downhill.
            // Keep the doorway apron exposed while seating the back wall in rock.
            for(float inset=-8f;inset<=8f;inset+=.125f)
            {
                float sx=x-forward.x*inset,sz=z-forward.z*inset;
                // Seat the entrance, not the remote rail tip. The approach is graded below.
                float candidateFloor=world.TerrainElevation(sx,sz)+.06f;
                float entry=world.TerrainElevation(sx,sz)-candidateFloor;
                if(entry>.08f)continue;
                seatedX=sx;seatedZ=sz;floor=candidateFloor;InsetMeters=inset;
                RearBurialMeters=world.TerrainElevation(sx-forward.x*3.5f,sz-forward.z*3.5f)-floor;
                EntranceGroundMeters=entry;
                if(RearBurialMeters>=3.8f)break;
            }
            var nudge=world.SavedNudge(new DistrictSelectionRef(DistrictSelectionKind.Entity,"mine:"+deposit.Id));
            transform.localPosition=new Vector3(seatedX+nudge.x,floor,seatedZ+nudge.y);
            transform.localRotation=rotation;
            var model=Instantiate(prefab,transform);model.name="Mine shed";
            FitRailApproach(model,world);
            ImportedBuildingMaterials.Prepare(model.transform,materials);
            // Write mine depth before the opaque terrain color pass (1999).
            // The terrain then covers only the submerged pixels; its existing
            // no-depth-write contract for flora and billboard buildings is preserved.
            foreach(var material in materials)material.renderQueue=1998;
            world.RegisterSelectable(gameObject, new DistrictSelectionRef(DistrictSelectionKind.Entity, "mine:" + deposit.Id),
                "COAL MINE", "This mine follows the mountain slope; its orientation is fixed to preserve the entrance and rail approach.").WithBuildingDeletion(() => deposit.MineBuilt=false, true, world.RefreshCoalBuildings).WithStatus(() => DistrictBusinessEconomy.Describe(null));
        }
        private void FitRailApproach(GameObject model,DistrictWorldController world)
        {
            foreach(var filter in model.GetComponentsInChildren<MeshFilter>())
            {
                var source=filter.sharedMesh;
                if(source==null||!source.isReadable)continue;
                var mesh=Instantiate(source);mesh.name=source.name+" — Graded mine approach";
                var vertices=mesh.vertices;int changed=0;
                for(int i=0;i<vertices.Length;i++)
                {
                    var local=transform.InverseTransformPoint(filter.transform.TransformPoint(vertices[i]));
                    if(local.z<=0||local.y>=1f)continue;
                    // Keep building walls/roof rigid; feather the low downhill approach only.
                    float weight=Mathf.SmoothStep(0,1,Mathf.Clamp01(local.z/.8f))
                        *(1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(.4f,1f,local.y)));
                    var worldPoint=world.transform.InverseTransformPoint(transform.TransformPoint(local));
                    float delta=world.TerrainElevation(worldPoint.x,worldPoint.z)+.06f-transform.localPosition.y;
                    local.y+=delta*weight;
                    vertices[i]=filter.transform.InverseTransformPoint(transform.TransformPoint(local));changed++;
                }
                if(changed==0){Destroy(mesh);continue;}
                mesh.vertices=vertices;mesh.RecalculateNormals();mesh.RecalculateBounds();
                filter.sharedMesh=mesh;fittedMeshes.Add(mesh);ApproachVertices+=changed;
            }
        }
        private void OnDestroy(){foreach(var mesh in fittedMeshes){if(Application.isPlaying)Destroy(mesh);else DestroyImmediate(mesh);}foreach(var m in materials){if(Application.isPlaying)Destroy(m);else DestroyImmediate(m);}}
    }
}

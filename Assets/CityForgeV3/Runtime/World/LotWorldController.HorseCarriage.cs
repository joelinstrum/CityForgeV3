using System.Collections.Generic;
using UnityEngine;
namespace CityForgeV3.World
{
    public sealed partial class LotWorldController
    {
        readonly Dictionary<string,HorseCarriageController.MotionState> _carriageRebuildMotions=new();
        object _carriageMotionData;
        void CaptureCarriageMotions()
        {
            _carriageRebuildMotions.Clear();
            var sameData=ReferenceEquals(_carriageMotionData,_session?.Data);
            _carriageMotionData=_session?.Data;
            var props=_session?.Data?.Props;if(props==null||!sameData)return;
            for(var i=0;i<props.Count&&i<_propPresentations.Count;i++)
            {
                if(!IsHorseWagon(props[i].PropId)||!props[i].HasCarriagePose)continue;
                var root=_propPresentations[i];var c=root!=null?root.GetComponent<HorseCarriageController>():null;
                if(c!=null)_carriageRebuildMotions[props[i].InstanceId]=c.CaptureMotion();
            }
        }
        void RestoreCarriagePose(Transform root,PlacedProp prop)
        {
            if(!IsHorseWagon(prop.PropId))return;
            var c=root.GetComponent<HorseCarriageController>();if(c==null)return;
            c.Fast=prop.CarriageFast;
            if(!prop.HasCarriagePose)return;
            c.RestoreHeadings(prop.HorseHeadingDegrees,prop.CarriageHeadingDegrees,prop.ForecarriageHeadingDegrees);
            if(_carriageRebuildMotions.TryGetValue(prop.InstanceId,out var state))c.RestoreMotion(state);
        }
        public const string HorseCarriagePropId="horse-carriage-v02";
        public const string HorseForestryWagonPropId="horse-forestry-wagon-v02";
        public const string HorseLumberWagonPropId="horse-lumber-wagon-v01";
        public const string HorseCoveredWagonPropId="horse-covered-wagon-v01";
        public const string HorseFoodWagonPropId="horse-food-wagon-v01";
        public static bool IsHorseWagon(string id)=>id==HorseForestryWagonPropId||id==HorseCarriagePropId||id==HorseLumberWagonPropId||id==HorseCoveredWagonPropId||id==HorseFoodWagonPropId;
        public string SelectedHorseWagonName=>SelectedPropIsCarriageTeam?HorseWagonDefinition.For(_session.Data.Props[SelectedPropIndex].PropId).DisplayName:"Horse and wagon";
        public bool SelectedPropIsCarriageTeam=>SelectedPropIndex>=0&&SelectedPropIndex<PropCount&&IsHorseWagon(_session.Data.Props[SelectedPropIndex].PropId);
        public bool SelectedPropCanWalk=>SelectedPropIsAnimal||SelectedPropIsCarriageTeam;
        public Transform CreateHorseCarriagePresentation(string name,float alpha,string propId, System.Func<Vector3,float> ground = null)
        {
            var vehicle=HorseWagonDefinition.For(propId);
            var root=new GameObject(name).transform;
            var horse=CreatePropPresentation(HorseAnimalId,"Carriage Horse",alpha);
            var carriage=CreateCarriagePresentation(vehicle.DisplayName,alpha,vehicle.ResourcePath,vehicle);
            if(horse==null||carriage==null){Destroy(root.gameObject);if(horse!=null)Destroy(horse.gameObject);if(carriage!=null)Destroy(carriage.gameObject);return null;}
            horse.SetParent(root,false);carriage.SetParent(root,false);
            Transform secondHorse=null;
            if(vehicle.HorseCount==2)
            {
                secondHorse=CreatePropPresentation(HorseAnimalId,"Second Wagon Horse",alpha);
                if(secondHorse==null){Destroy(root.gameObject);return null;}
                secondHorse.SetParent(root,false);
                horse.localPosition=Vector3.left*vehicle.HorseSpacing*.5f;
                secondHorse.localPosition=Vector3.right*vehicle.HorseSpacing*.5f;
            }
            root.gameObject.AddComponent<HorseCarriageController>().Configure(horse,carriage,vehicle,secondHorse);
            if(alpha>=.999f)
                root.gameObject.AddComponent<HorseCarriageGroundShadow>().Initialize(horse,carriage,ground ?? CarriageShadowGroundY,vehicle,secondHorse);
            return root;
        }
        float CarriageShadowGroundY(Vector3 point)
        {
            var local=_propRoot.InverseTransformPoint(point);
            return _propRoot.TransformPoint(new Vector3(local.x,SampleTerrainHeight(local.x,local.z)+.145f,local.z)).y;
        }
        bool CarriageSegmentClear(Vector3 from,Vector3 to)
        {
            var a=_propRoot.InverseTransformPoint(from);var b=_propRoot.InverseTransformPoint(to);
            return AnimalSegmentClear(new Vector2(a.x,a.z),new Vector2(b.x,b.z));
        }
        public bool CommandSelectedCarriage(Vector2 destination)
        {
            if(!SelectedPropIsCarriageTeam||SelectedPropIndex>=_propPresentations.Count)return false;
            var controller=_propPresentations[SelectedPropIndex].GetComponent<HorseCarriageController>();
            if(controller==null)return false;
            RebuildBearObstacles();
            var prop=_session.Data.Props[SelectedPropIndex];var from=new Vector2(prop.PositionX,prop.PositionZ);
            var route=FindAnimalRoute(from,destination);
            List<Vector3> smooth=null;
            if(route!=null)
            {
                var points=new List<Vector3>();foreach(var p in route)points.Add(_propRoot.TransformPoint(new Vector3(p.x,0,p.y)));
                smooth=controller.Plan(points,CarriageSegmentClear);
                if(smooth!=null&&!controller.RouteClear(smooth,CarriageSegmentClear))smooth=null;
            }
            ShowAnimalDestinationArrow(from,destination,smooth!=null);
            if(smooth==null)return false;
            controller.SetRoute(smooth);return true;
        }
        HorseCarriageController SelectedCarriageController =>
            SelectedPropIsCarriageTeam && SelectedPropIndex < _propPresentations.Count &&
            _propPresentations[SelectedPropIndex] != null
                ? _propPresentations[SelectedPropIndex].GetComponent<HorseCarriageController>() : null;

        public bool SelectedCarriageFast => SelectedPropIsCarriageTeam &&
            _session.Data.Props[SelectedPropIndex].CarriageFast;

        public void SetSelectedCarriageFast(bool fast)
        {
            var controller = SelectedCarriageController;
            if (controller == null) return;
            _session.Data.Props[SelectedPropIndex].CarriageFast = fast;
            controller.Fast = fast;
            NotifyStateChanged();
        }

        public bool SelectedCarriageIsStopping => SelectedCarriageController?.IsStopping == true;
        public bool SelectedCarriageIsMoving => SelectedCarriageController?.IsMoving == true;

        public void StopSelectedCarriage()
        {
            var controller = SelectedCarriageController;
            if (controller == null) return;
            controller.Stop();
            _session.Data.Props[SelectedPropIndex].AnimationState = controller.IsMoving ? "walk" : "idle";
            NotifyStateChanged();
        }

        public bool DriveSelectedCarriage()
        {
            var controller = SelectedCarriageController;
            if (controller == null) return false;
            RebuildBearObstacles();
            var local = controller.transform.localPosition;
            var location = new Vector2(local.x, local.z);
            var options = new List<(VehicleRoute route, float distance, float score)>();
            // Use the lot's road cycle, with extra corner smoothing for the
            // horse, front axle and rear axle to negotiate the bend in sequence.
            foreach (var clockwise in new[] { true, false })
            {
                var route = VehicleRoute.FromNetwork(_session.Data.VehicleNetwork, 1.05f, 4, clockwise);
                if (route == null || route.TotalLengthMeters < 12f) continue;
                var best = float.PositiveInfinity;
                var nearest = 0f;
                for (var distance = 0f; distance < route.TotalLengthMeters; distance += .25f)
                {
                    route.Sample(distance, out var point, out var direction);
                    var heading = _propRoot.TransformDirection(new Vector3(direction.x, 0, direction.y));
                    var score = Vector2.SqrMagnitude(point - location) +
                        8f * (1f - Vector3.Dot(controller.transform.forward, heading));
                    if (score < best) { best = score; nearest = distance; }
                }
                options.Add((route, nearest, best));
            }
            options.Sort((a,b) => a.score.CompareTo(b.score));
            foreach (var option in options)
            foreach (var lookAhead in new[] { 6f, 10f, 16f })
            {
                var entry = option.distance + lookAhead;
                option.route.Sample(entry, out var point, out var direction);
                var target = _propRoot.TransformPoint(new Vector3(point.x, 0, point.y));
                var heading = _propRoot.TransformDirection(new Vector3(direction.x, 0, direction.y));
                var joined = controller.Plan(new[] { target }, CarriageSegmentClear, heading);
                if (joined == null) continue;
                var repeatFrom = joined.Count;
                var count = Mathf.CeilToInt(option.route.TotalLengthMeters / .1f);
                for (var i = 1; i <= count; i++)
                {
                    option.route.Sample(entry + option.route.TotalLengthMeters * i / count,
                        out var next, out _);
                    var world = _propRoot.TransformPoint(new Vector3(next.x, 0, next.y));
                    world.y = 0;
                    joined.Add(world);
                }
                // Validate a second lap too: its starting axle pose differs
                // from the initial approach to the road.
                var validation = new List<Vector3>(joined);
                validation.AddRange(joined.GetRange(repeatFrom, joined.Count-repeatFrom));
                if (!controller.RouteClear(validation, CarriageSegmentClear)) continue;
                controller.SetRoute(joined, repeatFrom);
                NotifyStateChanged();
                return true;
            }
            return false;
        }

        void UpdateHorseCarriages()
        {
            var props=_session?.Data?.Props;if(props==null)return;
            var refreshed=false;
            for(var i=0;i<props.Count&&i<_propPresentations.Count;i++)
            {
                if(!IsHorseWagon(props[i].PropId))continue;
                var root=_propPresentations[i];if(root==null||!root.gameObject.activeInHierarchy)continue;
                root.GetComponent<HorseCarriageGroundShadow>()?.SetLighting(ProjectedObjectShadowRay(),
                    IsRaining||TimeOfDay==TimeOfDayPreset.Night);
                var c=root.GetComponent<HorseCarriageController>();if(c==null||!c.IsMoving)continue;
                if(!refreshed){RebuildBearObstacles();refreshed=true;}
                float Ground(Vector3 p){var local=_propRoot.InverseTransformPoint(p);return _propRoot.TransformPoint(new Vector3(local.x,CharacterGroundY(HorseAnimalId)+SampleTerrainHeight(local.x,local.z),local.z)).y;}
                c.Step(Time.deltaTime,Ground,CarriageSegmentClear);
                props[i].HasCarriagePose=true;
                props[i].HorseHeadingDegrees=root.localEulerAngles.y;
                props[i].ForecarriageHeadingDegrees=(Quaternion.Inverse(_propRoot.rotation)*c.Forecarriage.rotation).eulerAngles.y;
                props[i].CarriageHeadingDegrees=(Quaternion.Inverse(_propRoot.rotation)*c.Carriage.rotation).eulerAngles.y;
                props[i].PositionX=root.localPosition.x;props[i].PositionZ=root.localPosition.z;
                props[i].AnimationState=c.IsMoving?"walk":"idle";
                if(i==SelectedPropIndex)ApplyPropSelection();
                if(!c.IsMoving)NotifyStateChanged();
            }
        }
    }
}

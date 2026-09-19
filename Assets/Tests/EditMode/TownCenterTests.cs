using System.Linq;
using CityForgeV3.Buildings3D;
using CityForgeV3.World;
using NUnit.Framework;
using UnityEngine;

namespace CityForgeV3.Tests.EditMode
{
    public sealed class TownCenterTests
    {
        GameObject root;
        Camera camera;

        [SetUp] public void SetUp()
        {
            var source=Resources.Load<GameObject>("CityForgeV3/Buildings3D/TownCenterV01/Prefabs/TownCenter");
            Assert.That(source,Is.Not.Null);
            root=Object.Instantiate(source);
            camera=new GameObject("Isolated Town Center test camera").AddComponent<Camera>();
            camera.orthographic=true;camera.orthographicSize=9;
            camera.transform.position=new Vector3(-20,16,24);camera.transform.LookAt(new Vector3(0,4.5f,0));
        }
        [TearDown] public void TearDown()
        {
            if(root!=null)Object.DestroyImmediate(root);
            if(camera!=null)Object.DestroyImmediate(camera.gameObject);
        }

        [Test] public void NightAndEveningArePerInstanceAndDayDisablesEveryLamp()
        {
            var package=root.GetComponent<Building3DPackageInstance>();
            var life=root.GetComponentInChildren<BuildingInteriorAutomata>();
            var material=root.GetComponentsInChildren<Renderer>().First(r=>r.name=="TC_Interior").sharedMaterial;
            var original=material.GetColor("_EmissionColor");
            foreach(var amount in new[]{1f,.65f,0f})
            {
                package.SetNightAmount(amount);life.Advance(0,camera);
                foreach(var ctl in root.GetComponentsInChildren<BuildingNightLighting>())Assert.That(ctl.NightAmount,Is.EqualTo(amount));
                Assert.That(root.GetComponentsInChildren<Light>().Count(l=>l.enabled),Is.EqualTo(amount>0?5:0));
                var room=root.GetComponentsInChildren<Renderer>().First(r=>r.name=="TC_Interior");
                var block=new MaterialPropertyBlock();room.GetPropertyBlock(block,0);
                Assert.That(block.GetColor("_EmissionColor").r,Is.EqualTo(.62f*amount).Within(.001));
            }
            Assert.That(material.GetColor("_EmissionColor"),Is.EqualTo(original));
        }

        [Test] public void InteriorMovesWithoutLeavingItsAnchorAndCullsAtDistance()
        {
            var life=root.GetComponentInChildren<BuildingInteriorAutomata>();
            var art=root.GetComponentInChildren<SpriteRenderer>(true);
            var anchor=art.transform.localPosition;
            life.Advance(0,camera);var first=art.sprite;
            life.Advance(4,camera);Assert.That(art.sprite,Is.Not.SameAs(first));
            Assert.That(art.transform.localPosition,Is.EqualTo(anchor));
            root.transform.rotation=Quaternion.Euler(0,90,0);life.Advance(0,camera);
            Assert.That(art.transform.localPosition,Is.EqualTo(anchor));
            root.GetComponent<Building3DPackageInstance>().SetNightAmount(1);
            camera.orthographicSize=100;life.Advance(1,camera);
            Assert.That(life.IsPresenting,Is.False);
            Assert.That(root.GetComponentsInChildren<Light>().Any(l=>l.enabled),Is.False);
            camera.orthographicSize=9;life.Advance(0,camera);Assert.That(life.IsPresenting,Is.True);
            life.DisableForShadowCopy();life.Advance(1,camera);
            Assert.That(art.gameObject.activeSelf,Is.False);
        }

        [Test] public void RearWallClosesTheOpeningAndDistantShellReducesGeometry()
        {
            var package=root.GetComponent<Building3DPackageInstance>().Package;
            var near=package.Find(Building3DLevel.LOD0);
            var far=package.Find(Building3DLevel.LOD3);
            Assert.That(far.TargetTriangleBudget,Is.LessThan(near.TargetTriangleBudget*.5f));
            Assert.That(far.VisualPrefab.GetComponentInChildren<BuildingInteriorAutomata>(),Is.Null);
            var wall=root.GetComponentsInChildren<MeshFilter>().First(m=>m.name=="TC_RearSiding");
            var collider=wall.gameObject.AddComponent<MeshCollider>();collider.sharedMesh=wall.sharedMesh;
            Assert.That(collider.Raycast(new Ray(new Vector3(0,3,-10),Vector3.forward),out var hit,20),Is.True);
            Assert.That(hit.point.z,Is.InRange(-4.4f,-4.1f));
            Assert.That(BuildingContentCatalog.Find("town-center-v01"),Is.Not.Null);
        }

        [Test] public void RealLotPlacementAppliesNightAndSuppressesShadowCopyOccupants()
        {
            var owner=new GameObject("Transient Town Center Lot");
            // Let the real Lot create its own camera; the isolated renderer
            // fixture camera is not part of this integration scenario.
            Object.DestroyImmediate(camera.gameObject);camera=null;
            try
            {
                var world=owner.AddComponent<LotWorldController>();world.Build();
                world.NewEmptyLot("Town Center fixture",LotType.Civics,4,4);
                world.AddExperimentalBuilding3D("town-center-v01",0,0,0);
                world.SetTimeOfDay(TimeOfDayPreset.Night);
                var lives=owner.GetComponentsInChildren<BuildingInteriorAutomata>(true);
                Assert.That(lives.Count(l=>l.enabled),Is.EqualTo(1));
                foreach(var disabled in lives.Where(l=>!l.enabled))
                    Assert.That(disabled.GetComponentInChildren<SpriteRenderer>(true).gameObject.activeSelf,Is.False);
                var controls=owner.GetComponentsInChildren<BuildingNightLighting>(true);
                Assert.That(controls.Any(c=>c.enabled && c.NightAmount==1),Is.True);
            }
            finally { Object.DestroyImmediate(owner); }
        }
    }
}

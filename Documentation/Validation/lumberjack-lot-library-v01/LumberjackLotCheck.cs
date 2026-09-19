using System;
using System.Linq;
using CityForgeV3.World;
using UnityEngine;
public static class LumberjackLotCheck {
 public static void Run() {
 var go=new GameObject("Scratch lot factory"); go.SetActive(false);
 var factory=go.AddComponent<LotWorldController>();
 Transform actor=null, cart=null;
 try {
 actor=factory.CreatePropPresentation(LotWorldController.LumberjackCharacterId,"Fixture lumberjack",1);
 if(actor==null || actor.GetComponent<ThreeDimensionalCharacterAnimator>()==null) throw new Exception("Missing lumberjack/animator");
 var textures=actor.GetComponentsInChildren<Renderer>().SelectMany(r=>r.sharedMaterials).Where(m=>m!=null && m.mainTexture!=null).Select(m=>m.mainTexture.name).ToArray();
 if(!textures.Contains("AxemanBaseColor") || !textures.Contains("AxeBaseColor")) throw new Exception("Missing original character/axe textures: "+string.Join(",",textures));
 var clips=Resources.LoadAll<AnimationClip>(DistrictWorldController.AxemanResource);
 if(!clips.Any(c=>c.name.ToLowerInvariant().Contains("chop"))) throw new Exception("Missing Chop clip");
 cart=factory.CreateHorseCarriagePresentation("Fixture forestry cart",1,LotWorldController.HorseForestryWagonPropId,_=>0);
 if(cart==null || cart.GetComponent<HorseCarriageController>()==null) throw new Exception("Missing forestry cart/controller");
 var data=new LotSaveData(); data.Props.Add(new PlacedProp {PropId=LotWorldController.LumberjackCharacterId}); data.Props.Add(new PlacedProp {PropId=LotWorldController.HorseForestryWagonPropId});
 var restored=JsonUtility.FromJson<LotSaveData>(JsonUtility.ToJson(data));
 if(restored.Props[0].PropId!=data.Props[0].PropId || restored.Props[1].PropId!=data.Props[1].PropId) throw new Exception("IDs did not round trip");
 System.IO.File.WriteAllText("lumberjack-lot-check.txt","PASS: lumberjack mesh, animator, original body/axe textures, Chop clip, forestry cart/controller, and both saved prop IDs. Isolated fixture only; no harvesting or delivery simulation tested.");
 } finally {if(actor!=null)UnityEngine.Object.DestroyImmediate(actor.gameObject);if(cart!=null)UnityEngine.Object.DestroyImmediate(cart.gameObject);UnityEngine.Object.DestroyImmediate(go);}
 }
}

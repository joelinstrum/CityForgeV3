#if UNITY_EDITOR
using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using CityForgeV3.UI;
using CityForgeV3.World;
public static class FloraFamilyQa
{
 [MenuItem("City Forge/Flora/Check Family Drops")]
 static void Check()
 {
  var state=UnityEngine.Random.state;
  try
  {
   var pick=typeof(CityForgeApp).GetMethod("RandomDistrictTreeId",BindingFlags.Static|BindingFlags.NonPublic);
   foreach(var family in FloraFamilies.Names)
   {
    string previous="";
    for(int i=0;i<1000;i++)
    {
     var id=(string)pick.Invoke(null,new object[]{family,previous});
     if(string.IsNullOrEmpty(id)||FloraFamilies.ForTree(id)!=family||id==previous||id=="vendor-hickory"||id=="narrow-street-tree") throw new Exception("Invalid family drop: "+family+" / "+id);
     previous=id;
    }
   }
   Debug.Log("FLORA FAMILY QA PASS: 3000 picks/rerolls; no cross-family trees or retired entries.");
  }
  finally {UnityEngine.Random.state=state;}
 }
}
#endif

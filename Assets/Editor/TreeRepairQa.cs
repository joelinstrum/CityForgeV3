#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using CityForgeV3.UI;
public static class TreeRepairQa
{
 [MenuItem("City Forge/Flora/Repairs/Stones Mostly Large")]static void SL()=>Open("stones-mostly-large");
 [MenuItem("City Forge/Flora/Repairs/Stones Mixed")]static void SM()=>Open("stones-large-and-small");
 [MenuItem("City Forge/Flora/Repairs/Stones Small")]static void SS()=>Open("stones-mostly-small");
 static void Open(string id){if(EditorApplication.isPlaying)Object.FindFirstObjectByType<CityForgeApp>()?.OpenTreeRepairQa(id);}
 [MenuItem("City Forge/Flora/Repairs/Date Palm")]static void A()=>Open("date-palm");
 [MenuItem("City Forge/Flora/Repairs/Streettree3d")]static void B()=>Open("street-tree-3d");
 [MenuItem("City Forge/Flora/Repairs/Eucalyptus A")]static void C()=>Open("eucalyptus-robusta-a");
 [MenuItem("City Forge/Flora/Repairs/Eucalyptus B")]static void D()=>Open("eucalyptus-robusta-b");
 [MenuItem("City Forge/Flora/Repairs/Silver Maple A")]static void E()=>Open("silver-maple-a");
 [MenuItem("City Forge/Flora/Repairs/Angel Oak 3D")]static void F()=>Open("angel-oak-spanish-moss");
 [MenuItem("City Forge/Flora/Repairs/Red Maple")]static void G()=>Open("vendor-red-maple");
 [MenuItem("City Forge/Flora/Repairs/Cilician Fir")]static void H()=>Open("cilician-fir");
 [MenuItem("City Forge/Flora/Repairs/Snowy Fraser Fir")]static void I()=>Open("fraser-fir-snowy");
 [MenuItem("City Forge/Flora/Repairs/Classic Balsam Fir")]static void J()=>Open("vendor-balsam-fir-classic");
 [MenuItem("City Forge/Flora/Repairs/Hickory")]static void K()=>Open("vendor-hickory");
 [MenuItem("City Forge/Flora/Repairs/Willow")]static void L()=>Open("vendor-willow");
 [MenuItem("City Forge/Flora/Repairs/Cypress Oak")]static void M()=>Open("vendor-cypress-oak");
 [MenuItem("City Forge/Flora/Repairs/Oregon Ash")]static void N()=>Open("vendor-oregon-ash");
 [MenuItem("City Forge/Flora/Repairs/Wide Oregon Ash")]static void O()=>Open("vendor-oregon-ash-wide");
}
#endif

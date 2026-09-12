using UnityEngine;
namespace CityForgeV3.World
{
 public static class FloraTreeRepairs
 {
  public const string Root="CityForgeV3/Flora/TreeRepairsV01/";
  public static string Identity(string name){foreach(var s in new[]{"-spring","-summer","-autumn","-winter"})if(name!=null&&name.EndsWith(s))return name.Substring(0,name.Length-s.Length);return name??"";}
  public static string BillboardPath(string id,SeasonPreset season)=>id=="cilician-fir"||id=="fraser-fir-snowy"||id=="vendor-balsam-fir-classic"||id=="street-tree-3d"?Root+id+"-"+season.ToString().ToLowerInvariant():null;
  public static float PixelsPerUnit(string id)=>id switch
  {
   "eucalyptus-robusta-a"=>38.6900000f,
   "eucalyptus-robusta-b"=>34.0742857f,
   "vendor-hickory"=>64.0000000f,
   "vendor-willow"=>45.7142857f,
   "vendor-cypress-oak"=>45.7142857f,
   "vendor-oregon-ash"=>45.7142857f,
   "vendor-oregon-ash-wide"=>45.7142857f,
   "vendor-balsam-fir-classic"=>46.5625000f,
   "cilician-fir"=>67.0617284f,
   "fraser-fir-snowy"=>112.7246094f,
   "street-tree-3d"=>59.8361446f,
   _=>0f
  };
  public static bool TryPivot(string texture,out Vector2 pivot)
  {
   var id=Identity(texture);
   switch(id){
    case "vendor-balsam-fir-classic":pivot=new Vector2(0.50247687f,0.12109086f);return true;
    case "cilician-fir":pivot=new Vector2(0.50316471f,0.16080786f);return true;
    case "fraser-fir-snowy":pivot=new Vector2(0.51104259f,0.18089716f);return true;
    case "street-tree-3d":pivot=new Vector2(0.49467447f,0.12759677f);return true;
    case "angel-oak-spanish-moss":pivot=new Vector2(480f/1024f,310f/1024f);return true;
    case "date-palm":pivot=new Vector2(0.50000000f,0.03723404f);return true;
    case "eucalyptus-robusta-a":pivot=new Vector2(0.48958333f,0.05598958f);return true;
    case "eucalyptus-robusta-b":pivot=new Vector2(0.50000000f,0.11328125f);return true;
    case "silver-maple-a":pivot=new Vector2(0.52213542f,0.12109375f);return true;
    case "vendor-red-maple":
     pivot=texture.EndsWith("autumn")?new Vector2(.482f,.239f):texture.EndsWith("winter")?new Vector2(.524f,.239f):new Vector2(.487f,.229f);return true;
    case "vendor-cypress-oak":
     pivot=texture.EndsWith("autumn")?new Vector2(.51f,.164f):texture.EndsWith("winter")?new Vector2(.51f,.171f):new Vector2(.512f,.133f);return true;
    default:pivot=default;return false;
   }
  }
  public static void Apply(SpriteRenderer sprite,string id)
  {
   if(sprite==null)return;
   var block=new MaterialPropertyBlock();sprite.GetPropertyBlock(block);block.SetFloat("_FloraOpacity",id=="street-tree-3d"?2.4f:1f);block.SetFloat("_FloraSaturation",id=="vendor-willow"?1.4f:1f);
   // Round only the bottom seven texture pixels of Eucalyptus B's trunk.
   // Neutral on every other tree; source textures stay untouched.
   block.SetVector("_FloraBaseEllipse",id=="eucalyptus-robusta-b"?new Vector4(367f/768f,87f/768f,16f/768f,7f/768f):Vector4.zero);
   sprite.SetPropertyBlock(block);
  }
 }
}

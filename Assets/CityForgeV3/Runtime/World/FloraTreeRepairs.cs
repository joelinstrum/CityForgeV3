using UnityEngine;
namespace CityForgeV3.World
{
 public static class FloraTreeRepairs
 {
  public const string Root="CityForgeV3/Flora/TreeRepairsV01/";
  public const string RealisticCilicianRoot="CityForgeV3/Flora/CilicianFirRealisticV01/";
  public const string RealisticLondonPlaneRoot="CityForgeV3/Flora/LondonPlaneRealisticV01/";
  public const string RealisticRedMapleRoot="CityForgeV3/Flora/RedMapleRealisticV01/";
  public const string RealisticSilverMapleRoot="CityForgeV3/Flora/SilverMapleRealisticV01/";
  public const string RealisticWillowRoot="CityForgeV3/Flora/WillowRealisticV01/";
  public const string PhotographicDeciduousRoot="CityForgeV3/Flora/PhotographicDeciduousV01/";
  public const string ElmTrueAngleRoot="CityForgeV3/Flora/ElmTrueAngleV01/";
  public const string SpanishMossTrueAngleRoot="CityForgeV3/Flora/SpanishMossTrueAngleV01/";
  public const string BaldCypressMossRoot="CityForgeV3/Flora/BaldCypressMossV01/";
  public const string BaldCypressMossBRoot="CityForgeV3/Flora/BaldCypressMossV02/";
  public const string MediumConifersRoot="CityForgeV3/Flora/MediumConifersV01/";
  public const string PhotographicPalmsRoot="CityForgeV3/Flora/PhotographicPalmsV01/";
  public static string Identity(string name){foreach(var s in new[]{"-spring","-summer","-autumn","-winter"})if(name!=null&&name.EndsWith(s))return name.Substring(0,name.Length-s.Length);return name??"";}
  public static string BillboardPath(string id,SeasonPreset season)=>
   id is "la-fan-palm-a-medium" or "la-fan-palm-b-medium"
    ? PhotographicPalmsRoot+id.Substring(0,id.Length-"-medium".Length)
    :id is "date-palm-tall" or "date-palm-short" or "la-fan-palm-a" or "la-fan-palm-b"
    ? PhotographicPalmsRoot+id
    :id is "medium-balsam-fir" or "medium-fraser-fir" or "medium-blue-spruce"
    ? MediumConifersRoot+id+(season==SeasonPreset.Winter?"-snowy":"-snowfree")
    :id=="bald-cypress-moss-b" ? BaldCypressMossBRoot+id+"-"+season.ToString().ToLowerInvariant()
    :id=="bald-cypress-moss" ? BaldCypressMossRoot+id+"-"+season.ToString().ToLowerInvariant()
    :id=="angel-oak-spanish-moss" ? SpanishMossTrueAngleRoot+id
    :id=="american-elm" ? ElmTrueAngleRoot+id+"-"+
      (season==SeasonPreset.Spring?"summer":season.ToString().ToLowerInvariant())
    :id is "mature-oak" or "shagbark-hickory"
    ? PhotographicDeciduousRoot+id+"-"+season.ToString().ToLowerInvariant()
    :id=="cilician-fir"?RealisticCilicianRoot+id+"-"+season.ToString().ToLowerInvariant()
    :id=="london-plane-a"||(id=="london-plane-b"&&season!=SeasonPreset.Winter)
     ?RealisticLondonPlaneRoot+id+"-"+season.ToString().ToLowerInvariant()
    :id=="fraser-fir-snowy"||id=="vendor-balsam-fir-classic"||id=="street-tree-3d"
     ?Root+id+"-"+season.ToString().ToLowerInvariant():null;
  public static float PixelsPerUnit(string id)=>id switch
  {
   "eucalyptus-robusta-a"=>38.6900000f,
   "eucalyptus-robusta-b"=>34.0742857f,
   "vendor-hickory"=>64.0000000f,
   "vendor-willow"=>104f,
   "mature-oak"=>100f,
   // The 1312x1199 replacement keeps the old elm's roughly 16m height.
   "american-elm"=>72f,
   "angel-oak-spanish-moss"=>80f,
   "shagbark-hickory"=>98f,
   "bald-cypress-moss"=>82f,
   "bald-cypress-moss-b"=>82f,
   "medium-balsam-fir"=>128f,
   "medium-fraser-fir"=>128f,
   "medium-blue-spruce"=>120f,
   "date-palm-tall"=>120f,
   "date-palm-short"=>180f,
   "la-fan-palm-a"=>70f,
   "la-fan-palm-b"=>80f,
   "la-fan-palm-a-medium"=>70f*4f/3f,
   "la-fan-palm-b-medium"=>80f*4f/3f,
   "vendor-cypress-oak"=>45.7142857f,
   "vendor-oregon-ash"=>45.7142857f,
   "vendor-oregon-ash-wide"=>45.7142857f,
   "vendor-balsam-fir-classic"=>46.5625000f,
   // The new 1024×1536 realistic fir retains the prior approximately 14.5m
   // physical height, despite its taller canvas.
   "cilician-fir"=>105.0000000f,
   "london-plane-a"=>96.0000000f,
   "london-plane-b"=>96.0000000f,
   "vendor-red-maple"=>96f,
   "silver-maple-a"=>84f,
   "fraser-fir-snowy"=>112.7246094f,
   "street-tree-3d"=>59.8361446f,
   _=>0f
  };
  public static bool TryPivot(string texture,out Vector2 pivot)
  {
   var id=Identity(texture);
   switch(id){
    case "vendor-balsam-fir-classic":pivot=new Vector2(0.50247687f,0.12109086f);return true;
    case "cilician-fir":pivot=new Vector2(.5f,0f);return true;
    // Measured visible trunk foot, not canvas bottom or Plane B's padding.
    case "london-plane-a":pivot=new Vector2(.5f,
     (texture.EndsWith("winter") ? 18f : texture.EndsWith("autumn") ? 20f : 19f) / 1536f);return true;
    case "london-plane-b":pivot=new Vector2(.5f,.065f);return true;
    case "fraser-fir-snowy":pivot=new Vector2(0.51104259f,0.18089716f);return true;
    case "street-tree-3d":pivot=new Vector2(0.49467447f,0.12759677f);return true;
    case "angel-oak-spanish-moss":pivot=new Vector2(.5f,178f/1199f);return true;
    case "date-palm":pivot=new Vector2(0.50000000f,0.03723404f);return true;
    case "date-palm-tall":pivot=new Vector2(.5f,12f/1536f);return true;
    case "date-palm-short":pivot=new Vector2(.5f,41f/1536f);return true;
    case "la-fan-palm-a":pivot=new Vector2(.5f,24f/1536f);return true;
    case "la-fan-palm-b":pivot=new Vector2(.5f,31f/1536f);return true;
    case "eucalyptus-robusta-a":pivot=new Vector2(0.48958333f,0.05598958f);return true;
    case "eucalyptus-robusta-b":pivot=new Vector2(0.50000000f,0.11328125f);return true;
    // The visible root foot, measured in the approved 1536px seasonal cutouts.
    case "silver-maple-a":pivot=new Vector2(.5f,
     (texture.EndsWith("spring")?40f:texture.EndsWith("autumn")?36f:42f)/1536f);return true;
    case "vendor-willow":pivot=new Vector2(.5f,
     (texture.EndsWith("spring")?64f:texture.EndsWith("summer")?105f:
      texture.EndsWith("autumn")?133f:135f)/1536f);return true;
    case "mature-oak":pivot=new Vector2(.5f,
     (texture.EndsWith("spring")?146f:texture.EndsWith("winter")?135f:149f)/1536f);return true;
    case "american-elm":pivot=new Vector2(.5f,
     (texture.EndsWith("summer")?187f:texture.EndsWith("autumn")?151f:64f)/1199f);return true;
    case "shagbark-hickory":pivot=new Vector2(.5f,
     (texture.EndsWith("spring")?29f:texture.EndsWith("winter")?31f:33f)/1536f);return true;
    case "bald-cypress-moss":pivot=new Vector2(.5f,
     (texture.EndsWith("spring")?25f:texture.EndsWith("winter")?26f:27f)/1536f);return true;
    case "bald-cypress-moss-b":pivot=new Vector2(.5f,
     (texture.EndsWith("winter")?18f:17f)/1536f);return true;
    case "medium-balsam-fir-snowfree":pivot=new Vector2(.5f,14f/1536f);return true;
    case "medium-balsam-fir-snowy":pivot=new Vector2(.5f,15f/1536f);return true;
    case "medium-fraser-fir-snowfree":pivot=new Vector2(.5f,21f/1536f);return true;
    case "medium-fraser-fir-snowy":pivot=new Vector2(.5f,20f/1536f);return true;
    case "medium-blue-spruce-snowfree":pivot=new Vector2(.5f,6f/1536f);return true;
    case "medium-blue-spruce-snowy":pivot=new Vector2(.5f,7f/1536f);return true;
    case "vendor-red-maple":
     // Visible root foot measured from each approved 1024x1536 cutout.
     pivot=new Vector2(.5f,(texture.EndsWith("autumn")?40f:39f)/1536f);return true;
    case "vendor-cypress-oak":
     pivot=texture.EndsWith("autumn")?new Vector2(.51f,.164f):texture.EndsWith("winter")?new Vector2(.51f,.171f):new Vector2(.512f,.133f);return true;
    default:pivot=default;return false;
   }
  }
  public static void Apply(SpriteRenderer sprite,string id)
  {
   if(sprite==null)return;
   var block=new MaterialPropertyBlock();sprite.GetPropertyBlock(block);block.SetFloat("_FloraOpacity",id=="street-tree-3d"?2.4f:1f);block.SetFloat("_FloraSaturation",1f);
   // Round only the bottom seven texture pixels of Eucalyptus B's trunk.
   // Neutral on every other tree; source textures stay untouched.
   block.SetVector("_FloraBaseEllipse",id=="eucalyptus-robusta-b"?new Vector4(367f/768f,87f/768f,16f/768f,7f/768f):Vector4.zero);
   sprite.SetPropertyBlock(block);
  }
 }
}

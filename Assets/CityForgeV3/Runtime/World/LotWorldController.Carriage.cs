using UnityEngine;
using UnityEngine.Rendering;
namespace CityForgeV3.World
{
    public sealed partial class LotWorldController
    {
        public const string CarriagePropId="antique-carriage-v01";
        public const string CarriageResourcePath="CityForgeV3/Vehicles/CarriageV01/Carriage_Rolling_Wheels_v01";
        Transform CreateCarriagePresentation(string name,float alpha,string resourcePath=CarriageResourcePath,HorseWagonDefinition vehicle=null)
        {
            vehicle??=HorseWagonDefinition.Carriage;
            var prefab=Resources.Load<GameObject>(resourcePath);
            if(prefab==null)return null;
            var root=new GameObject(name).transform;
            var model=Instantiate(prefab,root,false);
            var renderers=model.GetComponentsInChildren<Renderer>();
            var bounds=new Bounds();var first=true;
            foreach(var r in renderers){if(first){bounds=r.bounds;first=false;}else bounds.Encapsulate(r.bounds);}
            if(!first && bounds.size.y>.001f)model.transform.localScale*=vehicle.Height/bounds.size.y;
            first=true;
            foreach(var r in renderers){if(first){bounds=r.bounds;first=false;}else bounds.Encapsulate(r.bounds);}
            model.transform.localPosition-=Vector3.up*bounds.min.y;
            var material=new Material(Shader.Find("Standard"));
            material.name=vehicle.DisplayName+" Original Coat";
            material.mainTexture=Resources.Load<Texture2D>(vehicle.TexturePath);
            material.color=new Color(vehicle.Tint,vehicle.Tint,vehicle.Tint,alpha);
            material.SetFloat("_Metallic",0f);material.SetFloat("_Glossiness",.18f);
            if(alpha<1f)
            {
                material.SetFloat("_Mode",3);material.SetInt("_SrcBlend",(int)BlendMode.SrcAlpha);
                material.SetInt("_DstBlend",(int)BlendMode.OneMinusSrcAlpha);material.SetInt("_ZWrite",0);
                material.EnableKeyword("_ALPHAPREMULTIPLY_ON");material.renderQueue=3000;
            }
            root.gameObject.AddComponent<CharacterShadowMaterialOwner>().Add(material);
            foreach(var r in renderers){r.sharedMaterial=material;r.shadowCastingMode=ShadowCastingMode.On;r.receiveShadows=true;}
            if(vehicle==HorseWagonDefinition.Forestry)
            {
                foreach(var kind in new[]{"Bark","Endgrain"})
                {
                    var cargoMaterial=new Material(Shader.Find("Standard"));
                    cargoMaterial.name="Forestry log "+kind;
                    cargoMaterial.mainTexture=Resources.Load<Texture2D>("CityForgeV3/Vehicles/ForestryWagonV02/Forestry_"+kind);
                    cargoMaterial.color=Color.white;cargoMaterial.SetFloat("_Glossiness",.08f);
                    root.GetComponent<CharacterShadowMaterialOwner>().Add(cargoMaterial);
                    foreach(var r in renderers)if(r.name.StartsWith("Cargo_Log_"+kind))r.sharedMaterial=cargoMaterial;
                }
            }
            var foreMesh=System.Array.Find(model.GetComponentsInChildren<Transform>(),t=>t.name=="Carriage_Forecarriage");
            if(foreMesh!=null)
            {
                var steering=new GameObject("Forecarriage Steering").transform;steering.SetParent(root,false);
                steering.localPosition=new Vector3(0,0,vehicle.FrontAxleOffset);
                foreach(var t in model.GetComponentsInChildren<Transform>())
                    if(t==foreMesh||t.name.StartsWith("Carriage_Front_"))t.SetParent(steering,true);
            }
            root.gameObject.AddComponent<CarriageWheelController>();
            return root;
        }
    }
}

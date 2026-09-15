using System.Linq;
using UnityEngine;
namespace CityForgeV3.World
{
    // A non-damaging signal: lift the musket pose and send the effect upward.
    public sealed class MarksmanWarningShot:MonoBehaviour
    {
        float seconds;int lastShots=-1;bool running;
        Transform arm,forearm,hand,rifle;
        LineRenderer flash;
        AudioSource sound;AudioClip report;
        void Start()
        {
            arm=GetComponentsInChildren<Transform>().FirstOrDefault(t=>t.name=="R_Upperarm"||t.name.EndsWith("RightArm")||t.name.EndsWith("RightUpperArm"));
            forearm=GetComponentsInChildren<Transform>().FirstOrDefault(t=>t.name=="R_Forearm");
            hand=GetComponentsInChildren<Transform>().FirstOrDefault(t=>t.name=="R_Hand");
            rifle=new GameObject("Warning Musket").transform;rifle.SetParent(hand!=null?hand:transform,false);
            rifle.localPosition=Vector3.zero;
            var owner=gameObject.AddComponent<CharacterShadowMaterialOwner>();
            var wood=new Material(Shader.Find("Standard")){color=new Color(.17f,.085f,.035f)};owner.Add(wood);
            var iron=new Material(Shader.Find("Standard")){color=new Color(.16f,.17f,.17f)};iron.SetFloat("_Metallic",.65f);owner.Add(iron);
            void Piece(PrimitiveType type,Vector3 pos,Vector3 scale,Material material,bool barrel=false)
            {
                var part=GameObject.CreatePrimitive(type);Destroy(part.GetComponent<Collider>());part.transform.SetParent(rifle,false);part.transform.localPosition=pos;part.transform.localScale=scale;
                if(barrel)part.transform.localRotation=Quaternion.Euler(90,0,0);part.GetComponent<Renderer>().sharedMaterial=material;
            }
            Piece(PrimitiveType.Cube,new Vector3(0,-.018f,-.25f),new Vector3(.065f,.10f,.65f),wood);
            Piece(PrimitiveType.Cylinder,new Vector3(0,.025f,.36f),new Vector3(.045f,.55f,.045f),iron,true);
            var go=new GameObject("Warning shot upward");go.transform.SetParent(transform,false);flash=go.AddComponent<LineRenderer>();flash.useWorldSpace=true;flash.positionCount=2;flash.startWidth=.07f;flash.endWidth=.005f;
            var mat=new Material(Shader.Find("Sprites/Default"));mat.color=new Color(1,.72f,.25f);flash.sharedMaterial=mat;gameObject.AddComponent<CharacterShadowMaterialOwner>().Add(mat);
            flash.SetPosition(0,new Vector3(.25f,1.65f,.4f));flash.SetPosition(1,new Vector3(.25f,3.8f,.6f));flash.enabled=false;
            sound=gameObject.AddComponent<AudioSource>();sound.spatialBlend=1;sound.minDistance=8;sound.maxDistance=90;sound.volume=.3f;
            report=AudioClip.Create("Musket warning",11025,1,22050,false);var data=new float[11025];uint seed=37;
            for(int i=0;i<data.Length;i++){seed=1664525*seed+1013904223;data[i]=((seed>>8)/(float)0xffffff*2-1)*Mathf.Exp(-i/1000f);}
            report.SetData(data,0);
        }
        public void Present(float remaining,int shots,bool moving)
        {
            seconds=remaining;running=moving;
            if(lastShots>=0&&shots>lastShots&&sound!=null&&moving)sound.PlayOneShot(report);
            lastShots=shots;
        }
        void LateUpdate()
        {
            if(flash!=null)flash.enabled=seconds>1.72f&&seconds<1.9f;
            if(seconds>0&&arm!=null&&forearm!=null&&hand!=null)
            {
                arm.rotation=Quaternion.FromToRotation(forearm.position-arm.position,transform.TransformDirection(new Vector3(.2f,.55f,.8f)))*arm.rotation;
                forearm.rotation=Quaternion.FromToRotation(hand.position-forearm.position,transform.TransformDirection(new Vector3(.05f,.95f,.25f)))*forearm.rotation;
            }
            if(rifle!=null)
            {
                var direction=transform.TransformDirection(new Vector3(.08f,1,.18f)).normalized;
                rifle.rotation=Quaternion.LookRotation(direction);
                // Cancel imported armature scale: the musket is authored in meters.
                var scale=rifle.parent.lossyScale;rifle.localScale=new Vector3(1/scale.x,1/scale.y,1/scale.z);
                if(flash!=null){var muzzle=rifle.TransformPoint(new Vector3(0,.025f,.92f));flash.SetPosition(0,muzzle);flash.SetPosition(1,muzzle+direction*1.2f);}
            }
        }
        void OnDestroy(){if(report!=null)Destroy(report);}
    }
}

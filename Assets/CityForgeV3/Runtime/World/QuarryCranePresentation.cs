using System.Collections.Generic;
using UnityEngine;

namespace CityForgeV3.World
{
    // Evaluated from saved quarry progress, so pause, reload and rotation preserve the load.
    public sealed class QuarryCranePresentation : MonoBehaviour
    {
        public const string BaseResource = "CityForgeV3/Industry/StoneQuarryCraneV02/QuarryBase";
        public static readonly Vector3 Pivot = new(1.6f, 0, 1.75f);
        public static readonly Vector3 Pickup = new(1.6f, 1.5f, -1.15f);
        public static readonly Vector3 WagonBed = new(5.65f, .28f, 2.45f);
        const float BoomHeight = 4.25f;
        const float CarryHeight = 2.85f;
        Transform swivel, trolley, hook, load;
        LineRenderer cable;
        readonly List<LineRenderer> slings = new();
        IReadOnlyList<Transform> cargo;
        public Vector3 LoadPosition => load.position;
        public Vector3 HookPosition => hook.position;
        public bool Carrying => load.gameObject.activeSelf;
        public float BoomYaw => swivel.localEulerAngles.y;

        public void Initialize(Transform stone, IReadOnlyList<Transform> wagonCargo)
        {
            load = stone; cargo = wagonCargo;
            var owner = GetComponent<QuarryMaterials>();
            var wood = Material(owner, new Color(.22f, .155f, .09f));
            var iron = Material(owner, new Color(.17f, .18f, .18f));
            var rope = Material(owner, new Color(.43f, .35f, .22f));
            var root = new GameObject("Quarry swivel crane").transform; root.SetParent(transform, false);
            root.localPosition = Pivot;
            Beam(root, "Fixed oak mast", new Vector3(0, .5f, 0), new Vector3(0, 4.7f, 0), .26f, wood);
            for (int i = -1; i <= 1; i += 2)
                Beam(root, "Mast foot brace", new Vector3(i * .95f, .35f, .25f), new Vector3(0, 2.1f, 0), .17f, wood);
            swivel = new GameObject("Rotating jib").transform; swivel.SetParent(root, false);
            Beam(swivel, "Oak lifting boom", new Vector3(0, BoomHeight, -.4f), new Vector3(0, BoomHeight, 5.3f), .22f, wood);
            Beam(swivel, "Jib diagonal brace", new Vector3(0, 2.45f, 0), new Vector3(0, BoomHeight, 3.2f), .16f, wood);
            Beam(swivel, "Upper iron stay", new Vector3(0, 4.65f, 0), new Vector3(0, BoomHeight, 5.15f), .045f, iron);
            for (int i=0; i<2; i++)
            {
                var collar=GameObject.CreatePrimitive(PrimitiveType.Cylinder);collar.name="Swivel iron collar";
                collar.transform.SetParent(root,false);collar.transform.localPosition=new Vector3(0,2.4f+i*1.85f,0);
                collar.transform.localScale=new Vector3(.38f,.1f,.38f);collar.GetComponent<Renderer>().sharedMaterial=iron;
                Destroy(collar.GetComponent<Collider>());
            }
            trolley = new GameObject("Rope trolley").transform; trolley.SetParent(swivel, false);
            var pulley=GameObject.CreatePrimitive(PrimitiveType.Cylinder);pulley.name="Hoist pulley";
            pulley.transform.SetParent(trolley,false);pulley.transform.localRotation=Quaternion.Euler(0,0,90);
            pulley.transform.localScale=new Vector3(.27f,.065f,.27f);pulley.GetComponent<Renderer>().sharedMaterial=iron;
            Destroy(pulley.GetComponent<Collider>());
            hook = new GameObject("Lifting hook").transform;hook.SetParent(transform,false);
            var eye=GameObject.CreatePrimitive(PrimitiveType.Sphere);eye.name="Iron lifting eye";eye.transform.SetParent(hook,false);
            eye.transform.localScale=new Vector3(.13f,.20f,.13f);eye.GetComponent<Renderer>().sharedMaterial=iron;Destroy(eye.GetComponent<Collider>());
            cable=Line("Hoist rope",rope,.045f);
            for(int i=0;i<4;i++)slings.Add(Line("Stone lifting sling",rope,.027f));
        }
        public void Present(DistrictStoneSite site)
        {
            Vector3 position;
            bool carrying=site.Phase=="loading";
            if(carrying)
            {
                var destination=transform.InverseTransformPoint(cargo[Mathf.Clamp(site.CartBlocks,0,cargo.Count-1)].position);
                position=LoadPath(Pickup,destination,Mathf.Clamp01(site.Elapsed/site.Script.loadingSeconds));
            }
            else if(site.Phase=="delivering"||site.Phase=="unloading"||site.Phase=="returning")position=Pickup;
            else if(site.Phase=="full" && Vector3.Distance(transform.InverseTransformPoint(cargo[0].position),Pivot)>8)position=Pickup;
            else if(site.BlocksLoaded>0)
            {
                int previous=site.CartBlocks>0?site.CartBlocks-1:Mathf.Clamp(site.Script.cartCapacity-1,0,cargo.Count-1);
                var destination=transform.InverseTransformPoint(cargo[previous].position);
                float duration=site.Phase=="full"?site.Script.fullCartSeconds:Mathf.Min(8,site.Script.miningSeconds);
                // The full-cart phase already returned the hook before the next mining cycle.
                float t=site.Phase=="mining"&&site.CartBlocks==0?1:Mathf.Clamp01(site.Elapsed/duration);
                position=ReturnPath(destination,Pickup,t);
            }
            else position=Pickup;
            var delta=position-Pivot;
            swivel.localRotation=Quaternion.Euler(0,Mathf.Atan2(delta.x,delta.z)*Mathf.Rad2Deg,0);
            trolley.localPosition=new Vector3(0,BoomHeight,new Vector2(delta.x,delta.z).magnitude);
            load.localPosition=position;
            load.gameObject.SetActive(carrying);
            hook.localPosition=position+Vector3.up*.65f;
            cable.SetPosition(0,trolley.position);cable.SetPosition(1,hook.position);
            for(int i=0;i<slings.Count;i++)
            {
                slings[i].gameObject.SetActive(carrying);
                slings[i].SetPosition(0,hook.position);
                slings[i].SetPosition(1,load.TransformPoint(new Vector3(i%2==0?-.5f:.5f,-.4f,i<2?-.5f:.5f)));
            }
        }
        public static Vector3 LoadPath(Vector3 pickup, Vector3 destination, float progress)
        {
            float t=Mathf.Clamp01(progress);
            var raisedPickup=new Vector3(pickup.x,CarryHeight,pickup.z);
            var raisedDestination=new Vector3(destination.x,CarryHeight,destination.z);
            if(t<.22f)return Vector3.Lerp(pickup,raisedPickup,Ease(t/.22f));
            if(t<.70f)return Swing(raisedPickup,raisedDestination,Ease((t-.22f)/.48f));
            if(t<.94f)return Vector3.Lerp(raisedDestination,destination,Ease((t-.70f)/.24f));
            return destination;
        }
        public static Vector3 ReturnPath(Vector3 destination,Vector3 pickup,float progress)
        {
            float t=Mathf.Clamp01(progress);
            var raisedDestination=new Vector3(destination.x,CarryHeight,destination.z);
            var raisedPickup=new Vector3(pickup.x,CarryHeight,pickup.z);
            if(t<.2f)return Vector3.Lerp(destination,raisedDestination,Ease(t/.2f));
            if(t<.8f)return Swing(raisedDestination,raisedPickup,Ease((t-.2f)/.6f));
            return Vector3.Lerp(raisedPickup,pickup,Ease((t-.8f)/.2f));
        }
        static float Ease(float t)=>Mathf.SmoothStep(0,1,t);
        static Vector3 Swing(Vector3 a,Vector3 b,float t)
        {
            var from=a-Pivot;var to=b-Pivot;
            float angle=Mathf.LerpAngle(Mathf.Atan2(from.x,from.z)*Mathf.Rad2Deg,Mathf.Atan2(to.x,to.z)*Mathf.Rad2Deg,t)*Mathf.Deg2Rad;
            float radius=Mathf.Lerp(new Vector2(from.x,from.z).magnitude,new Vector2(to.x,to.z).magnitude,t);
            return Pivot+new Vector3(Mathf.Sin(angle)*radius,Mathf.Lerp(a.y,b.y,t),Mathf.Cos(angle)*radius);
        }
        static Material Material(QuarryMaterials owner,Color color)
        {var material=new Material(Shader.Find("Standard")){color=color};material.SetFloat("_Glossiness",.12f);owner.Owned.Add(material);return material;}
        static void Beam(Transform parent,string name,Vector3 a,Vector3 b,float width,Material material)
        {
            var beam=GameObject.CreatePrimitive(PrimitiveType.Cube);beam.name=name;beam.transform.SetParent(parent,false);
            beam.transform.localPosition=(a+b)*.5f;beam.transform.localRotation=Quaternion.FromToRotation(Vector3.up,b-a);
            beam.transform.localScale=new Vector3(width,(b-a).magnitude,width);beam.GetComponent<Renderer>().sharedMaterial=material;Destroy(beam.GetComponent<Collider>());
        }
        LineRenderer Line(string name,Material material,float width)
        {
            var go=new GameObject(name);go.transform.SetParent(transform,false);var line=go.AddComponent<LineRenderer>();
            line.sharedMaterial=material;line.positionCount=2;line.widthMultiplier=width;line.useWorldSpace=true;
            line.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.On;return line;
        }
    }
}

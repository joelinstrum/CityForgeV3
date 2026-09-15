using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace CityForgeV3.World
{
    // A quarry-only derivative of the axeman rig. The source FBX and its axe are unchanged.
    public sealed class QuarryWorkerPresentation : MonoBehaviour
    {
        readonly Dictionary<Transform, Quaternion> rest = new();
        Transform leftUpper, leftForearm, leftHand, rightUpper, rightForearm, rightHand, spine, pick;
        Mesh headMesh;
        float phase, offset;
        bool working;
        public float CyclePhase => Mathf.Repeat(phase + offset, 1);
        public Vector3 PickHeadPosition => pick.TransformPoint(new Vector3(0, .7f, .45f));
        public bool Working => working;

        public void Initialize(float stagger)
        {
            offset = stagger;
            var sourceAnimator = GetComponent<ThreeDimensionalCharacterAnimator>();
            if (sourceAnimator != null) Destroy(sourceAnimator);
            var animator = GetComponentInChildren<Animator>();
            animator.enabled = false;
            var idle = Resources.LoadAll<AnimationClip>(DistrictWorldController.AxemanResource)
                .First(c => c.name.Contains("idle"));
            idle.SampleAnimation(animator.gameObject, 0);
            var bones = GetComponentsInChildren<Transform>(true);
            Transform Bone(string name) => bones.First(t => t.name == name);
            foreach (var renderer in GetComponentsInChildren<Renderer>())
                if (renderer.name.Contains("Axeman_Axe")) renderer.enabled = false;
            leftUpper=Bone("L_Upperarm");leftForearm=Bone("L_Forearm");leftHand=Bone("L_Hand");
            rightUpper=Bone("R_Upperarm");rightForearm=Bone("R_Forearm");rightHand=Bone("R_Hand");spine=Bone("Spine01");
            foreach (var bone in new[]{spine,leftUpper,leftForearm,leftHand,rightUpper,rightForearm,rightHand})rest[bone]=bone.localRotation;
            var owner=GetComponent<CharacterShadowMaterialOwner>();
            var wood=new Material(Shader.Find("Standard")){color=new Color(.30f,.16f,.065f)};
            wood.SetFloat("_Glossiness",.16f);owner.Add(wood);
            var iron=new Material(Shader.Find("Standard")){color=new Color(.22f,.25f,.27f)};
            iron.SetFloat("_Metallic",.65f);iron.SetFloat("_Glossiness",.28f);owner.Add(iron);
            pick=new GameObject("Quarry pickaxe").transform;pick.SetParent(transform,false);
            var shaft=GameObject.CreatePrimitive(PrimitiveType.Cylinder);shaft.name="Ash wood handle";
            shaft.transform.SetParent(pick,false);shaft.transform.localPosition=new Vector3(0,.08f,0);
            shaft.transform.localScale=new Vector3(.055f,.64f,.055f);shaft.GetComponent<Renderer>().sharedMaterial=wood;
            Destroy(shaft.GetComponent<Collider>());
            var head=new GameObject("Forged double-point pick head");head.transform.SetParent(pick,false);
            headMesh=CreateHead();head.AddComponent<MeshFilter>().sharedMesh=headMesh;head.AddComponent<MeshRenderer>().sharedMaterial=iron;
            Pose(0);
        }
        public void SetWorking(bool value) => working=value;
        void Update() { if(working)phase+=Time.deltaTime/2.4f; }
        void LateUpdate() { if(pick!=null)Pose(working?CyclePhase:-1); }
        void Pose(float t)
        {
            foreach(var pair in rest)pair.Key.localRotation=pair.Value;
            // Draw back over the shoulder, lift, drive into the face, then recover.
            Vector3 grip;float angle,lean;
            if(t<0){grip=new Vector3(.18f,1.05f,.30f);angle=15;lean=0;}
            else if(t<.42f)
            {float u=Mathf.SmoothStep(0,1,t/.42f);grip=Vector3.Lerp(new Vector3(.08f,1.02f,.55f),new Vector3(.24f,1.66f,-.12f),u);angle=Mathf.Lerp(62,-48,u);lean=Mathf.Lerp(12,-5,u);}
            else if(t<.57f)
            {float u=Mathf.SmoothStep(0,1,(t-.42f)/.15f);grip=Vector3.Lerp(new Vector3(.24f,1.66f,-.12f),new Vector3(.10f,1.80f,.06f),u);angle=Mathf.Lerp(-48,-18,u);lean=-5;}
            else if(t<.73f)
            {float u=Mathf.Pow((t-.57f)/.16f,2);grip=Vector3.Lerp(new Vector3(.10f,1.80f,.06f),new Vector3(.08f,.98f,.65f),u);angle=Mathf.Lerp(-18,67,u);lean=Mathf.Lerp(-5,17,u);}
            else
            {float u=Mathf.SmoothStep(0,1,(t-.73f)/.27f);grip=Vector3.Lerp(new Vector3(.08f,.98f,.65f),new Vector3(.08f,1.02f,.55f),u);angle=Mathf.Lerp(67,62,u);lean=Mathf.Lerp(17,12,u);}
            spine.rotation=Quaternion.AngleAxis(lean,transform.right)*spine.rotation;
            pick.localPosition=grip;pick.localRotation=Quaternion.Euler(angle,0,-8);
            SolveArm(rightUpper,rightForearm,rightHand,pick.TransformPoint(new Vector3(0,-.18f,0)),transform.right);
            SolveArm(leftUpper,leftForearm,leftHand,pick.TransformPoint(new Vector3(0,.16f,0)),-transform.right);
        }
        static void SolveArm(Transform upper,Transform forearm,Transform hand,Vector3 target,Vector3 outward)
        {
            var a=upper.position;float l1=Vector3.Distance(a,forearm.position),l2=Vector3.Distance(forearm.position,hand.position);
            var delta=target-a;float distance=Mathf.Clamp(delta.magnitude,Mathf.Abs(l1-l2)+.001f,l1+l2-.001f);
            var direction=delta.normalized;
            var bend=Vector3.ProjectOnPlane(outward+Vector3.down*.35f,direction).normalized;
            float along=(l1*l1-l2*l2+distance*distance)/(2*distance);
            var elbow=a+direction*along+bend*Mathf.Sqrt(Mathf.Max(0,l1*l1-along*along));
            upper.rotation=Quaternion.FromToRotation(forearm.position-a,elbow-a)*upper.rotation;
            forearm.rotation=Quaternion.FromToRotation(hand.position-forearm.position,a+direction*distance-forearm.position)*forearm.rotation;
        }
        static Mesh CreateHead()
        {
            var vertices=new List<Vector3>();var triangles=new List<int>();
            const int sections=9;
            for(int i=0;i<sections;i++)
            {
                float t=i/(float)(sections-1),z=Mathf.Lerp(-.48f,.48f,t),y=.74f-.22f*Mathf.Pow(Mathf.Abs(t*2-1),1.7f);
                float radius=Mathf.Lerp(.009f,.07f,Mathf.Pow(1-Mathf.Abs(t*2-1),.55f));
                vertices.Add(new Vector3(-radius,y-radius,z));vertices.Add(new Vector3(radius,y-radius,z));
                vertices.Add(new Vector3(radius,y+radius,z));vertices.Add(new Vector3(-radius,y+radius,z));
                if(i==0)continue;
                for(int j=0;j<4;j++){int a=(i-1)*4+j,b=(i-1)*4+(j+1)%4,c=i*4+j,d=i*4+(j+1)%4;triangles.AddRange(new[]{a,c,b,b,c,d});}
            }
            triangles.AddRange(new[]{0,1,2,0,2,3,32,34,33,32,35,34});
            var mesh=new Mesh{name="QuarryPickaxeHeadV01"};mesh.SetVertices(vertices);mesh.SetTriangles(triangles,0);mesh.RecalculateNormals();mesh.RecalculateBounds();return mesh;
        }
        void OnDestroy(){if(headMesh!=null)Destroy(headMesh);}
    }
}

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
namespace CityForgeV3.World
{
    public sealed partial class DistrictWorldController
    {
        Transform _riverSculptRoot;
        readonly List<LineRenderer> _riverSculptLines=new();
        public void HideRiverSculptPreview(){if(_riverSculptRoot!=null)_riverSculptRoot.gameObject.SetActive(false);}
        public void ShowRiverStrokePreview(IReadOnlyList<Vector2> stroke,Vector2 point,float width,bool erase)
        {
            if(_content==null)return;
            width=Mathf.Max(18,width);
            if(_riverSculptRoot==null)
            {
                _riverSculptLines.Clear();_riverSculptRoot=new GameObject("River shaping preview").transform;
                _riverSculptRoot.SetParent(_content,false);
                var material=new Material(Shader.Find("Sprites/Default"));
                for(int i=0;i<3;i++)
                {
                    var item=new GameObject("Untextured river guide");item.transform.SetParent(_riverSculptRoot,false);
                    var line=item.AddComponent<LineRenderer>();line.useWorldSpace=false;line.sharedMaterial=material;
                    line.shadowCastingMode=ShadowCastingMode.Off;line.receiveShadows=false;_riverSculptLines.Add(line);
                }
            }
            _riverSculptRoot.gameObject.SetActive(true);
            Vector2 Metres(Vector2 p)=>new((p.x-.5f)*_widthMeters,(p.y-.5f)*_depthMeters);
            for(int i=0;i<3;i++)
            {
                var line=_riverSculptLines[i];line.gameObject.SetActive(i==0 || stroke.Count>=2);
                line.startColor=line.endColor=erase?new Color(1,.25f,.15f):new Color(.1f,.8f,1);
                line.widthMultiplier=Mathf.Max(1.5f,width*.012f);line.loop=i==0;
                int count=i==0?32:stroke.Count;line.positionCount=count;
                for(int j=0;j<count;j++)
                {
                    Vector2 p;
                    if(i==0){float angle=j*Mathf.PI*2/count;p=Metres(point)+new Vector2(Mathf.Cos(angle),Mathf.Sin(angle))*width*.5f;}
                    else
                    {
                        p=Metres(stroke[j]);var tangent=(Metres(stroke[Mathf.Min(count-1,j+1)])-Metres(stroke[Mathf.Max(0,j-1)])).normalized;
                        p+=new Vector2(-tangent.y,tangent.x)*width*.5f*(i==1?1:-1);
                    }
                    // Rivers share a flat water level. No terrain/texture work in the gesture preview.
                    line.SetPosition(j,new Vector3(p.x,.7f,p.y));
                }
            }
        }
        void ClipRiversToDistrict()
        {
            var bounds=new Rect(-_widthMeters*.5f,-_depthMeters*.5f,_widthMeters,_depthMeters);
            foreach(var filter in _riverRoot.GetComponentsInChildren<MeshFilter>())RiverMeshUnion.ClipToRect(filter.sharedMesh,bounds);
        }
    }
}

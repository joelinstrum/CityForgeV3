#ifndef CITYFORGE_PIKE_DIRT
#define CITYFORGE_PIKE_DIRT
// Keep generated PNGs intact. Calibrate their irregular silhouettes to a shared
// 8m carriageway on the 10m grid, with identical material at connecting ports.
sampler2D _DirtStraightTex;
float _DirtTopology; // -1 disabled, 0 straight, 1 corner, 2 T, 3 cross, 4 end
float PikeSegmentDistance(float2 p, float2 a, float2 b)
{
    float2 v=b-a;
    return length(p-a-v*saturate(dot(p-a,v)/dot(v,v)));
}
fixed4 RoadArtwork(float2 uv)
{
    fixed4 art=tex2D(_MainTex,uv);
    if (_DirtTopology < -0.5) return art;
    float d=abs(uv.x-.5);
    float2 sourceUV=uv;
    if (_DirtTopology > .5 && _DirtTopology < 1.5)
    {
        d=abs(length(uv-float2(1,1))-.5);
        sourceUV=float2(lerp(.08,.56,uv.x),lerp(0,.55,uv.y));
    }
    else if (_DirtTopology > 1.5 && _DirtTopology < 2.5)
    {
        d=min(abs(uv.y-.5),PikeSegmentDistance(uv,float2(.5,0),float2(.5,.5)));
        sourceUV=float2(lerp(.33,.67,uv.x),lerp(.45,.75,uv.y));
    }
    else if (_DirtTopology > 2.5 && _DirtTopology < 3.5)
    {
        d=min(abs(uv.x-.5),abs(uv.y-.5));
        sourceUV=lerp(.29,.71,uv);
    }
    else if (_DirtTopology > 3.5)
        d=PikeSegmentDistance(uv,float2(.5,.5),float2(.5,1));
    art=tex2D(_MainTex,sourceUV);
    float crossCoordinate=abs(uv.x-.5)<abs(uv.y-.5)?uv.x:uv.y;
    if (_DirtTopology < .5 || _DirtTopology > 3.5) crossCoordinate=uv.x;
    float alongCoordinate=abs(uv.x-.5)<abs(uv.y-.5)?uv.y:uv.x;
    if (_DirtTopology < .5 || _DirtTopology > 3.5) alongCoordinate=uv.y;
    fixed4 portColor=tex2D(_DirtStraightTex,float2(lerp(.2,.8,crossCoordinate),frac(alongCoordinate+.5)));
    float edge=min(min(uv.x,1-uv.x),min(uv.y,1-uv.y));
    art.rgb=lerp(portColor.rgb,art.rgb,smoothstep(0,.1,edge)*smoothstep(.98,1,art.a));
    art.a=1-smoothstep(.397,.403,d);
    return art;
}
#endif

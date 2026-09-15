#!/usr/bin/env python3
"""Headless reference simulator for the cargo-loading-v1 command/state contract."""
import argparse,json,math
from pathlib import Path
from lumber_loading import definition

def create(d):
    return dict(Loaded=0,Departing=False,Departed=False,Distance=0.0,
                Workers=[dict(Phase='pickup',Elapsed=-i*d['staggerSeconds'],Carrying=False) for i in range(d['workers'])])

def step(d,s,dt,travel,connected,length=100.0,boat=True):
    if not boat or s['Departed'] or not math.isfinite(dt) or dt<=0: return
    connected=connected and length>0
    remaining=dt
    while remaining>1e-5:
        tick=min(.05,remaining);remaining-=tick
        if s['Departing']:
            if not connected:return
            s['Distance']=min(length,s['Distance']+tick*d['departureSpeed'])
            s['Departed']=s['Distance']>=length
            if s['Departed']:return
            continue
        for w in s['Workers']:
            if w['Phase']=='idle':continue
            w['Elapsed']+=tick
            duration=d['pickupSeconds'] if w['Phase']=='pickup' else d['unloadSeconds'] if w['Phase']=='unload' else max(.1,travel)
            if w['Elapsed']<duration:continue
            w['Elapsed']-=duration
            if w['Phase']=='pickup':
                reserved=s['Loaded']+sum(x['Carrying'] for x in s['Workers'])
                if reserved<d['capacity']:w['Carrying']=True;w['Phase']='carry'
                else:w['Phase']='idle';w['Elapsed']=0
            elif w['Phase']=='carry':w['Phase']='unload'
            elif w['Phase']=='unload':
                s['Loaded']+=int(w['Carrying']);w['Carrying']=False;w['Phase']='return'
            elif w['Phase']=='return':w['Phase']='idle' if s['Loaded']>=d['capacity'] else 'pickup'
        if s['Loaded']>=d['capacity'] and all(w['Phase']=='idle' and not w['Carrying'] for w in s['Workers']) and connected:
            s['Departing']=True

def verify():
    for capacity in (1,12,13):
        d=definition(capacity);s=create(d)
        for i in range(4000):
            step(d,s,.1,2,False)
            assert s['Loaded']+sum(w['Carrying'] for w in s['Workers'])<=capacity
        assert s['Loaded']==capacity and not s['Departing']
        s=json.loads(json.dumps(s));step(d,s,100,2,True)
        assert s['Departed'] and s['Loaded']==capacity
    print('PASS capacity reservation, disconnected waiting, snapshot roundtrip, connected departure')

if __name__=='__main__':
    p=argparse.ArgumentParser(description=__doc__);p.add_argument('--definition',type=Path);p.add_argument('--connected',action='store_true');p.add_argument('--seconds',type=float,default=300);p.add_argument('--travel-seconds',type=float,default=7.27);p.add_argument('--verify',action='store_true');a=p.parse_args()
    if a.verify:verify()
    else:
        d=json.loads(a.definition.read_text()) if a.definition else definition();s=create(d)
        step(d,s,a.seconds,a.travel_seconds,a.connected)
        print(json.dumps(dict(status='departed' if s['Departed'] else 'sailing' if s['Departing'] else 'waiting_for_connected_river' if s['Loaded']==d['capacity'] else 'loading',state=s),indent=2))

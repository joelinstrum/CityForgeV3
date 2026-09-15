#!/usr/bin/env python3
"""Author a portable City Forge lot behavior. No Unity installation required.

python lumber_loading.py --capacity 20 --output my-dock.json
Copy the resulting JSON into the game's Mods/LotBehaviors folder, then Reload Mod Definitions.
This authors data consumed by the game; Python itself is not executed inside Unity.
"""
import argparse, json
from pathlib import Path

def definition(capacity=12):
    if not 1 <= capacity <= 1000:
        raise ValueError('capacity must be between 1 and 1000')
    return dict(id='lumber-loading-v01', displayName='Load lumber barge', kind='cargo-loading-v1',
                workerPrefabResourcePath='CityForgeV3/Characters/DockWorkerV01/DockWorkerV01',
                workers=2, capacity=capacity, pickupSeconds=2.0, unloadSeconds=2.0,
                walkSpeed=1.1, departureSpeed=2.0, staggerSeconds=1.5)

if __name__ == '__main__':
    parser=argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--capacity',type=int,default=12)
    parser.add_argument('--output',type=Path)
    args=parser.parse_args(); text=json.dumps(definition(args.capacity),indent=2)+'\n'
    if args.output:
        args.output.parent.mkdir(parents=True,exist_ok=True); args.output.write_text(text)
    else: print(text,end='')

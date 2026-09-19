# Bridge road transition V01

Unity 6000.1.12f1. Fixed stone bridge approaches taper from the 7.62-meter district road width to each model's calibrated entry width. The last two meters overlap the complete bridge deck and fade the road artwork from opaque to transparent using mesh vertex alpha, revealing the untouched source bridge texture beneath it.

The source bridge meshes and road textures are unchanged. Transition geometry belongs to the individual bridge presentation and is rebuilt from saved placement data. It adds no renderer beyond the existing approach renderer, performs no district scan or repaint, and does not change persistence or autosave behavior.

`FixedStoneBridgeQa.Run` verifies 16 approach vertices per fixed bridge, opaque and transparent transition endpoints, both calibrated widths, complete source bridge topology, placement fit, undo, and reload. The Metal transition shader compiled without errors. [Long bridge transition render](stone-long-transition.png).

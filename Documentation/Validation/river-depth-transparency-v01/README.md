# River depth transparency V01

Unity 6000.1.12f1. The river shader now samples the camera's opaque depth texture. Water retains at least 60% of its prior opacity over objects immediately beneath the surface, then returns smoothly to the authored opacity over 2.2 meters of view depth. The existing lateral bank-to-center opacity, water artwork, lighting, whitecaps, and deep-water darkening remain in place.

The district camera explicitly requests a depth texture. The fixed stone bridge graphics QA compiled the Metal shader without errors and rendered both complete bridge models over procedural rivers. [Long stone bridge](stone-long-depth.png) shows submerged pier masonry through the water while the moving surface remains visible.

The focused EditMode regression suite passed 93/93 checks. `FixedStoneBridgeQa.Run` also verifies the required camera depth mode, fixed bridge topology, chooser fit, construction, undo, reload, and dense bridge query fixture.

This adds one opaque depth texture for the district camera and one depth sample in each river-water fragment. The existing short batch render cannot measure GPU or full-frame cost; dense mixed-district GPU profiling remains open. No district scan, repaint, persistence change, or autosave was added.

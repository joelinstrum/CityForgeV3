# Mixed temperate forest clusters

The true-angle deciduous and mountain clusters previously chose every piece
from one atlas: all deciduous or all fir. Their separate groups remained
conspicuous in autumn. Existing saved cluster IDs now assemble one or two fir
pieces with deciduous pieces. Compact clusters have one fir among four trees;
large clusters have one or two among seven. The fir positions vary across the
three layouts and sit near the visible front or side. Fir art remains green in
autumn while deciduous pieces use the fall atlas. Tropical palm clusters and
individual harvestable fir records keep their existing identities.

This changes presentation only. Existing district saves need no regeneration
or write: a cluster remains one simulation, harvest, and selection handle.
The spatial batch now binds the two existing cached atlases on one mesh, using
a per-vertex selector for each tree piece. It does not add a renderer per tree
or a second batch per cluster. Local cluster and season changes still rebuild
only their affected spatial batch. The temporary district shadow-free mode
remains in place.

Isolated previews: [summer](summer.png) and [autumn](autumn.png). They show four
representative compact and large clusters in a flat district. Joe's open
Unity editor was not driven; the final variety and grounding need visual
review there.

The focused canopy, mixed-atlas batching, staged flora, and shadow-free tests
passed 25/25. An isolated profile using a saved 1,777-tree district measured
195 flora batch renderers and 45,651 flora mesh vertices both before and
after. Its shadow-free rebuild took 3.47 seconds before and 3.55 seconds on a
repeat after run; a first after run took 3.83 seconds. Unity allocated-memory
change was about 838 MB in both. These cold EditMode timings vary with asset
caching. Actual GPU draw calls, GPU texture cost, frame-time spikes, and
long-duration behavior were not measured. The broader EditMode run previously
had 92 failures and was not rerun.

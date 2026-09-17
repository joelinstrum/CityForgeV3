# Meadow patches validation — September 17, 2026

11/11 targeted terrain, surface-cache and lighting EditMode tests passed.
Shader imported and rendered successfully on Metal in isolated Regions Review.
Inspected actual Game-view screenshots before/after at the same camera for each
pair; soft irregular straw patches are visible at close range, existing base
remains dominant. Overview road remains above grass. Main Unity project untouched.

Testy / District 9 saved fixture: 9,951 flora records. No save written.
Each phase: 60 warmup frames, 180 measured frames, same scene and camera per pair.
Shader keyword disabled/enabled for cost comparison, strength 1. Original camera
size restored afterward, new blend enabled. Editor Game capture 2070x1008.

| View | Median off/on ms | p95 off/on ms | Max off/on ms | Draw calls off/on |
|---|---|---|---|---|
| Overview | 8.65 / 8.66 | 9.41 / 9.44 | 11.37 / 11.65 | 2943 / 2943 |
| Close | 2.58 / 2.63 | 2.99 / 3.04 | 3.44 / 10.10 | 108 / 108 |

ProfilerRecorder GC Allocated In Frame median 28,219 bytes in all four phases
(editor-wide; not an assertion of zero allocations). Frame times are wall-frame
CPU observations, not isolated GPU timing. One close/on spike remains visible;
this short sequential comparison cannot attribute it or prove long-term stability.
Other Unity session remained running. No new per-frame CPU callback was added.

Initial timing pass showed medians 8.14/8.16ms overview, 2.46/2.55ms close;
its screenshots switched camera too early, so those captures were discarded.
The retained run delays state changes until screenshot capture completes.

Patch fields are independent of camera, texture tiling and time by shader
construction. Full continuous-pan visual coverage, dedicated GPU profiling and
hilly/mountain district visual regression are not claimed by this check.

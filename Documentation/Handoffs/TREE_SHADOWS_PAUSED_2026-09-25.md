# Tree shadows — paused handoff (September 25, 2026)

## Start here in a new chat

Work in `/Users/joelinstrum/dev/CityForge - V3` on
`feature/time-and-light-adjustments`. Read `AGENTS.md` and
`Documentation/TIME_AND_LIGHT_ADJUSTMENTS_HANDOFF.md`. Joe has paused tree
shadows and is moving to other work. **Do not resume the shadow experiment
without a new request.** Do not merge PR #34, push, restart or drive his open
Unity editor, or alter his saved region. Preserve unrelated uncommitted files.
The latest shadow implementation commit at this handoff is `3e08152`.

## Current playable behavior

- Normal districts have district shadows disabled for the performance study.
  `CityForgeApp.RegionEditor.cs` opts only a district named `Shadow DIstrict`
  into the soft tree-shadow prototype.
- In that district, grouped trees (`ForestClusterCatalog.IsCluster`) retain the
  soft terrain-following, generic-canopy shadow. Individual trees, including
  standalone firs, have **no shadow**. Billboard trees do not cast realtime
  Unity shadows. The district directional light's realtime shadows are off.
- The grouped shadow moves with the time preset. Small sets of up to 64
  shadow-bearing handles update in one call. Larger sets update eight source
  shadows per frame, then rebuild flora batch cells one per frame. The visible
  rolling transition remains. Joe finds the blobs less convincing than the
  older detailed shadows and did not observe a useful live performance gain.
- This is a temporary visual/performance experiment, not an approved final
  shadow design. Do not describe it as instant or as a proven GPU improvement.

## Why the prior "instant" result did not match play

The first isolated test used a 20-tree hillside copy. The saved `Large Region
Test` / `Shadow DIstrict` (`city-022`) later contained **5,353 flora records**,
including 2,103 grouped and 3,250 individual trees. That exceeds the 64-handle
atomic-update limit. The Sun preset UI also serializes the approximately
1.8 MB district into in-memory undo history; this does **not** write progress
to disk. The existing staged path rebuilds flora batch cells after source
shadow updates. The original 20-tree EditMode result never established live
frame-time or dense-district performance.

## Work completed in this sequence

| Commit | Change |
| --- | --- |
| `3ccd443` | Temporarily disabled district shadows and deferred provisional hosted-Lot artwork during district load. |
| `6ffb797` | Added opt-in soft, terrain-following tree shadows in Shadow District. |
| `4365487` | Sized grouped-tree shadow footprints to the full cluster. |
| `37d2f3f` | Added one-call shadow refresh for small sets of at most 64 handles. |
| `3e08152` | Kept blob shadows for groups; removed them and their update slots from individual trees. |

Related work on this branch: Zoom 3 grass was brought closer to Zoom 2 while
retaining grain (`2303b07`, `6d25ecb`); close-zoom edge panning was expanded
and sped up (`0e31721`); temperate clusters gained fir variety (`97cc882`);
the mixed-atlas season crash was fixed (`2f4d6a5`). These are separate from
the paused shadow experiment.

## Dense-save measurement and its limits

An isolated Unity 6000.1.12f1 EditMode fixture used a read-only copy of the
saved district. One baseline and one group-only run reported:

| Measure | Shadows on all trees | Group-only shadows |
| --- | ---: | ---: |
| Shadow-bearing tree sources | 5,353 | 2,103 |
| Individual shadow sources | 3,250 | 0 |
| Flora shadow batch renderers | 392 | 174 |
| District rebuild CPU wall time | 2,987 ms | 2,687 ms |
| Staged time-change sync CPU wall time | 590 ms | 420 ms |
| Staged sync calls | 1,062 | 655 |

The focused `FarForestCanopyTests` suite passed 27/27 after the group-only
change. These single-run numbers are CPU observations in headless EditMode;
they do not establish Game view frame-time spikes, GPU cost, draw calls, or
long-duration stability. The earlier broader EditMode run had **92 failures**
and must not be called green. Detailed results and test scope are in
`Documentation/Validation/shadow-district-group-only-v01/README.md`.

## If Joe later resumes shadows

Use Joe's next request to set the scope of any experiment. The smallest
suggested direction was a fixed-direction silhouette derived from the actual
billboard alpha, generated once against terrain and faded through a shared
material value. It has **not** been implemented or visually approved. Validate
the 20-tree hillside and a representative dense district, including rendered
frame time and load cost, before rolling out. An alternate optimization is to
update only shadow-bearing batch cells instead of rebuilding every flora cell.
Neither idea is a commitment to add shadows now.

## Workspace guardrails

At handoff, unrelated Town Center material edits, river texture `.png.meta`
edits, and untracked `ForestCanopyObliqueSummerV04` artwork are present. Leave
them unstaged and intact unless Joe gives a task involving them. Joe tests in
the open Unity editor on the workspace; use isolated fixtures for headless or
destructive QA. Keep district/region persistence manual. Commit validated
work locally, with no push or merge.

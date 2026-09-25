# Mixed flora season batch fix

Validated September 25, 2026 with Unity 6000.1.12f1 in the isolated project
`/private/tmp/cityforge-shadow-pass-MBltYx`. The open editor and player saves
were not used.

During a budgeted summer-to-autumn update, mountain clusters kept the summer
fir root sprite while their deciduous pieces changed to the autumn atlas.
Old and new clusters in the same spatial batch therefore required three atlas
textures, exceeding the batch shader's two-texture limit. The new regression
test reproduces the reported `DistrictFloraBatches.RebuildCell` exception on
the previous code. With the fix, a cached autumn fir root sprite uses the
same fir texture but a separate batch identity. All fir-backed trees use it,
so a settled autumn cell still has one fir batch.

Focused EditMode results: 25/25 passed. The three-tree regression fixture had
one flora batch before the transition, two while seasons coexisted, and one
after the transition in both directions. A 384-cluster fixture used 16 batches
before and after the autumn transition, completed in 97 budgeted slices, and
reported 16.78 ms maximum / 15.23 ms p95 per slice, with zero managed bytes
reported by `GC.GetAllocatedBytesForCurrentThread`. Comparable winter and
deciduous runs were 16.65 / 15.24 ms and 16.58 / 15.29 ms respectively. These
EditMode CPU measurements do not establish Game view frame time, GPU cost, or
long-duration stability. During a transition, cells containing both seasons
temporarily need an additional flora draw batch; settled batch counts are
unchanged.

The broader EditMode run previously had 92 failures and was not rerun here.
This validation covers the focused canopy suite only.

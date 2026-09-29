# Dense Forest trees on hills

September 29, 2026. Dense Forest now fills steep ground around individually grounded trees. The original pass still places clumps on terrain that can support their shared baseline. Each original slope tree tries four nearby positions; a candidate is accepted only where the terrain footprint remains too steep for a clump and the planting masks exclude roads, river banks, lots, retained trees, clumps, and other generated trees. Light and Medium coverage do not run this pass. Existing placements are untouched until the user explicitly regenerates the forest.

This is an explicit district generation pass, not a frame or simulation tick scan. Its work is bounded by four attempts per accepted slope tree. The extra records are ordinary individual trees and enter the existing spatial flora batches and bounded seasonal update queue. A 4 m occupancy grid is allocated only for Dense Forest generation on districts with relief.

## Isolated Unity 6000.1.12f1 comparison

A 4×4 district with 55 m hills and Dense Forest, seed 313, was generated and built in an isolated project. Each result below is one headless run; editor asset caches and GC can affect timings. The baseline was commit `54772bc`; the tuned result uses four fill attempts.

| Measure | Baseline | Tuned |
| --- | ---: | ---: |
| Flora records | 8,636 | 14,517 |
| Original slope trees | 5,169 | 5,169 |
| Additional slope trees | 0 | 5,881 |
| Generation | 77.38 ms | 81.59 ms |
| Flora batches | 1,380 | 1,437 |
| Full district build | 5,838 ms | 8,116 ms |
| Mono heap growth during build | 24.4 MB | 26.6 MB |
| Seasonal update calls | 1,490 | 2,205 |
| Largest seasonal call | 20.82 ms | 24.50 ms |

The full district build is an explicit bulk operation and is the material cost: about 2.3 seconds longer for this large fixture. The additional flora batches are a draw-call proxy, not GPU draw-call measurements. Headless EditMode cannot establish actual frame-time spikes, GPU cost, or long-duration play stability; those remain for visual and performance review in the open editor. The validation fixture did not write a player save.

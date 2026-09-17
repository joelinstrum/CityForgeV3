# Meadow patch center refinement — September 17, 2026

User approved a modest increase in straw patch centers, retaining soft edges.
The dry blend is now `(.52*m + .10*m*m*m)*strength`, where `m` is the
unchanged smoothstep mask. Maximum blend increases from 52% to 62%; at
mask 0.25 it increases only from 13% to 13.15625%, and at mask 0.5 from
26% to 27.25%. Coverage, 28m/110m anchoring, green modulation, texture
coordinates, sample count and canonical artwork remain unchanged.

## Visual check

Isolated Regions Review, Testy / District 9 (9,951 flora in the saved fixture).
Before/after images compare the previous enabled treatment with the refined
enabled treatment, at matching camera transforms and 2070x1008 resolution.
These are direct live Unity camera RenderTexture captures, not Game-view UI
screenshots. Camera size and render target were restored after capture.
Animation and clouds were not frozen, so their differences are unrelated.
Close images show a restrained increase in straw centers with soft transitions;
overview keeps the base green dominant and the road visible.
Shader supported on Metal; ShaderUtil reported no errors for the active shader.
The project-specific capture helper is archived here as text.

## Short performance check

Existing review-only probe: 60 warmup and 180 measured frames per phase,
keyword disabled/enabled with strength 1, same scene/camera within each pair.

| View | Median off/on ms | p95 off/on ms | Max off/on ms | Draw calls off/on |
|---|---|---|---|---|
| Overview | 10.14 / 9.90 | 11.86 / 13.68 | 16.58 / 19.20 | 2649.1 / 2649.1 |
| Close | 6.01 / 5.73 | 7.38 / 6.35 | 15.62 / 14.18 | 152.1 / 152.1 |

Median editor-wide managed allocations: 204,856 bytes/frame in every phase.
This baseline differs from v01 (including draw counts and allocations); the
runs are not directly comparable. Lower on medians are noise, not evidence
of a speedup. Overview p95/max increased during this short sequential sample.
These wall-frame observations do not isolate GPU time or establish long-term
stability. No new texture fetch, pass, renderer, CPU update or district scan
was added to runtime code. Dedicated GPU, continuous pan and hill regression
checks remain outstanding. The original 11 passing EditMode tests were not
rerun for this shader arithmetic-only refinement.

## Scope

Only the small shader hunk was applied independently to the worktree and review
project. No whole implementation files were copied between projects. Existing
UI edits were verified unchanged by SHA-256. The other Unity project was not
modified or controlled. No district/region progress was saved. The isolated
review is left with patches enabled and its original overview zoom restored.

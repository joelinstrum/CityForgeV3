# Gentleman and Lady Strolling V01

This is a derivative of the archived colonial lady source recorded in
`colonial-ladies-chat-v01-source-manifest.json` and the archived gentleman
source recorded in `victorian-gentlemen-chat-v01-source-manifest.json`.
`Tools/build_couple_stroll_atlases.py` reads their existing rigged FBXs and
the source-derived dark gentleman texture without modifying any canonical
asset. The lady's red dress and the gentleman's charcoal coat remain intact.
The available gentleman model wears Victorian clothing, so the player-facing
name is period-neutral rather than claiming an eighteenth-century costume.

The Blender authoring scene moves both rigs along one four-metre path. Their
centres are separated by about half a metre, with a small fore-aft offset so
they nearly touch without becoming one silhouette in side views. They
walk the first two metres, stop and look around, walk the second two metres,
pause again, turn, return the full four metres, and turn to begin another loop.
The source walk actions drive their legs at different gait phases. The
gentleman's source look-around action and a small independent turn by the
lady supply the stopped beats. Their feet are held still during pauses and
turns. The 20-second loop is baked at 2 fps from eight directions using one
shared orthographic camera, 256 × 160 px frames, and 32 px/m calibration.
`Tools/pack_couple_stroll_atlases.py` packs 40 frames per facing into eight
2048 × 800 atlases. The original color atlases and thumbnail remain unchanged.

For optional per-placement recoloring, `Tools/build_couple_recolor_uv_masks.py`
classifies burgundy fabric and charcoal cloth in copies of the two source
textures. `Tools/build_couple_stroll_atlases.py` projects those UV masks through
the same rigs, camera, poses, and depth test as the color bake.
`Tools/pack_couple_recolor_masks.py` packs the rendered red dress and green
outfit channels into eight matching atlases. Skin, cream trim, and most hair
retain their original colors. The dark shoes share some charcoal texture and
may also pick up the gentleman's chosen color. No canonical source or base
flipbook pixels are changed.

The Automata catalog registers **Gentleman and Lady Strolling** as one 8 × 8 m
selectable and draggable group. The existing `AutomataClipPlayer` presents it
through one art renderer, a yellow group selection outline, rotation, and
per-placement time/season checkboxes. Default visibility is morning, noon,
and afternoon in all seasons. No individual-character runtime script,
district scan, full repaint, or automatic disk save is introduced.

The catalog advertises two generic garment slots and an optional mask root.
Each placed group stores its own two hex colors; empty strings mean original
art. The Automata inspector offers presets and custom hex values. One shared
sprite shader samples the aligned mask, recolors only its channels, and keeps
the original baked lighting variation. A MaterialPropertyBlock carries each
group's colors and current facing mask, so no per-placement material or
per-character script is created. Undo and the existing manual lot save carry
the colors. Other future clips can opt in with mask atlases and catalog labels.

The lowest opaque pixels reach about 1.14 m below the sprite pivot during the
southward loop. The clip therefore declares 1.35 m of visible artwork below
its ground anchor. `AutomataClipPlayer` slides the camera-facing art toward
the camera along the sightline, leaving its screen position and the selection
footprint unchanged while clearing the brick road's depth surface. One shared
sprite material draws all Automata art after road artwork and ground decals;
placements do not create per-group materials.

The eight atlases contain 13,107,200 RGBA pixels in total, equivalent to
about 52.4 MB before GPU compression and without mipmaps. The shared player
loads and creates 320 sprite slices on first use, then reuses them for every
placement; each additional visible group adds one art renderer. This
first-use texture and sprite cost is larger than the chatting clips. Dense
district CPU, allocation, draw-call, frame-time, and longer playback checks
remain necessary before claiming large-scale stability.

The additional mask atlases total 13,107,200 RGB pixels, about 39.3 MB if
expanded to RGB24 before GPU compression. They add eight shared texture loads
on first use and one mask fetch per visible art pixel. They create no extra
renderers or district queries; a facing change updates one property block.
The masks use no mipmaps and are imported with sRGB disabled. Any compressed
GPU size depends on the target platform and importer format.

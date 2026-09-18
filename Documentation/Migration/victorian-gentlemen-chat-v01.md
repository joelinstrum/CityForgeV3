# Victorian Gentlemen Chatting V01

The existing `VictorianGentlemanAnimatedV01.fbx` and `base-color-dark.png`
are the canonical inputs for this Automata derivative. The original 10K source
archive was copied byte-for-byte from the separate V3 checkout to
`SourceArchives/VictorianGentlemenChatV01/`; the archive, runtime FBX, and
source-derived dark texture have hashes in
`victorian-gentlemen-chat-v01-source-manifest.json`. Neither source asset was
edited. The outfit is Victorian, so the clip uses a Victorian label rather
than implying an eighteenth-century costume.

`Tools/build_victorian_gentlemen_chat_atlases.py` duplicates the single rig in
Blender authoring, faces the two men inward, and bakes distinct performances.
The charcoal-coated man uses the idle action with a brief hand gesture. The
brown-coated man uses the supplied fold-arms action, timed across the loop.
Both lower bodies are constrained to a planted standing pose, avoiding a
synchronized step. A shader-only derivative recolors dark neutral cloth warm
brown while retaining the original skin and shirt colors. The clip uses eight
camera directions and 16 frames at 2 fps. The 224 × 128 frames are packed by
`Tools/pack_victorian_gentlemen_chat_atlases.py` into eight 1792 × 256 group
atlases. Only the group atlases and thumbnail are loaded at runtime.

The Automata catalog registers **Victorian Gentlemen Chatting** as one 4 × 4 m
selectable and draggable group. The existing `AutomataClipPlayer` handles its
single renderer, yellow outline, rotation, per-placement schedule, undo, and
manual persistence. The default schedule is morning, noon, and afternoon in
all seasons. No per-character runtime scripts, new district scans, full
repaints, or autosave paths were added.

The two characters share a canonical model, differentiated by color and
performance. A future second sculpt could replace one authoring rig without
changing the runtime clip contract.

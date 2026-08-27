# Victorian Gentleman 10K V01

- Canonical intake archive: `Authoring/Props/VictorianGentleman10K/v01_source/victorian+gentleman+3d+model-10k.zip`.
- Extracted source: `Authoring/Props/VictorianGentleman10K/v01_source/raw/`, preserving the supplied FBX and five texture maps.
- Previous 99K runtime package: `Authoring/Props/VictorianGentleman10K/v01_previous_runtime/`.
- Runtime identity remains `victorian-gentleman-animated-v01`, so existing lots and UI references migrate without save changes.
- Runtime geometry: 4,787 vertices, 9,560 triangles, two skinned meshes, and 41 bones.
- Runtime material correction: metallic is packed in RGB and inverted source roughness in alpha. This restores the dark coat and top hat by removing the erroneous uniform gloss caused by treating an opaque metallic JPG as Unity's metallic/smoothness map.
- Black-level correction: runtime uses the UV-registered `base-color-dark.png` derivative, which deepens the coat and top-hat texels without applying a global tint to skin or shirt. The supplied base color remains preserved.
- Shadow contract: characters use a presentation-root-owned feathered ground shadow driven by the canonical 3D sun direction. Raw skinned-mesh shadow casting is disabled because root-motion-suppressed animation bounds could detach the caster from a moving character and leave a humanoid-shaped ghost elsewhere on the lot.
- Autonomous travel follows the canonical Lot Editor headings: North 0°, East 90°, South 180°, and West 270°.
- Business-as-usual walking is destination-driven: walking now has a 75% selection weight and, once begun, maintains its cardinal heading until the character reaches the corresponding lot boundary. Arrival produces a brief pause before a new action; elapsed animation time can no longer cause a mid-lot reversal.
- Animation families: agree, afraid, bow, clap, fall, flee, fold arms, hit to body, idle, laugh, look around, sit, wait, and walk.
- Business-as-usual behavior: walk 60%, wait 10%, fold arms 5%, idle 20%, and look around 5%. Explicit walking input temporarily suspends autonomous selection; seated characters remain seated.

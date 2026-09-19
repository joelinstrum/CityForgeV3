# Garden hedge and wrought-iron fence color

The existing clipped-hedge leaf texture is unchanged. Its shared seasonal material tints now favor deep green with little blue: summer `(0.14, 0.29, 0.09)`, spring `(0.16, 0.32, 0.10)`, autumn `(0.13, 0.24, 0.07)`, and winter `(0.11, 0.21, 0.07)`. This affects the hedge-bordered Natural Grass pieces and the older Georgian clipped-hedge plots, which use the same leaf material contract. It does not alter global season lighting.

The straight and corner wrought-iron fence props now use a neutral `(0.25, 0.25, 0.25)` material tint. Their existing base-color, normal and metallic/smoothness textures remain intact. The pale ground strip reads as dark gray; because the fence is a single texture atlas, its stone and brick surfaces are also subdued by this tint. Other fences, garden beds and props are unchanged. No saved IDs, geometry, selection anchors, Undo behavior, or manual Save behavior changed.

Validation: `Validation/garden-hedge-fence-color-v01/hedge-and-fences.png` is an isolated 45° render of a hedge-bordered lawn and both fence shapes. Unity compiled, and the temporary fixture and review code were removed. The active Lot session was not modified or saved. This was a visual check, not physical pointer placement or disk save/reload testing.

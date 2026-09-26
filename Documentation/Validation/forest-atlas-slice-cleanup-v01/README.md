# Forest atlas slice cleanup

The 4×3 true-angle deciduous atlases were cut at fixed 341/683 px row boundaries. In the summer sheet, every middle-row slice included separate foliage from the next row, with detached alpha components of roughly 400–1,100 px. Some column cuts also crossed branches, leaving straight edges. The new per-column cuts follow the gaps in the artwork; their trunk pivots use measured source-sheet foot positions. The same layout is used for summer, fall, and winter.

Summer deciduous slot 11 still has a disconnected 52×48 px foliage island inside its source art, so its appearances use clean slot 9 instead. Narrow fir slot 5 similarly has a detached 29×25 px summer island, so fir selection uses the other clean narrow slots. The atlas PNGs remain unchanged and no tree records or placements are rewritten.

An isolated Unity 6.1 batch run passed script compilation and checked all three seasonal atlases, slice bounds, pivots, and fir/deciduous slot selection. The visual result still needs review in Joe's open district editor after it reloads.

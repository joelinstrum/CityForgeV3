# Base stops at lot; overlay continues outside

`base-stops-at-lot.png` shows an isolated 30 × 30 m Brick Paving base ending at its own boundary. `overlay-outside-lot.png` adds a temporary independent 10 × 10 m overlay tile immediately beyond the east edge. The heightfield mesh bounds were ±15 m on both ground axes. The scene fixture and overlay were destroyed after capture; no user lot or disk save was changed.

`targeted-editmode.xml` records **2 passed, 0 failed** for the boundary/overlap test and the existing base/overlay catalog and persistence test. The test runs are specific to this work; earlier forest and regional suites are unrelated.

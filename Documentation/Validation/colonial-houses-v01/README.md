# Colonial Houses V01 validation — September 18, 2026

The two directional contact sheets show eight shared-camera views per house,
including both door fronts, rear walls, roof planes, stone bases, and brick
chimneys. The four front-corner images were inspected at full resolution.
The source geometry and textures remained intact; calibration to 10 m high
is provisional.

In isolated Regions Review, `live-near-3d.png` shows both houses as real-time
models in an unsaved residential lot. `live-far-billboards.png` shows the same
pair using their distant billboards after the orthographic facing correction.
The Residential / Colonial library showed both enabled cards. Focused EditMode
tests passed (3/3): each model loads and places as one 3D building, each has
loaded texture and far-view resources, and two distant buildings choose the
same facing under an orthographic camera. Results are in
`colonial-houses-editmode-tests.xml`. Testy, District 9 was restored from its
latest manual save after review; no lot, district, or region was saved.

The active review screenshot includes the selected building's cyan spatial
outline. Dense-district profiling and winter-specific distant artwork remain
open; the short isolated review does not establish long-duration stability.

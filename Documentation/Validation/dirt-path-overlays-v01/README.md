# Dirt path overlay validation

The existing grass/overlay persistence test passes, including all four newly registered texture resources. A separate empty-lot fixture paints and rotates each dirt path piece, then verifies its ID and rotation after in-memory save-data serialization/restoration. No user saves written.

All four source PNGs match their runtime copies byte-for-byte (see source-hashes.json). The runtime curve filename corrects the supplied dirth-path typo. Unity compilation and diff checks pass. Existing full-size source artwork uses normal runtime texture quality settings.

The isolated helper is archived for repeatability. This is content import validation, not a district-performance or pathfinding test.

Actual windowed Unity Game view inspected: all four shapes render over grass with transparent surrounds. See unity-overlays.jpg. Review left in an unsaved overlay fixture.

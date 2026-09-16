# Quarry delivery access

A fully loaded wagon in Testy / District 9 could leave the quarry but could not find a destination: the only Brickworks receiving point was beyond its far side, outside the road connection radius. The model was valid and the simulation was running.

Brickworks now exposes receiving bays on four sides, transformed with its rotation and saved nudge. The existing front bay is retained for saved routes. Navigation chooses a reachable bay and validates the full articulated wagon path. Each receiving driveway joins nearby road cells; it does not substitute for the connected road graph. Small turning aprons are sized from the Forestry wagon turning radius and width. Building, tree, water, terrain and district bounds checks still apply throughout the apron.

Validation: Unity 6000.1.12f1 compilation and six Brickworks regression tests passed, including a rear-road regression, disconnected roads, flooding, pause/reload and material accounting. Existing disconnected-road coverage now severs access to every bay, since the original front-only gap does not isolate a building with rear access. The actual user's quarry delivered four tons in the isolated Review session, the Brickworks began producing bricks, and the empty wagon drove back on the road. Main Unity project and road layout were not modified. The remote quarry with only an isolated road cell remains disconnected, as expected.

Full cycle confirmed at 20:43:19 local: wagon returned to (-464.6297, 37.3254), phase mining, cargo zero; Brickworks processed all four delivered tons into four bricks. The raw live-trip report includes the initially blocked return before the turning-apron fix and the successful trip afterward.

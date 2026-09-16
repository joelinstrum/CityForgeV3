# Selected building operational warnings

The shared DistrictSelectable contract supplies optional live warnings to the existing right-side selection panel. The panel refreshes every 500ms and hides empty warnings; it does not perform route searches. Quarry warnings cover paused operations, unpaid wages, missing/paused Brickworks, failed outbound routing and blocked returns. Brickworks reports stock and production, unavailable quarry supply, and failures from explicitly assigned suppliers. Unassigned route failures are not attributed to arbitrary Brickworks.

Seven delivery and warning regression tests passed in the isolated Regions Review Unity editor. Production runtime changes were also applied narrowly to that review project.

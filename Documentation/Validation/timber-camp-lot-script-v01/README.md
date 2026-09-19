# Timber Camp Lot script validation

- `lot-script-tests.xml`: 22/22 passed. Four new cases cover automatic Lumberjack,
  Forestry Cart and Connector binding; missing-object rejection; script JSON
  round-trip; and idempotent placed-Lot crew creation.
- `timber-tests.xml`: 14/14 passed. Includes source-Lot crew/worker cleanup plus
  existing tree delivery, cargo, mill routing, wagon arrival and reload checks.
- `labor-tests.xml`: 9/9 passed. Existing tree claiming, route budget, chopping,
  return, wage and save compatibility coverage remains green.

Unity 6000.1.12f1 ran the tests in an APFS copy-on-write temporary project. The
main editor and all player save files were left untouched. No live visual or
physical pointer test is claimed.

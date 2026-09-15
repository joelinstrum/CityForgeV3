# Lumber-loading lot behaviors — v01

A lot can own a saved behavior instance independently of its building prefab. This first behavior uses two copies of the supplied dock worker, animated with its native idle/walk clips and a procedural carrying pose. Workers pick up bundles, walk to the shore-side dock point, unload, and return. The barge leaves only after it has 12 bundles and both workers have returned.

## Using it

Open **Lumber Mill Dock Operations v01** in the Lot Editor. This is a new 40 × 30 m derivative of the original Lumber Mill lot, with room beside the mill for the barge and workers. The original saved lot remains unchanged.

Open **Main → Lot Behaviors…**, or **Boats → Lot Behaviors…** when inspecting a boat. Attach a routine to the selected boat (or the first boat if none is selected). One routine may own each boat. Pause/Run controls it; Reset Cargo starts a fresh shipment. Set Pickup Point and Set Dock Point each arm the next ground click; Escape cancels. Moving an anchor resets cargo. Keep the straight walking corridor clear of buildings and water. Save the lot after editing.

The Lot Editor has no district river: full boats wait there. In a district, place the boat partly over a real river while keeping the worker path on shore. Navigation requires enough channel width/depth and an authored downstream endpoint at the district boundary. On departure, the barge follows that channel; it disappears when it reaches the downstream exit.

The routine observes the district simulation pause. Each placed lot gets its own fresh shipment state, independent of the library template and other placed mills. Region saves retain that instance's workers, cargo, route, and traveled distance. Active district behavior state is saved every five seconds and at the existing region save/quit points. If the route disappears or changes during a voyage, the boat pauses at its saved route position; restoring the route lets it continue.

## Python and mods

The live game currently executes a C# behavior runner. It loads portable JSON definitions, including overrides from:

`~/Library/Application Support/City Forge/City Forge V3/Mods/LotBehaviors/`

Python can author those definitions and run the supplied **standalone reference simulator without Unity**. Python is not embedded in the player, and arbitrary Python scripts are not yet live game mods. The C# state machine (`CargoLoadingSimulation.cs`) has no Unity dependency and can also be hosted by a headless .NET process. This separates simulation data from animation/rendering.

From this folder:

```sh
python3 lumber_loading.py --capacity 20 --output my-dock.json
python3 simulate.py --definition my-dock.json --verify
python3 simulate.py --definition my-dock.json --connected --seconds 300
```

Copy the JSON to Mods/LotBehaviors and choose Reload Mod Definitions. Its stable `id` overrides the built-in routine. Give it a different id/displayName to add another recipe. Fields configure worker count/prefab, capacity, pickup/unload duration, walking/departure speed, and stagger. Changing worker count or lowering capacity below existing cargo requires Reset Cargo. Speeds and timings must be positive; workers are limited to 1–16 and capacity to 1–1000.

`kind: cargo-loading-v1` is the supported behavior family. Future shopkeeper/shopper families or a live Python host need additional command adapters; existing shopkeeper/shopper scripts have not been migrated.

## Deliberate first-version limits

- Cargo bundles are visual shipment units, not deducted from the district's economic lumber inventory.
- The boat follows one authored river to its district exit. Confluence routing, recipient-town delivery, payment, return trips, and automatic replacement barges are not implemented yet.
- The source contains idle/walk clips, not bespoke lifting animations. Carrying arms are posed procedurally; pickup/unload use short idle waits.
- Worker paths are authored straight lines, not obstacle navigation. The pickup stack is a visual source and does not deplete.
- Boat cargo arrangement fits the imported wooden barge. Additional hulls will need cargo/deck profiles.
- A connected river means a sufficiently wide/deep channel with a downstream district exit, not proof that a recipient town exists beyond that exit.

## Validation

15 Unity EditMode tests pass: capacity reservations, odd capacities, workers clear before departure, missing boat, route loss/resume, save round trips, legacy lots, per-placement progress, and both river directions. The Python simulator's built-in checks pass. Live windowed Game view checks cover two workers carrying, unloading, full/no-river wait, and downstream movement. Evidence and the original lot backup are under CityForgeMCP/artifacts/behaviors/lumber-loading/v01.

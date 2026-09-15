# Editable lot scripts — v01

Open **Main → Lot Behaviors**. Each routine has **Start / Run / Pause**, **Re-run**, and **{ } Script**. Start begins a fresh routine; Run resumes a paused routine; Re-run resets progress and starts again. Newly added or edited routines remain stopped until Start.

The main panel is generic. Task-specific configuration lives in the script. **Add Behavior** opens the recipe library. **Import Script** opens a file/folder browser for JSON scripts; imported source opens for review before anything is applied. Enter a full file path or browse folders. The example `lumber-mill.lot-script.json` targets the saved Lumber Mill Dock Operations v01 barge.

In **Script**, edit the source, choose **Check** to validate, then **Apply**. Apply replaces that lot instance's script, resets its previous progress, and leaves it ready to Start. Save the lot using the normal Save button to persist edits. **Cancel** discards the draft. **Copy Script** copies source for sharing/saving in an external editor. **Import** inside the editor replaces the draft, not the running routine, until Apply.

**Objects / IDs** lists the lot's placed buildings, props/boats/characters, flora, components, effects, decals, water, road pieces and graph elements. Existing IDs are retained; blank IDs receive persistent unique values. Moving/rotating and saving objects preserves them. IDs are scoped to a lot instance, so separate district copies may use the same local IDs without controlling one another. Generated crew have stable IDs of the form `behavior-id/worker/1`; these are reserved identifiers for future actor commands, not editable placed objects.

## Script format

This is a declarative JSON script, not Python source. It selects the supported `cargo-loading-v1` routine and its configuration. Its fixed execution order is pickup → walk → unload → return, repeated until full, then wait for workers to clear and depart on a connected downstream river. The editor describes that order. Arbitrary code, reordered steps, and new routine kinds require a future interpreter/adapter; the existing Python authoring/reference tools still work outside Unity.

- `boatId`: ID of the target placed boat. A boat can have one owning routine.
- `pickup`, `dock`: each contains `objectId` and an `offset` vector in metres.
- Empty `objectId`: the offset is an absolute point in the lot's local coordinates.
- Nonempty `objectId`: the offset is added to the referenced object's local position. Offsets follow the lot axes, not the object's rotation. Deleting a referenced object prevents the routine from advancing.
- `behavior`: display name, routine kind, worker prefab/count, capacity, timings, speed and stagger. These settings are embedded with this lot's script, so editing one mill does not change other mills or the global recipe.

Not every registry item is a usable location. Current location bindings support placed props/boats, flora, buildings, free effects, decals, circulation nodes and road pieces. Unsupported location types are rejected. Imported IDs from another lot must be rebound using Objects / IDs before Apply.

Validation checks the schema, numeric values, prefab availability, target type, target ownership, referenced IDs and lot bounds before changing the document. Invalid drafts preserve the existing routine. Old lot routines remain compatible until explicitly edited; `HasScript` distinguishes embedded source from Unity's default-deserialized empty objects.

## Evidence

28 Unity EditMode tests passed, including script validation, unchanged data on invalid target, independent configuration, Run/Re-run semantics, stable ID migration, save/copy, object location references and legacy routine migration. Script editor, generic controls and object list were inspected in normal windowed Unity Game view. Automated physical mouse calls did not produce UI Toolkit pointer events; direct mouse acceptance remains for Joe's review.

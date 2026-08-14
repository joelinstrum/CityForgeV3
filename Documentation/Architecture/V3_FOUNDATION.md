# City Forge V3 foundation

## Product direction

V3 is a genuine 3D spatial game with a constrained isometric camera. Detailed
architecture may remain pre-rendered in directional 2D views while hidden or
subtle 3D proxies provide placement, collision, selection, depth, occlusion,
terrain contact, and shadows.

The prior City Forge project remains intact and is not a template for V3.
Approved content can cross the boundary only through an explicit migration.

## Runtime boundaries

1. **Simulation** owns durable game state and has no dependency on whether an
   entity is presented as a sprite, proxy, or full model.
2. **Spatial world** owns transforms, lot occupancy, collision, proxy geometry,
   attachment points, and scene depth.
3. **Hybrid presentation** selects directional renders and registers them to
   the spatial contract.
4. **UI** issues commands and observes state. It does not contain simulation
   rules.

## Hybrid building contract

Every hybrid building will ultimately provide:

- foundation dimensions and origin;
- proxy volumes and optional attachment points;
- four approved directional RGBA renders;
- the exact authoring camera contract;
- projected foundation corners and per-view registration;
- optional per-view depth;
- seasonal and time-of-day overlays;
- validation evidence.

No hand-tuned screen offset is accepted without a reproducible projection
contract.

## UI architecture

UI is organized like a web design system:

- `Foundations`: color, typography, spacing, sizing, borders, elevation, motion;
- `Components`: buttons, icon buttons, fields, toggles, tabs, tooltips;
- `Patterns`: toolbars, inspectors, dialogs, notifications, asset browsers;
- `Screens`: splash, main menu, lot editor.

The prototype expresses foundations as USS variables and component classes.
Screen controllers compose these pieces and only publish semantic commands.
Legacy UI code and sliced controls are excluded.

## Vertical-slice acceptance

The first vertical slice is accepted when:

1. The approved splash opens and transitions to the new main menu.
2. Existing menu labels are visible; only Lot Editor is interactive.
3. Lot Editor opens a true 3D scene with a constrained isometric camera.
4. A visible lot grid, proxy building, selection state, and camera rotation work.
5. A directional presentation can be attached to the proxy without changing
   simulation state.
6. A foreground actor can prove front/behind occlusion behavior.
7. The lot can be serialized and restored.
8. UI components demonstrate consistent hover, pressed, focus, selected, and
   disabled states.

The current foundation completes items 1–4 at prototype level. Items 5–7 are
the next technical milestone.


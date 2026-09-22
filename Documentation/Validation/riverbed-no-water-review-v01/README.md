# Riverbed review with visual water disabled

Date: September 22, 2026

The river-water shader's `_WaterVisible` review switch is temporarily set to
zero so the user can inspect the complete authored riverbeds without animated
blue water or whitecaps. Restoring its default to one re-enables water in one
line.

This does not remove or alter river geometry, banks, riverbed artwork, water
surface sampling, navigation, collision, editing, saved data, flow direction,
or district persistence. Existing water meshes and materials remain present;
the shader clips their visual fragments only. No player content is written.

The shader and river material contract are covered by the focused riverbank
EditMode suite in the isolated project fixture. The open City Forge V3 editor
is not driven or restarted for this review.

# Farmer hoeing automata v01

Source: `Assets/CityForgeV3/Resources/CityForgeV3/Props/Characters/FarmerV01/Farmer_Animated_v01.fbx` and its canonical `base-color.jpg`. The source model and texture are unchanged.

Derivative: `Assets/CityForgeV3/Resources/CityForgeV3/Automata/FarmerHoeV01/Group/`, built with `Tools/build_farmer_hoe_atlases.py` and `Tools/pack_farmer_hoe_atlases.py`. Eight directional RGBA atlases bake the source Hoe and Walk actions into one 32-frame, four-fps, eight-second loop. The farmer hoes at two patches and walks between them; both stations and the body remain within a 3 × 3 m lot footprint. The clip is daylight-only and unavailable in winter by default, editable through the normal automata schedule controls.

This is a reusable Automata library entry, not a farm-specific runtime special case. Frames and sprites use the existing shared clip cache; the animation does not create a rig or run a farm-specific update in play. Existing saved lots still reference their 3D farmer props until a user replaces those props in the Lot Editor and explicitly saves the lot. No lot or district progress is rewritten by this migration.

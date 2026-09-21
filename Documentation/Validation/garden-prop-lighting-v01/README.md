# Garden prop lighting validation

Date: September 20, 2026

The reported district capture showed the Town Center's aged white picket garden
reading gray even after the native building was corrected. Inspection found two
lighting paths inside the composed prop: flower cards already used
`LitShadowReceivingSprite` and the district world uniforms, while the 3D fence
still used Unity's plain Standard shader. Directional building shade therefore
removed most of the fence's readable light without the calibrated indirect
response used by the building beside it.

`GardenPropPBR` preserves each mesh's shared texture, tint, normal response,
metallic value, smoothness, shadow casting, and placement-preview blend state.
It scales only Unity GI indirect diffuse through the shared
`_CFNativeSurfaceIndirectScale`: 2.5 in Morning, Noon, and Afternoon and 1.0 in
Evening and Night. Its emission output is always zero.

The garden presentation boundary assigns the shader only to Standard-lit mesh
materials under the newly created or loaded garden root. Sprite cards retain
their existing shared flora material, and custom grass/water shaders are not
replaced. This one local creation-time pass is not repeated per frame or when
time changes; afterward all materials consume the already-published uniform.

An isolated Unity EditMode run passed 20/20 focused world-lighting, white-picket,
foundation-bed, hedge-bordered-grass, and natural-grass checks. The suite covers
the shared shader assignment, source texture preservation, all nine picket
compositions, seasons, preview transparency, placement rotation, and session
round trips.

A graphics-enabled isolated capture rendered the full cottage picket garden at
Noon twice with identical camera, sunlight, material, art, and grass. At neutral
indirect scale 1.0 the shaded fence read charcoal-gray. At the district scale
2.5 it returned to aged ivory while the flower cards and grass remained visually
unchanged. Unity reported no compiler or shader errors. The fixture did not load
or save player Lot, district, or region content; the open editor was not driven,
and CityForge-Regions-Review was not used.

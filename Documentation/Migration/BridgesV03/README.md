# Bridge assembly correction — V03

Source lineage and cut coordinates remain as documented in ../BridgesV02/README.md. No source mesh, texture, or runtime asset package changed in this revision. Both original end sections retain their shape.

The previous repeated assembly joined different left/right cut profiles. Alternating the complete center section longitudinally now joins matching profiles exactly. Negative longitudinal transforms reverse triangle winding and transform normals; deck height sampling uses the same orientation. An odd number of bays leaves the actual left and right ends connected to their original matching cut faces. This preserves the irregular source profile rather than fabricating a new rectangular section.

Earth approaches use newly generated local geometry and the existing shared ground material. Eight-meter road approaches rise to the bridge with grass shoulders graded toward local terrain. The road-textured vertical walls were removed. The earth overlay lives and is destroyed with the bridge, without mutating terrain or canonical artwork. Manual persistence stores the bridge record, from which the earth is reconstructed.

Validation and images: ../../Validation/district-bridges-v03/README.md.

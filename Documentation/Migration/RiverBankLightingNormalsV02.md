# River mirrored-bank lighting correction — 2026-09-09

Joel reported one unusually dark bank and a prominent seam at the river center in Screenshot 2026-09-09 at 3.50.23 PM.png. AddRiverBand intentionally retained identical triangle winding for both mirrored banks. RecalculateNormals consequently gave one side downward lighting normals. RiverBedSurface uses those normals directly for diffuse and ambient lighting; the mismatch also affected the two flat center-floor strips visible through translucent water.

Corrected only the bank normal array after RecalculateNormals: normals with negative Y are negated. Vertex positions, triangles, UVs, colors, shoreline variation, materials, shaders, textures and terrain shape remain unchanged. Retaining the original two-sided winding avoids reopening the earlier grass-edge disappearance workaround.

Validation: 10 bank meshes; zero downward normals; minimum normal Y 0.9985751. All 132 flat-center normals agree with up to within 5.96e-8. The normal docked Game view shows balanced bank lighting and removal of the center lighting split. See game-view.jpg and checks.txt. Runtime/editor compilation completed during the change. Test uses the transient generated river fixture; saved regions/lots were not written.

QA menu: City Forge > QA > River > Check Bank Lighting Normals. Source-only diff and prior source retained here.

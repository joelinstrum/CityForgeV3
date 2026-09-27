# Rounded rolling hill corners

September 27, 2026. In the saved "The Hills" district, the front corner had a
pointed diagonal ridge. The rolling hill height field faded toward the nearest
of the two district borders using a hard minimum. Where the nearest border
switched, that minimum made a visible straight crease from the corner.

The rolling hill edge fade now lifts the terrain beside that diagonal into a
rounded join. The diagonal's own height is retained, and the correction falls
to zero at district borders. The change applies only when generating rolling
hills; mountain terrain, soil artwork, and saved district settings are not
modified. The reference district was read from a copy of its save. The open
Unity editor and original save were not driven or written.

Paired isolated Unity 6000.1.12f1 renders of that exact district, with clouds
hidden for a clear terrain comparison, showed the sharp front-corner line
softened into a broad crown. A probe across eight positions near that corner
measured mean diagonal ridge excess of +0.281 m before the change and -0.232 m
afterward; the broader shoulders remove the pointed excess without lowering
the original diagonal. The district peak remained 58.5 m. The 15
`DistrictElevationTests` passed, including a new corner shape regression.

On a 4 × 4 district with 263,169 terrain samples, six timed height-field builds
after warmup had median CPU times of 123.26 ms before and 125.04 ms after. The
managed allocation counter reported 0 bytes in both timed paths; it does not
cover native Unity allocations. This is work during explicit district terrain
construction, not a per-frame cost. The mesh vertex count and draw calls are
unchanged. The short batch does not establish long-duration performance.

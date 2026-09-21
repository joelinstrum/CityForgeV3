# Region river authoring markers

Date: September 21, 2026

The hand-drawn region-river tool now presents its three existing serialized
sizes as **Major**, **Medium**, and **Stream**. The underlying enum values and
saved channel widths remain unchanged for compatibility:

| Visible tool | Existing enum | Saved width | Drawing marker |
| --- | --- | ---: | ---: |
| Major | `Major` | 128 m | 28 px |
| Medium | `Large` | 64 m | 7 px |
| Stream | `Small` | 18 m | 2 px |

The major marker is twice its former 14-pixel display width, and the stream
marker is thinner than the former 3-pixel small-river marker. These are
authoring-display changes only; generated rivers, saved manual-river widths,
district geometry, water materials, collision, construction constraints and
simulation are unchanged.

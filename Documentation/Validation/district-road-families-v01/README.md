# Restore country dirt road and expose pike separately

Dirt Road was erroneously mapped to the national-pike package while its menu
preview still showed the original grass-center artwork. Restore the original
DirtRoadId mapping and add Pike Dirt Road as the third district road family,
after Antique Brick Road. The existing generic placement and topology handling
serve all three choices. Original textures remain unchanged.

Saved package IDs are preserved: existing country roads remain country roads,
and existing pike roads remain pike roads. Painting a different family over an
existing tile replaces its package through the normal placement path. No bulk
save migration is attempted because manually placed and intentional pike tiles
share the same saved package ID. Regional national-pike placement is unchanged.

Unity compilation and five road-placement tests passed, including mixed-family
placement and replacing a pike tile with country dirt. No new rendering work,
scans, or texture assets. Review installation preserves the live region snapshot
without writing a progress save.

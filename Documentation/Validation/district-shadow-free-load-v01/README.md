# District shadow-free load pass

This temporary pass turns off district shadows in the normal region-to-district
view while load performance is investigated. It leaves the standalone Lot
editor and shadow-enabled test path available. Re-enable by changing the
`ShowDistrictShadows` setting at the district entry point.

## Measured case

The saved Chambersburg district was copied to an isolated Unity project for
read-only profiling. It has 1,777 district trees, seven placed lots, 47 roads,
and one river. Timings below measure `RebuildEntireDistrict`, not save parsing,
Unity startup, or the first rendered frame. The cold EditMode runs vary with
asset caching and should be read as approximate comparisons.

| Configuration | District rebuild | Unity allocated-memory change |
| --- | ---: | ---: |
| Before this pass, shadows enabled | about 7.0–7.5 s | about 1.53 GB |
| Before this pass, district tree shadows skipped in profiling fixture | about 6.2 s | not recorded |
| Deferred saved-lot construction, shadows enabled | about 3.9 s | about 0.90 GB |
| Deferred saved-lot construction, shadows disabled | about 3.3–3.4 s | about 0.84 GB |

The largest avoidable cost was the default Government House hybrid presentation
built for each hosted lot before loading its saved state. The seven provisional
builds took about 2.5–2.7 seconds and were then replaced or discarded. Hosted
lots now create their shell first and build only the saved presentation.

Tree shadow creation and updates accounted for roughly 0.6–0.8 seconds in this
case. The shadow-free path also skips cloud shadows, hosted-lot projected tree
and building shadows, and native sun shadows. Some vehicle and lot prefab
shadow objects are still constructed by their existing builders; their
renderers are disabled after each hosted lot loads and after time-of-day
changes. Removing that construction is a separate optimization.

With these changes, the shadow-free rebuild still spends about 1.6 seconds on
the seven saved lots, 0.52 seconds on flora, 0.51 seconds on the district grid,
0.33 seconds on the river, 0.20 seconds on ground, and 0.13 seconds on resource
presentations. These are bulk load costs; this pass does not add routine full
district redraws or scans. Renderer inspection is limited to a lot when that
lot is loaded, and cached references are reused for time-of-day changes.

## Validation and limits

The focused shadow-free district, shadow-enabled staged flora, and hosted-lot
orientation tests passed 3/3 in an isolated Unity fixture. The actual saved
Chambersburg profile test passed 1/1, with zero active shadow-named renderers
and zero shadow-casting lights. The complete `DistrictFloraBatchesTests` class
passed 34/35; its camera-render assertion had zero visible pixels in the
headless test's control view. The broader EditMode run previously had 92
failures and is not green. A related district lighting, lot site, and Town
Center selection passed 16/18; its two failures invoke
`LotContentCatalog.Add` with a mismatched reflection argument count before
reaching the hosted-lot build path. The open interactive Unity editor was not driven,
so the visual result and first-frame timing still need Joe's review. Unity's
allocated-memory change here is not a managed allocation or steady-state
memory measurement. GPU draw calls, frame-time spikes, and long-duration
behavior were not measured in this pass.

# River generation UI default V01 validation

Date: September 21, 2026

Unity 6000.1.12f1 compiled without C# errors. Eleven focused EditMode tests
passed:

- `MapChromeTests`: 8/8
- `RegionTerrainMenuTests`: 3/3

The new end-to-end regression begins with a region whose major and small river
settings are both `None`, invokes the same map-level regeneration action used
by the UI, verifies that the Rivers panel opens with one major and two small
rivers selected, submits the actual Generate Rivers button callback, and
requires exactly three generated paths plus modal closure. It also verifies
that an explicitly empty submission returns visible guidance and does not
alter river data.

The isolated `CityForge-Regions-Review` project is loaded through the standard
sync workflow after commit. No player region is written automatically.

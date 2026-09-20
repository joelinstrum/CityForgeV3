# District Town Center founder Lots

The persisted Lot type value `8` is **District Town Center**. Earlier values
remain unchanged. Lot Editor → New Lot and Lot General expose it as a Civics
subcategory. The ordinary Civic district browser includes both general Civics
and District Town Center Lots.

Opening Start Town refreshes the existing Lot content cache once and adds one
card for every bundled, player-saved, or mod Lot with this type. Selection is by
stable Lot ID; no name or building ID is hard-coded. Founder placement uses the
selected Lot's complete geometry and its authored population, jobs, wages,
seasonal revenue/cost, services, and resource benefits. It stores no population
override and grants no synthetic food or money. Existing seasonal systems apply
those values through their maintained per-district aggregates.

The Fort remains a separate compatibility path with zero founder population and
a 250-food minimum. Older saves whose founder identity is `city-charter-house`
retain the earlier zero-population compatibility migration, but no new founder
card uses that identity.

Focused isolated Unity EditMode results are in `tests.xml`. Coverage includes
all nine Lot population categories, enum/JSON stability, Civics authoring maps,
normal Civic browsing, dynamic founder discovery, runtime Town Center loading,
the unchanged Fort behavior, and a founder fixture with authored population,
jobs, finances, Culture capacity, and food production. No player Lot, district,
or region was saved.

# River banks V05 Wide — size-specific muted shallows

Date: September 21, 2026

Major rivers now select a separate bank family at widths of 100 metres or
greater. Region generation currently creates medium rivers at 48–76 metres and
major rivers at 144–228 metres, leaving an intentional gap around the cutoff.

Runtime assets:

- `BanksV5Wide/shoreline-wide-muted.png`
- `BanksV5Wide/open-gravel-wide-muted.png`
- `BanksV5Wide/submerged-gravel-wide-muted.png`

The accepted `BanksV4` artwork remains the active family below 100 metres.
Only resource selection changes: continuous 48-metre mapping, bend variation,
meshes, water animation, junction blending, simulation and persistence are
unchanged. To avoid a visible cutoff against terrain, the major-river bank
overlay extends eight metres beyond the physical channel and its existing
transparent edge fade spans that shoulder. This is presentation-only: channel
width, collision and construction clearance do not expand. Resource choice and
fade calibration occur during an existing river presentation rebuild and add
no per-frame work or draw calls.

Generation lineage and exact prompts are documented in
`Documentation/ArtStudies/RiverBanksV05Wide/README.md`.

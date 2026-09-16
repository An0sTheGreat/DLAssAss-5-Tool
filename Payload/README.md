# Tool payload

Release builds package the verified add-on as:

`renodx-dlss5-super-anus.addon64`

Maintainers must place that file here before running `scripts/publish.ps1`.
The binary is ignored by Git and validated against the pinned SHA-256 before
packaging. NVIDIA DLLs never belong in this folder.

Manager `1.0.8` includes release addon `1.0.8-multipass-edge.1`, Windows file
version `1.0.8.18`, with independent controls for each pass, colour 100% and sharpness
0% defaults, shared sequential-pass scratch resources, and fence-drained admission
when increasing passes. Compact native resolution controls remain available when
scaled allocation fails, provided their smaller allocation fits safely. The nested-SR guard
is retained; investigation-only probes are excluded. Compact allocation failures
now retry and permit fence-safe cache reuse, rather than permanently bypassing
controls. Temporary native fallback under genuine exhaustion remains. BOTDW's focus-change freeze
remains unresolved.
Direct sharpening now uses the incoming pass image instead of raw neural detail;
zero-transfer sharpening is independent. Interrupted passes invalidate dependent
history before managed recovery. Multipass motion handling is now selectable in
game, with game-motion reuse as the recommended default and legacy zero-motion
behavior retained.
The neutral first pass now participates in transition tracking, preventing later
passes from staying on native fallback after changing the pass count at 100%.
Encoding and Pass 1 Neural Detail and Colour values now restore separately per
preset. Preset-scoped edge strength, thickness, softness, shift and visualization
apply to Pass 2 and later. Multipass capture now waits for every bypassed pass so
the OFF image is a zero-pass comparison. Cyberpunk shimmering remains unresolved.
SHA-256: `6A1A9F37FB8F861C02BFDFFF001C5285F202649C694E10356BFBB5F1F9C3EA12`.

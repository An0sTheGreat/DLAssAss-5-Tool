# Tool payload

Release builds package the verified add-on as:

`renodx-dlss5-super-anus.addon64`

Maintainers must place that file here before running `scripts/publish.ps1`.
The binary is ignored by Git and validated against the pinned SHA-256 before
packaging. NVIDIA DLLs never belong in this folder.

Manager `1.0.9` includes release addon `1.0.9-vram-warning.2`, Windows file
version `1.0.9.21`, with independent controls for each pass, colour 100% and sharpness
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
game. Chained Temporal History is recommended and defaults on new configurations;
existing saved selections and the other motion modes are retained.
The neutral first pass now participates in transition tracking, preventing later
passes from staying on native fallback after changing the pass count at 100%.
Encoding and Pass 1 Neural Detail and Colour values now restore separately per
preset. Preset-scoped edge strength, thickness, softness, shift and visualization
apply to Pass 2 and later. Multipass capture now waits for every bypassed pass so
the OFF image is a zero-pass comparison. Launch state is persistent, and the edge
masking path has a complete bypass switch. Cyberpunk shimmering remains unresolved.
SHA-256: `EE36FE7BD29221A7DAE5AEF85E667E9AB61CEB331E0370E0E01B4FA6B4D9D6A5`.

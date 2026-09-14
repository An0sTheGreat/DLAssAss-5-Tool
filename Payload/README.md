# Tool payload

Release builds package the verified add-on as:

`renodx-dlss5-super-anus.addon64`

Maintainers must place that file here before running `scripts/publish.ps1`.
The binary is ignored by Git and validated against the pinned SHA-256 before
packaging. NVIDIA DLLs never belong in this folder.

Manager `1.0.6` includes release addon `1.0.6-slider-reset.1`, Windows file
version `1.0.6.17`, with independent controls for each pass, colour 100% and sharpness
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
Cyberpunk shimmering remains unresolved; this is not a shimmer-fix release.
SHA-256: `8F282AC07D6F430580626A4DA375E2501912C450CF6D22B857406E91874A9622`.

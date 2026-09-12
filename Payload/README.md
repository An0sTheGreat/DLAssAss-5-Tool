# Tool payload

Release builds package the verified add-on as:

`renodx-dlss5-super-anus.addon64`

Maintainers must place that file here before running `scripts/publish.ps1`.
The binary is ignored by Git and validated against the pinned SHA-256 before
packaging. NVIDIA DLLs never belong in this folder.

Manager `1.0.3` includes release addon `1.0.3-manager-release.3`, Windows file
version `1.0.3.12`, with independent controls for each pass, colour 50% and sharpness
0% defaults, shared sequential-pass scratch resources, and fence-drained admission
when increasing passes. Compact native resolution controls remain available when
scaled allocation fails, provided their smaller allocation fits safely. The nested-SR guard
is retained; investigation-only probes are excluded. Compact allocation failures
now retry and permit fence-safe cache reuse, rather than permanently bypassing
controls. Temporary native fallback under genuine exhaustion remains. BOTDW's focus-change freeze
remains unresolved.
Direct sharpening now uses the incoming pass image instead of raw neural detail;
zero-transfer sharpening is independent. Interrupted passes invalidate dependent
history before managed recovery. Motion handling and saved settings are unchanged.
The neutral first pass now participates in transition tracking, preventing later
passes from staying on native fallback after changing the pass count at 100%.
Cyberpunk shimmering remains unresolved; this is not a shimmer-fix release.
SHA-256: `21C61735076FCB1FC0F439542F34D003F172D91D43253D5C1884EBD90C43714A`.

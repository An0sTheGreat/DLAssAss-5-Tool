# Tool payload

Release builds package the verified add-on as:

`renodx-dlss5-super-anus.addon64`

Maintainers must place that file here before running `scripts/publish.ps1`.
The binary is ignored by Git and validated against the pinned SHA-256 before
packaging. The pinned NVIDIA runtime files are kept in `DLSS Files`.

Manager `1.1.1` includes release addon `1.1.1-dx11-neural-controls.1`, Windows file
version `1.1.1.30`, with independent controls for each pass, colour 100% and sharpness
0% defaults, shared sequential-pass scratch resources, and fence-drained admission
when increasing passes. Compact native resolution controls remain available when
scaled allocation fails, provided their smaller allocation fits safely. The nested-SR guard
is retained; investigation-only probes are excluded. Compact allocation failures
now retry and permit fence-safe cache reuse, rather than permanently bypassing
controls. Supported DX11 games do not require the external DLSS 5 Bridge.
Same-generation working resources remain cached while
NR is active, and valid host-owned native features remain retained across stream
generations, so reducing the pass count does not release NVIDIA features live.
The cache may grow from 512 MiB to 1 GiB only when DXGI confirms safe headroom;
the original reserve and 512 MiB fallback remain. Temporary native fallback
under genuine exhaustion remains. BOTDW's focus-change freeze
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
The integrated build also contains DLSS5 Feeder 1.16.0-beta.6 and is enabled
only when the manager writes `managed_non_dlss=1` for a supported non-DLSS game.
SHA-256: `A764CB6D4E0E7A52D7B6D6DF706C57FB3361900DCA264BA9CF8E2689B877B235`.

# DLAssAss 5 Tool v.1.0.9

## Changes since v.1.0.8

- Bundle addon 1.0.9.19 with a persistent option to start Neural Rendering
  enabled or disabled. The custom option appears above RenoDX's native preset
  and options-mode selectors.
- Add Chained Temporal History as the recommended Multipass Motion mode and the
  default for new configurations. Existing saved selections remain unchanged.
- Add a persistent master switch that completely bypasses multipass edge masking
  while retaining the existing strength, thickness, softness, shift, and
  visualization controls when enabled.

## Build identity

- Manager: **1.0.9**, Windows **1.0.9.0**.
- Bundled addon: **1.0.9-startup-history.1**, Windows **1.0.9.19**.
- Addon SHA-256: `EE36FE7BD29221A7DAE5AEF85E667E9AB61CEB331E0370E0E01B4FA6B4D9D6A5`.

No NVIDIA runtime DLLs are included. `SHA256SUMS.txt` inside the archive covers
the executable and addon; `SHA256SUMS-v.1.0.9.txt` covers the release ZIP.

## Validation

The full v63-v66 regression suite and guarded release-addon build pass, including
Windows release metadata, static hook verification, UI/config defaults, edge-mask
bypass, DX11 lifecycle tests, Vulkan export/ordinal checks, and the nested source
guard. Live game compatibility still varies; this release does not claim to fix
Cyberpunk's separate multipass shadow shimmer.

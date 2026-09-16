# DLAssAss 5 Tool v.1.0.8

## Changes since v.1.0.7

- Bundle addon 1.0.8.18. Encoding and Pass 1 Neural Detail and Colour values
  now restore separately for each preset.
- Add preset-scoped multipass edge protection strength, thickness, softness,
  inward/outward shift, and live visualization for Pass 2 and later.
- Fix edge processing that could overwork the GPU, freeze the image, or show a
  mostly black frame when enabling multiple passes.
- Retain the nested Frame Generation source guard used to prevent TLOU2 flicker.
- Correct NR ON/OFF screenshot pairs: with multiple passes, OFF waits until the
  complete pass group is bypassed and therefore contains zero NR passes.

## Build identity

- Manager: **1.0.8**, Windows **1.0.8.0**.
- Bundled addon: **1.0.8-multipass-edge.1**, Windows **1.0.8.18**.
- Addon SHA-256: `6A1A9F37FB8F861C02BFDFFF001C5285F202649C694E10356BFBB5F1F9C3EA12`.

No NVIDIA runtime DLLs are included. `SHA256SUMS.txt` inside the archive covers
the executable and addon; `SHA256SUMS-v.1.0.8.txt` covers the release ZIP.

## Validation

The capture-policy tests cover one-, two-, and three-pass OFF readiness. The
full v63-v66 regression suite and guarded addon build pass, including Windows
version, static hook, shader-layout, lifetime, UI, DX11, Vulkan-export and nested
source checks. Live game compatibility still varies; Cyberpunk's separate
multipass shadow shimmer remains unresolved.

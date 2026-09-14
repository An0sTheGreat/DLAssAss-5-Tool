# DLAssAss 5 Tool v.1.0.6

## Changes since v.1.0.5

- Unsaved Pass 1 and Pass 2–10 Neural Color Strength values now default to
  100%. Existing saved values are preserved.
- Neural Color Strength now ranges from 0–200%. Hovering the control warns that
  values above 100% may oversaturate, leave the display gamut or strengthen
  haloing.
- Every native RenoDX slider and custom Neural Rendering slider now has a themed
  right-click **Reset** action. It resets only the selected slider to its
  authoritative default through the existing persistence path.
- Neural Rendering Resolution resets to a staged 100%; **Apply** remains required
  before the rendering resolution changes.

## Build identity

- Manager: **1.0.6**, Windows **1.0.6.0**.
- Bundled addon: **1.0.6-slider-reset.1**, Windows **1.0.6.17**.
- Addon SHA-256: `8F282AC07D6F430580626A4DA375E2501912C450CF6D22B857406E91874A9622`.

No NVIDIA runtime DLLs are included. `SHA256SUMS.txt` inside the archive covers
the executable and addon; `SHA256SUMS-v.1.0.6.txt` covers the release ZIP.

## Validation

The full V6.6 regression suite, native patch preservation, Windows version,
DX11 lifecycle/isolation, Vulkan export/ordinal lookup, and manager tests pass.
The production shader passed 731 hardware and 731 WARP resolves, 24 three-pass
chains and 63 independent final-image comparisons including 200% color.
These checks are not a substitute for live game testing.

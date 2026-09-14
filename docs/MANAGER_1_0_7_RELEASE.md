# DLAssAss 5 Tool v.1.0.7

## Changes since v.1.0.6

- The window remembers its last restored size and position and safely restores
  maximized sessions within the available desktop.
- Library covers and major sections use consistent rounded corners. Right-click
  a game and choose **Change artwork** to import a validated image into managed
  local storage without depending on the source file afterward.
- **UPDATE ALL** turns green when more than one detected addon installation is
  eligible. Completion details expand into matching Updated, Skipped and Failed
  bullet lists; successful entries stay concise and full paths remain available
  in **SETTINGS & LOG**.
- **PLAY** pulses fully between green and gray when both ReShade and the addon are
  detected. The implementation uses a safe layered opacity animation and avoids
  the immutable-brush crash found during preview testing.
- Launch, **Refresh analysis** and **UPDATE ALL** display a dark themed loading
  overlay with operation-specific green status text. Analysis overlays dismiss
  before asynchronous cover-art downloads finish.

## Build identity

- Manager: **1.0.7**, Windows **1.0.7.0**.
- Bundled addon: **1.0.6-slider-reset.1**, Windows **1.0.6.17** (unchanged).
- Addon SHA-256: `8F282AC07D6F430580626A4DA375E2501912C450CF6D22B857406E91874A9622`.

No NVIDIA runtime DLLs are included. `SHA256SUMS.txt` inside the archive covers
the executable and addon; `SHA256SUMS-v.1.0.7.txt` covers the release ZIP.

## Validation

The manager regression suite, release XAML build and real saved-library launch
smoke test pass. Update-only refusal still protects games without the addon, and
the package contains the unchanged validated addon payload. These checks are not
a substitute for live game testing.

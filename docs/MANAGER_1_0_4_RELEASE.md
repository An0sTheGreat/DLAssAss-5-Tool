# DLAssAss 5 Tool v.1.0.4

## Changes since v.1.0.3

- **Install/Reinstall ReShade** now offers two themed choices: **Install
  ReShade Only** and **Install ReShade + Shaders**.
- ReShade Only preserves the existing automatic, shader-free installation.
- ReShade + Shaders opens the official ReShade Setup so the user can select
  shader and add-on packages before completing installation.
- Existing ReShade settings, presets, proxy backups and API selection behavior
  are preserved.

## Build identity

- Manager: **1.0.4**, Windows **1.0.4.0**.
- Bundled addon: **1.0.3**, build **12**, Windows **1.0.3.12**.
- Addon SHA-256: `21C61735076FCB1FC0F439542F34D003F172D91D43253D5C1884EBD90C43714A`.

No NVIDIA runtime DLLs are included. `SHA256SUMS.txt` inside the archive covers
the executable and addon; `SHA256SUMS-v.1.0.4.txt` covers the release ZIP.

## Validation

The release tests cover fresh install and reinstall arguments for both ReShade
installation modes. The Windows x64 release build and package are checked for
the expected versions, pinned addon hash, archive integrity and absence of
NVIDIA runtime DLLs.

This manager release does not change the bundled addon or its known game and
graphics API limitations documented in the README and v.1.0.3 release notes.

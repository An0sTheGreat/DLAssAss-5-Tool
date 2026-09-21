# DLAssAss 5 Tool v1.1.1

## Application changes

- Remove the external DLSS 5 Bridge prompt, download, and installation path.
- Leave any existing `dlss5-bridge.addon64` in a game directory untouched during
  installation, reinstallation, update, and restore.
- Bundle and validate addon 1.1.1.30.

## Addon changes

- Restore live Neural Transfer, Neural Colour, and pass-count changes in
  supported DX11 games without restarting the game.
- Refresh reused private-DX12 command-list identities and retain compatible
  working sets across DX11 pass transitions.
- Allow the working cache to grow from 512 MiB to 1 GiB only when DXGI confirms
  sufficient safe VRAM headroom. The original reserve, set-count limits, and
  512 MiB no-query fallback remain active.

DX11 Present-hook motion-related flickering remains under investigation. This
release does not claim a flicker fix or include DLSS 5 Feeder integration.

## Build identity

- Manager: **1.1.1**, Windows **1.1.1.0**.
- Bundled addon: **1.1.1-dx11-neural-controls.1**, Windows **1.1.1.30**.
- Addon SHA-256: `8F4858A8AF6992794E4C721AC13B5850714EEBDBA8E9E9B93884092603C699AA`.

No NVIDIA runtime DLLs are included. `SHA256SUMS.txt` inside the archive covers
the executable and addon; the release checksum asset covers the ZIP.

## Validation

The manager tests cover install, reinstall, update, backup, restore, and external
Bridge preservation. The addon passed its V6.4-V6.6 regressions, DX11 WARP
lifecycle tests, static release validation, Windows version checks, and Vulkan
export lookup tests. Live game validation remains separate.

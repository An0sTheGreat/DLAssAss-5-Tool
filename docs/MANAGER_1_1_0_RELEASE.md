# DLAssAss 5 Tool v1.1.0

## Changes since v1.0.9

- Prompt for the latest stable official DLSS 5 Bridge during interactive DX11
  add-on installation, with the Bridge covered by normal backup and restore.
- Enable neural resolution, detail, colour, edge, and per-pass controls in DX11
  games after the official DX11 bridge supplies a tracked DX12 evaluation.
- Retain same-generation bridge working resources while Neural Rendering is
  active so reducing the pass count does not trigger live resource cleanup.
- Retain valid host-owned native features across stream generations during an
  active DX11 bridge session, preventing live NVIDIA feature release.

## Build identity

- Manager: **1.1.0**, Windows **1.1.0.0**.
- Bundled addon: **1.1.0-dx11-bridge-retention.2**, Windows **1.1.0.23**.
- Addon SHA-256: `920889CDBCA28128CB68F1F3963768F957AAE523F8C2CA49C232808CFB4C6AB7`.

No NVIDIA runtime DLLs are included. `SHA256SUMS.txt` inside the archive covers
the executable and addon.

## Validation

The full v63-v66 regression suite and guarded release-addon build pass, including
the working-resource and native-feature bridge retention regressions, Windows
release metadata, static hook checks, DX11 lifecycle tests, Vulkan export checks,
and the nested source guard. Live FFXIV testing completed a 1 -> 2 -> 1
transition: generation 7 evaluated successfully, the process remained responsive,
and bridge output copies continued.

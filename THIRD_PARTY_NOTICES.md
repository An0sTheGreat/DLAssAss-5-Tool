# Third-party notices

This project builds on or interoperates with the following projects and SDKs.
Their names and trademarks belong to their respective owners.

## RenoDX

The base RenoDX DLSS add-on is derived from
[RenoDX](https://github.com/clshortfuse/renodx), copyright © 2025 Carlos Lopez
Jr., distributed under the MIT License. The retained license is in
`licenses/RenoDX-MIT.txt`.

## DLSSNR Cost Scaler

The resolution-cost algorithms were adapted from
[DLSSNR Cost Scaler](https://github.com/xenmods/DLSSNR-Cost-Scaler), copyright ©
2026 xen, distributed under the MIT License. The retained license is in
`licenses/DLSSNR-Cost-Scaler-MIT.txt`.

## MinHook

The experimental DX11 bridge and the native-SR duplicate-return guard use
[MinHook](https://github.com/TsudaKageyu/minhook), copyright © 2009–2017 Tsuda
Kageyu and other credited contributors. Its BSD-style license is retained in
`licenses/MinHook.txt` and is also included with binary releases.

## DLSS5 Feeder

The integrated non-DLSS input provider incorporates
[DLSS5 Feeder](https://github.com/artur-graniszewski/DLSS5-Feeder), distributed
under the MIT License. The retained license is in `licenses/DLSS5-Feeder-MIT.txt`.

## LumeniteFX

LumeniteFX is not redistributed. For a supported non-DLSS installation, the
manager downloads a pinned revision directly from the author's GitHub repository,
verifies its SHA-256, and installs only the required Kernel 2.0 files. LumeniteFX
remains subject to its author's license and terms.

## ReShade, NVIDIA, and other SDKs

ReShade, Dear ImGui, the NVIDIA NGX/DLSS SDK, DirectX headers, and Vulkan headers
are external build/runtime dependencies. Binary release archives include the
pinned DLSS runtime files documented in `DLSS Files/README.md`; the NR runtime is
a modified community binary with an Authenticode hash mismatch. Consult each
upstream project or SDK for its applicable terms.

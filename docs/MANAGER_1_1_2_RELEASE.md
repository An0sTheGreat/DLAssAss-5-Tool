# DLAssAss 5 Tool v1.1.2

## Release changes

- Integrate DLSS5 Feeder for detected non-DLSS 64-bit DX11/DX12 games.
- Install and configure LumeniteFX Kernel 2.0 and `DLSS5_MV_PROVIDER=3` through
  the existing non-DLSS ReShade setup path.
- Detect the integrated feeder from the bundled add-on.
- Bind embedded feeder callbacks/configuration directly to the verified ReShade
  module and defer setup until effect-runtime initialization.
- Disable the unsafe duplicate embedded feeder overlay that crashed when Home
  opened ReShade; all nine feeder callbacks remain enabled.
- Bundle and hash-check `nvngx_dlss.dll`, `nvngx_dlssg.dll`, and the labeled
  modified community `nvngx_dlssnr.dll`.

## Build identity

- Manager: **1.1.2**, Windows **1.1.2.0**.
- Bundled add-on: **1.1.2-integrated-feeder.1**, Windows **1.1.2.31**.
- Add-on SHA-256: `7393AB462F9CDA74DA9E1AEEED9DB832D8FCBC65C6CCDF2136455729D680E455`.

## Validation

The add-on passed its V6.6 static checks, DX11 WARP/lifecycle regressions,
Windows version validation, and Vulkan export lookup tests. The manager passed
its discovery, install, reinstall, update, backup, restore, non-DLSS setup, and
external-bridge preservation tests. Skyrim runtime testing confirmed frame
delivery and successful ReShade Home-menu opening with the integrated feeder.

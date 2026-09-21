# DLAssAss 5 Tool

**Version 1.1.1** — [Download the Windows release](../../releases/tag/v1.1.1)

DLAssAss 5 Tool is a local Windows game-library manager for installing the
DLSS 5 Super Anus ReShade add-on, its pinned DLSS runtime files,
installing ReShade with add-on support, and safely restoring replaced files.

## Supported GPUs

| GPU family | Status | Neural Rendering requirement |
| --- | --- | --- |
| ✅ GeForce RTX 50 Series | Supported | Official DLSS 5 Neural Rendering hardware. |
| <img src="assets/compatibility-experimental.svg" width="18" height="18" alt="Orange warning"> GeForce RTX 40 Series | Experimental | Uses the bundled modified community `nvngx_dlssnr.dll`. |
| <img src="assets/compatibility-experimental.svg" width="18" height="18" alt="Orange warning"> GeForce RTX 30 Series | Experimental | Uses the bundled modified community `nvngx_dlssnr.dll`. |

The bundled `nvngx_dlssnr.dll` is a modified community runtime and does not have
a valid NVIDIA Authenticode signature. It is not an official NVIDIA build and
cannot guarantee compatibility with every game, driver, or GPU.
These statuses apply to DLSS 5 Neural Rendering; other DLSS features have their
own hardware requirements.

## Current graphics API compatibility

These statuses describe Neural Rendering in the included add-on. The manager
can detect and install ReShade for additional APIs; that alone does not provide
Neural Rendering support.

| Graphics API | Status | Current scope |
| --- | --- | --- |
| ✅ DirectX 12 | Yes | Primary supported backend, including multipass and 25–150% NR resolution. Compatibility still varies by game. |
| <img src="assets/compatibility-experimental.svg" width="18" height="18" alt="Orange warning"> DirectX 11 | Experimental | Native DLSS SR input capture through a private DX12 consumer; requires compatible inputs and runtime files. |
| <img src="assets/compatibility-experimental.svg" width="18" height="18" alt="Orange warning"> Vulkan | Experimental | Native post-DLSS SR path, limited to one pass at 100% NR resolution and the validated NR runtime. No general non-DLSS Vulkan support. |
| ❌ DirectX 10 | No | No native Neural Rendering backend. |
| ❌ DirectX 9 | No | No native Neural Rendering backend. |
| ❌ OpenGL | No | No native Neural Rendering backend. |

For detected non-DLSS games, the manager enables the integrated DLSS5 Feeder on
64-bit DX11/DX12 only. It requires the official ReShade full add-on build and
standard shader package. The manager downloads the pinned LumeniteFX source from
its author's GitHub repository, verifies its SHA-256, installs Kernel 2.0, and
configures `DLSS5_MV_PROVIDER=3` automatically.

API support does not guarantee correct results in every game. The included addon:
**General frame-gen flickering fixes improved.** The included addon prevents
duplicate NR processing through nested callbacks; this is not a universal
Frame Generation compatibility guarantee.

**Known issues:** DX11 Present-hook motion-related flickering remains unresolved;
this release fixes separate pass-transition/control failures, not the flickering
itself. BOTDW may
freeze after alt-tabbing or leaving it unfocused,
then toggling NR or changing settings. This remains unresolved; see the deferred
[BOTDW Freeze Plan](docs/BOTDW_FREEZE_PLAN.md).

## Addon controls and current improvements

- **Neural Detail and Colour:** Neural Transfer Strength, Neural Color Strength
  and Neural Sharpness now work at all supported DX12 NR resolutions, including
  100%. Transfer 100%, colour 100% and sharpness 0% retain the native first-pass
  bypass at 100% resolution. Non-neutral settings use additional GPU working
  textures/resolve work, with the existing memory and fence protections.
- Encoding and the main **Neural Detail and Colour** section control **Pass 1**
  and restore independently for each preset.
- **Per-Pass Controls:** each additional pass has independent transfer, colour
  and sharpness controls; changes to Pass 1 no longer propagate to later passes.
  Only enabled passes appear. Sections start expanded and remember
  your collapsed/expanded choices across restarts, including temporarily hidden
  passes. Hue-stable detail and colour coupling have been removed from every pass.
- Defaults: colour **100%**, sharpness **0%**, transfer **100%**. Colour ranges
  from 0–200%; values above 100% may oversaturate, leave the display gamut or
  strengthen haloing. Existing saved
  base settings and enabled per-pass overrides are preserved. Old disabled
  overrides start from the new independent defaults. Retired detail/coupling
  keys are ignored. Settings are stored per game in ReShade's
  configuration in the three original preset banks.
- **Multipass Motion** appears below Hook Method. Chained Temporal History is
  recommended and is the default for new configurations; existing saved modes
  remain unchanged. Switching modes safely resets history.
- **Multipass Edge Protection** is preset-scoped and applies only to Pass 2 and
  later. Strength, thickness, softness and a +/-6 pixel shift are adjustable;
  visualization displays the exact combined depth/residual mask. A master switch
  fully bypasses the masking path when disabled.
- **Neural Rendering Enabled On Launch** appears above the native preset and
  options-mode selectors and persists the requested initial state.
- Right-click any native RenoDX or custom Neural Rendering slider and choose
  **Reset** to restore only that slider to its defined default. Resetting Neural
  Rendering Resolution stages 100% and still requires **Apply**.
- Debug, Runtime API, Links and About are the final four sections and start
  collapsed. About shows the addon version and build date/time. Windows Details
  shows addon file version **1.1.1.30** (manager file version **1.1.1.0**).

These image controls are available in DX12 and supported DX11 games. The add-on
does not require the external DLSS 5 Bridge. Vulkan scope is unchanged.
If safe working resources are unavailable, the addon retains native output.

Upgrading the tool does not automatically update installed games. Close the game,
select it in the tool, and choose **Reinstall** to deploy the new bundled add-on.
Existing libraries, settings, and backups retain their current storage location.
**Install/Reinstall ReShade** asks for confirmation, then opens the official Setup
interface for shader and add-on package selection. Existing ReShade proxy backups
are kept beside their originals as `.dlss5manager-*.bak`; settings and presets are
kept.

Compatible sequential passes share a scratch working set while keeping source
views separate and fence-owned. Compact native-resolution controls can recover
after temporary allocation failure; the menu reports effective resolution when
it differs from the request. The cache remains 512 MiB when memory information is
unavailable and may grow to 1 GiB only when DXGI confirms safe headroom. The VRAM
reserve remains. Genuine resource exhaustion can still temporarily bypass controls.

DX11 sessions retain same-generation pooled resources while Neural Rendering is
active. Valid native features that remain in host slots are also retained across
stream generations. Reducing the pass count therefore avoids live NVIDIA feature
release and keeps reusable pass resources available.

At 100% with neutral Pass 1, changing the pass count previously left later passes
on native fallback. The first pass now participates in complete-group transition
tracking, allowing additional-pass controls to resume. Sharpening stays independent
of transfer, and interrupted dependent history resets on managed recovery.

See [v1.1.1 release notes and validation](docs/MANAGER_1_1_1_RELEASE.md).
These changes do not establish that DX11 Present-hook flickering is fixed or guarantee
correct output in every game. Experimental API support is unchanged.

PLAY pulses fully between green and gray only when the selected game has both
ReShade and the add-on installed. It returns to gray otherwise; launching is unchanged.

Loose add-on binaries for manual installation are published in the separate
[`DLSS-5-Super-Anus-Manual`](https://github.com/An0sTheGreat/DLSS-5-Super-Anus-Manual)
repository.

## Features

- One-click **UPDATE ALL** for detected games that already have the addon;
  ReShade, DLSS files, settings and unmodded games remain untouched
- Expandable **UPDATE ALL** results grouped into updated, skipped and failed games
- Persistent window placement and per-game custom library artwork
- Steam discovery and recursive scanning of user-selected drives or folders
- Steam-style cover library and sortable Explorer-style table views
- Graphics API detection using ReShade logs, executable imports, runtime files,
  executable names, and bounded binary evidence
- Explicit API selection for games supporting multiple graphics APIs
- Interactive installation of the latest official ReShade build with add-on
  support and selected packages
- One-click installation of the bundled add-on and pinned DLSS runtime files
- Automatic integrated feeder, Lumenite Kernel 2.0, and ReShade preset setup for
  detected non-DLSS 64-bit DX11/DX12 games
- Per-game backups and one-click restoration of the latest installation
- Game launching, folder access, renaming, hiding, and persistent view settings
- Steam and GOG cover lookup with executable-icon fallback and local caching

## Requirements

- 64-bit Windows 10 or Windows 11
- A game supported by the included ReShade add-on
- Internet access for ReShade, LumeniteFX, and game-cover discovery

Release archives include the pinned DLLs listed in `DLSS Files/README.md`.

## Installation

1. Download `DLAssAss-5-Tool-win-x64.zip` from
   [GitHub Releases](../../releases/latest).
2. Extract the complete archive to a writable folder.
3. The included `DLSS Files` folder contains:

   | File | Purpose |
   | --- | --- |
   | `nvngx_dlss.dll` | DLSS Super Resolution |
   | `nvngx_dlssg.dll` | DLSS Frame Generation |
   | `nvngx_dlssnr.dll` | DLSS Ray Reconstruction / Neural Rendering |

4. Start `DLAssAss 5 Tool.exe`. The status bar confirms the bundled files are present.

## Using the tool

1. Select **Scan Steam** to find Steam games automatically, or select
   **Add Search Directory** to scan another drive or folder.
2. Select a game in Library View or Folder View.
3. Review the detected executable, graphics API, ReShade state, add-on state,
   and available DLSS features.
4. For native-DLSS games, install ReShade in either mode. For non-DLSS games,
   the tool requires ReShade + Shaders and prompts you to select the standard
   shader package in official ReShade Setup.
5. Select **Install** to install the add-on and bundled DLSS DLLs. Non-DLSS
   64-bit DX11/DX12 games are also configured with Lumenite Kernel 2.0 and DLSS 5 Feed.
6. Select **Play** to launch the detected game executable.

After non-DLSS setup, open ReShade with **Home** and confirm **DLSS 5 Feed** is
enabled immediately below **LUMENITE: Kernel 2.0**. This order is required; the
motion-vector provider is already configured by the tool.

ReShade installation uses the latest official full add-on build available from
`reshade.me`, configures the selected API, and opens official Setup for package
selection.

## Backups and restoration

Before replacing a managed file, the tool creates a per-game backup under its
local application-data directory. Select **Restore Latest** to restore the most
recent manager backup for the selected game.

Settings, logs, backups, and cached covers are stored under
`%LOCALAPPDATA%\DLSS5ManAger`. The legacy directory name is intentionally kept
so upgrades preserve existing game libraries and backups.

## Important notes

- Use ReShade's full add-on build only where appropriate; avoid multiplayer or
  anti-cheat-protected games unless the game explicitly permits it.
- Graphics API detection is evidence-based. Confirm the selected API when a game
  offers multiple renderers.
- Cover lookup may require a few moments after initial game discovery.
- The bundled NR runtime is modified community software; review the included
  hashes and notices before use.

## Building from source

Install the .NET 8 SDK on Windows, then run:

```powershell
dotnet build .\DLAssAss5Tool.csproj -c Release
dotnet run --project .\tests\DLAssAss5Tool.Tests.csproj -c Release
```

To create a self-contained release, place the verified add-on in `Payload` and
the three pinned runtime DLLs in `DLSS Files`, then run:

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\publish.ps1
```

The publisher validates the add-on and all three runtime DLLs against pinned
SHA-256 values, includes them in the archive, and writes all release output to
the workspace-level `artifacts` folder.

Public releases use two paired repositories: this repository publishes the
complete manager application, while `DLSS-5-Super-Anus-Manual` publishes only
the matching loose `.addon64` file. See
[Release channels](docs/RELEASE_CHANNELS.md).

Third-party attribution is available in [THIRD_PARTY_NOTICES.md](THIRD_PARTY_NOTICES.md).

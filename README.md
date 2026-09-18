# DLAssAss 5 Tool

**Version 1.0.9** — [Download the Windows release](../../releases/tag/v.1.0.9)

DLAssAss 5 Tool is a local Windows game-library manager for installing the
DLSS 5 Super Anus ReShade add-on, supplying your own NVIDIA DLSS runtime files,
installing ReShade with add-on support, and safely restoring replaced files.

## Supported GPUs

| GPU family | Status | Neural Rendering requirement |
| --- | --- | --- |
| ✅ GeForce RTX 50 Series | Supported | Official DLSS 5 Neural Rendering hardware. Supply a compatible `nvngx_dlssnr.dll`. |
| <img src="assets/compatibility-experimental.svg" width="18" height="18" alt="Orange warning"> GeForce RTX 40 Series | Experimental | Requires the community-patched `nvngx_dlssnr.dll` available from the RenoDX Discord server. |
| <img src="assets/compatibility-experimental.svg" width="18" height="18" alt="Orange warning"> GeForce RTX 30 Series | Experimental | Requires the community-patched `nvngx_dlssnr.dll` available from the RenoDX Discord server. |

The RTX 30/40 runtime patch is unofficial, is not bundled or downloaded by this
project, and cannot guarantee compatibility with every game, driver or GPU.
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

API support does not guarantee correct results in every game. The included addon:
**General frame-gen flickering fixes improved.** The included addon prevents
duplicate NR processing through nested callbacks; this is not a universal
Frame Generation compatibility guarantee.

**Known issues:** Cyberpunk shimmering remains unresolved; this release fixes a
separate pass-transition/control failure, not the shimmering itself. BOTDW may
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
  shows addon file version **1.0.9.19** (manager file version **1.0.9.0**).

These new image controls are DX12-only. Experimental DX11/Vulkan scope is unchanged.
If safe working resources are unavailable, the addon retains native output.

Upgrading the tool does not automatically update installed games. Close the game,
select it in the tool, and choose **Reinstall** to deploy the new bundled add-on.
Existing libraries, settings, and backups retain their current storage location.
**Install/Reinstall ReShade** offers ReShade Only, which uses Setup's automatic
update operation without shader packages, or ReShade + Shaders, which opens the
official Setup interface for shader and add-on package selection. Existing ReShade
proxy backups are kept beside their originals as `.dlss5manager-*.bak`; settings
and presets are kept.

Compatible sequential passes share a scratch working set while keeping source
views separate and fence-owned. Compact native-resolution controls can recover
after temporary allocation failure; the menu reports effective resolution when
it differs from the request. The 512 MiB cache cap, VRAM reserve and safe fallback
remain. Genuine resource exhaustion can still temporarily bypass controls.

At 100% with neutral Pass 1, changing the pass count previously left later passes
on native fallback. The first pass now participates in complete-group transition
tracking, allowing additional-pass controls to resume. Sharpening stays independent
of transfer, and interrupted dependent history resets on managed recovery.

See [v.1.0.9 release notes and validation](docs/MANAGER_1_0_9_RELEASE.md).
These changes do not establish that Cyberpunk shimmer is fixed or guarantee
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
- Installation of the latest official ReShade build with add-on support, either
  automatically without shaders or interactively with selected packages
- One-click installation of the bundled add-on and validated user-supplied DLSS
  files
- Per-game backups and one-click restoration of the latest installation
- Game launching, folder access, renaming, hiding, and persistent view settings
- Steam and GOG cover lookup with executable-icon fallback and local caching

## Requirements

- 64-bit Windows 10 or Windows 11
- A game supported by the included ReShade add-on
- NVIDIA DLSS DLLs supplied by the user
- Internet access for ReShade version checks and game-cover discovery

The tool does not download or redistribute NVIDIA DLLs.

## Installation

1. Download `DLAssAss-5-Tool-win-x64.zip` from
   [GitHub Releases](../../releases/latest).
2. Extract the complete archive to a writable folder.
3. Open the included `DLSS Files` folder.
4. Add the NVIDIA DLLs you are legally permitted to use:

   | File | Purpose |
   | --- | --- |
   | `nvngx_dlss.dll` | DLSS Super Resolution |
   | `nvngx_dlssg.dll` | DLSS Frame Generation |
   | `nvngx_dlssnr.dll` | DLSS Ray Reconstruction / Neural Rendering |

5. Start `DLAssAss 5 Tool.exe`. The status bar confirms each valid DLL and
   identifies missing or mismatched files.

## Using the tool

1. Select **Scan Steam** to find Steam games automatically, or select
   **Add Search Directory** to scan another drive or folder.
2. Select a game in Library View or Folder View.
3. Review the detected executable, graphics API, ReShade state, add-on state,
   and available DLSS features.
4. If ReShade is missing, select **Install ReShade**, choose ReShade Only or
   ReShade + Shaders, then choose the intended API for a multi-API game.
5. Select **Install** to install the included add-on and every validated DLSS
   DLL currently available in `DLSS Files` beside the selected game executable.
6. Select **Play** to launch the detected game executable.

ReShade installation uses the latest official full add-on build available from
`reshade.me` and configures the selected API. ReShade Only installs no shaders;
ReShade + Shaders opens official Setup for package selection.

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
- NVIDIA DLLs in `DLSS Files` and add-on payload binaries are ignored by Git.

## Building from source

Install the .NET 8 SDK on Windows, then run:

```powershell
dotnet build .\DLAssAss5Tool.csproj -c Release
dotnet run --project .\tests\DLAssAss5Tool.Tests.csproj -c Release
```

To create a self-contained release, place the verified
`renodx-dlss5-super-anus.addon64` in `Payload`, then run:

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\publish.ps1
```

The publisher refuses to include NVIDIA DLLs and validates the add-on payload
against its pinned SHA-256 before creating the archive.

Public releases use two paired repositories: this repository publishes the
complete manager application, while `DLSS-5-Super-Anus-Manual` publishes only
the matching loose `.addon64` file. See
[Release channels](docs/RELEASE_CHANNELS.md).

Third-party attribution is available in [THIRD_PARTY_NOTICES.md](THIRD_PARTY_NOTICES.md).

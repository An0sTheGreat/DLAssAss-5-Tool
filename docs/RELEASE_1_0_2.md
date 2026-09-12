# DLAssAss 5 Tool v.1.0.2

- General frame-gen flickering fixes improved.
- Added independent Neural Transfer Strength, Neural Color Strength and Neural Sharpness under Neural Detail and Colour. These work at every supported DX12 NR resolution, including 100%.
- Added customizable reconstruction/output controls for additional neural passes. Existing settings are inherited until customized.
- Per-Pass Controls and newly enabled pass sections start expanded. Your expanded/collapsed choices persist across restarts and pass-count changes.
- Removed the global hue-stable detail and colour coupling sliders. Their old global values are ignored; these controls and saved values remain available within individual passes.
- Reordered the ReShade menu. Debug, Runtime API, Links and About are the final four sections, initially collapsed.
- Added addon version and build date/time to About and Windows file properties.

The new image controls are **DX12-only**. Experimental DX11 and Vulkan support is unchanged. The manager's game discovery, installation, backup and restore behavior is unchanged.

## Updating

Extract the complete manager ZIP. Put your legally obtained NVIDIA DLLs in **DLSS Files**, then open the tool. Close each game and use **Install** to deploy the updated addon beside its executable. Updating the tool alone does not update installed games. Existing settings and backups retain their storage location; no NVIDIA DLLs are bundled.

At 100% NR resolution, transfer 100%, colour 100% and sharpness 0% preserve the original native first pass. Non-neutral values use the existing protected working-resource path. Existing saved sharpness values now apply at 100% too. Unsupported inputs or insufficient safe resources can retain native output.

## Known issue

BOTDW may freeze after alt-tabbing or leaving it unfocused and then toggling NR or changing settings. This is **not fixed** in this release. The investigation is deferred in [BOTDW Freeze Plan](BOTDW_FREEZE_PLAN.md).

## Build and validation

- Manager: **1.0.2**. Addon: **1.0.3-pass-controls.4**, Windows file/product version **1.0.3.4**.
- Addon About: `Build: V1.0.3 | D: 2026-09-12 | T: 14:28:45`.
- Addon SHA-256: `4213C8D2C820891448A33225C6E1EA592F760194FFFE5A5C167FC2C3CD54B9FD`.
- UI: 3,780 slider interactions across 25–150% applied/staged resolution, 432 layout frames, all ten section headers toggled/saved/restored, hidden-pass retention and retired-global-value checks.
- Production GPU shader: 495 resolve cases plus 45 per-pass detail/coupling cases on WARP and hardware, including 100% direct colour and sharpness.
- Exact addon loaded in two isolated live SR/NR fixtures: 390 successful NR evaluations each, native-size independent controls and 50–150% scale transitions, preset/off/on and multipass sequences; 10,240 synthetic FrameGen callbacks per fixture with no callback-side NR.
- Existing GPU-fence/resource lifetime, native-SR duplicate guard, DX11 lifecycle and Vulkan export regressions passed. Native Windows version/resource validation passed.
- Manager path/discovery/API/cover/preferences/install/backup/restore regressions passed with .NET 8.

Synthetic callbacks do not generate interpolated game frames. These tests are not new in-game acceptance or a guarantee for every game/hardware combination.

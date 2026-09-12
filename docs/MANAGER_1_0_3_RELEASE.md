# DLAssAss 5 Tool v.1.0.3

## Changes since v.1.0.2

- Fixed pass-count changes leaving additional passes stuck on native fallback
  at 100% NR resolution when Pass 1 uses neutral controls. Pass 1 now participates
  in complete-group transition tracking before taking its native fast path.
- Pass 1 and additional passes have independent transfer, colour and sharpness
  controls at supported DX12 resolution scales. Per-pass hue-stable detail and
  colour coupling are removed. Defaults are transfer 100%, colour 50%, sharpness
  0%; saved active overrides and section expansion preferences are preserved.
- Sequential passes share scratch resources with separate source views and real
  fence ownership. Compact native controls recover after temporary allocation
  failures when resources become available. Effective resolution is shown when
  safe fallback differs from the requested scale.
- Sharpening remains independent of transfer and uses the incoming pass image
  rather than reintroducing raw neural detail. Interrupted dependent histories
  reset when managed processing resumes, not every frame.
- Installed games offer **Reinstall ReShade** and **Reinstall** actions. ReShade
  uses its official update operation without shaders, preserves settings, and
  retains verified proxy backups. The PLAY button has a soft green border pulse
  when both ReShade and the addon are installed.

## Updating

Extract the complete `DLAssAss-5-Tool-win-x64.zip`. Place your legally obtained
NVIDIA runtime DLLs in **DLSS Files**. Close the game, select it in the tool,
and choose **Reinstall** to deploy the updated addon beside its executable.
Updating the tool alone does not update installed games. Reinstalling ReShade
is not required just to update the addon. No NVIDIA DLLs are included.

Existing libraries, settings and backups keep their current storage location.
The installer continues to use the selected executable's folder, including
games with deeply nested executable directories.

## Known limitations

**Cyberpunk shimmering remains unresolved.** The unchanged official addon and
our addon reproduce the same temporal variation with a controlled frozen input
and identical SR/NR runtime DLLs. All 60 output hashes matched in the one-pass
comparison. That synthetic result does not prove the game's exact cause or a
vendor defect. This release fixes the separate pass-transition/control failure;
it does not claim to fix shimmer and does not force a history reset every frame.

BOTDW's deferred focus-change freeze remains unresolved; see
[BOTDW Freeze Plan](BOTDW_FREEZE_PLAN.md). The user previously reported its
normal rendering working; the tests below are local regressions, not new game
acceptance. DX11 and Vulkan remain experimental with their existing limits.
VRAM exhaustion can still temporarily preserve native output instead of applying
controls. No universal game/hardware compatibility guarantee is made.

## Build identity

- Manager: **1.0.3**, Windows **1.0.3.0**.
- Addon: **1.0.3**, build **12**, Windows file/product **1.0.3.12**, non-prerelease.
- Runtime ID: `1.0.3-manager-release.3`.
- About: `Build: V1.0.3 | D: 2026-09-12 | T: 18:46:16`.
- Addon SHA-256: `21C61735076FCB1FC0F439542F34D003F172D91D43253D5C1884EBD90C43714A`.

`SHA256SUMS.txt` inside the archive covers the executable and addon. The separate
`SHA256SUMS-v.1.0.3.txt` release asset covers the ZIP. The publisher validates the
pinned payload hash and refuses to overwrite existing local release outputs.

## Validation scope

The transition regression fails before the fix and passes afterward through the
actual production entry path with a mocked vendor call. The same implementation
passes existing lifecycle/WARP fence, history, UI/configuration, nested-source,
DX11 isolation and Vulkan export/ordinal tests.

Hardware and WARP production-shader checks passed: 495 resolves, 24 three-pass
chains, and 63 independent final-image comparisons at 25/70/99/100/101/125/150%.
These shader inputs are synthetic, not game imagery.

The exact release binary is checked separately in real-NR local fixtures:

- Neutral first-pass 1→2→1: 120 evaluations, 28 second-pass resolves, five
  transition-native calls, 20,000 NR-free synthetic FrameGen callbacks.
- BOTDW-shaped two passes at 70%: 120 evaluations, 114 resolves and 20,000
  NR-free synthetic FrameGen callbacks.
- Independent-control recovery after simulated memory pressure: each pass's
  transfer, colour and sharpness changes the final output after recovery.

These fixtures do not generate interpolated frames or certify live game output.
Stable Windows version metadata and original resource-manifest preservation
are verified. Investigation-only input traces are excluded from the release.

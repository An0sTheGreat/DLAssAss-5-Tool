# Changelog

## Unreleased

- Detect the embedded feeder capability directly from the installed add-on, so
  its status is correct for every game independently of activation state.
- Determine native DLSS support from game/engine NGX integration evidence rather
  than treating any loose `nvngx_dlss.dll` as native support.
- Hide Integrated Feed setup status for native-DLSS DX12 games while keeping
  the feeder embedded and available silently; retain the status for DX11.

## [v1.1.2] - 2026-09-21

- Integrate DLSS5 Feeder into the add-on and gate it behind the manager's
  non-DLSS marker for supported 64-bit DX11/DX12 games.
- Require ReShade's add-on build and standard shader package for non-DLSS setup;
  download and verify pinned LumeniteFX Kernel 2.0 files, enable Kernel then
  DLSS 5 Feed, and configure motion-vector provider 3 automatically.
- Bundle and hash-check the three pinned DLSS runtime files in future builds.
  The modified community `nvngx_dlssnr.dll` is labeled explicitly.
- Replace the ReShade install-mode chooser with one Yes/No confirmation and
  always open official Setup for interactive package selection.
- Keep legacy non-DLSS games on the integrated-feed path when their DLSS SR DLL
  came from the manager, and identify incomplete feed setup as needing repair.
- Defer integrated-feeder configuration until effect-runtime startup and bind its
  ReShade callbacks, configuration and logging directly to the verified local module.
- Disable the integrated feeder's duplicate overlay, preventing a crash when Home
  opens ReShade while retaining all nine feeder callbacks and frame delivery.

## [v1.1.1] - 2026-09-20

- Remove the external DLSS 5 Bridge prompt, download, and installation workflow.
  Existing bridge files in game folders are left untouched.
- Bundle addon 1.1.1.30 with restored live Neural Transfer, Neural Colour, and
  pass-count changes in supported DX11 games.
- Refresh reused private-DX12 command-list identities and retain compatible
  working sets across DX11 pass transitions.
- Allow the addon cache to grow from 512 MiB to 1 GiB only when DXGI confirms
  sufficient safe VRAM headroom, preventing false Skyrim multipass failures.

## [v1.1.0] - 2026-09-19

- Prompt DX11 users during interactive add-on installation to download and
  transactionally install the latest stable official DLSS 5 Bridge release.
- Enable the neural resolution, detail, colour, edge, and per-pass controls for
  DX11 games after a tracked evaluation from the official DX11 bridge.
- Keep same-generation bridge working resources cached while Neural Rendering
  remains active, preventing live cleanup when reducing the pass count.
- Retain valid host-owned native features during active DX11 bridge sessions,
  avoiding NVIDIA feature release during live 1 -> 2 -> 1 pass transitions.

## [v.1.0.9] - 2026-09-17

- Add a persistent launch option that starts Neural Rendering enabled or disabled.
- Add Chained Temporal History and make it the recommended default for new
  configurations while retaining existing saved motion selections.
- Add a master switch that completely bypasses multipass edge masking.
- Place the custom launch option above RenoDX's native preset and options-mode
  selectors without renaming those native controls.

## [v.1.0.8] - 2026-09-15

- Bundle addon 1.0.8.18 with preset-scoped Encoding, Neural Detail and Colour,
  and experimental multipass edge protection controls.
- Add adjustable edge protection strength, thickness, softness, inward/outward
  shift, and mask visualization for Pass 2 and later.
- Fix multipass image freezing/black output caused by edge-shader GPU load and
  a root-constant layout mismatch.
- Preserve the TLOU2 nested Frame Generation source guard and fix multipass
  NR ON/OFF screenshots so the OFF image contains zero NR passes.

## [v.1.0.7] - 2026-09-13

- Remember the last restored window size and position, including when closing
  maximized, and keep restored placement within the available desktop.
- Add managed per-game custom artwork from the library context menu and round
  game covers and major interface sections consistently.
- Highlight **UPDATE ALL** when multiple detected installations are eligible.
  Its themed completion dialog now expands into matching Updated, Skipped and
  Failed bullet lists while full install paths remain in **SETTINGS & LOG**.
- Pulse the complete **PLAY** button between green and gray when ReShade and the
  addon are installed. Fix the preview crash caused by animating an immutable
  WPF brush and reset the green layer when selection eligibility changes.
- Show a dark themed loading overlay while launch/refresh analysis or Update All
  is active. Analysis overlays dismiss before asynchronous cover downloads.

## [v.1.0.6] - 2026-09-13

- Default every unsaved Pass 1 and additional-pass Neural Color Strength slider
  to 100%. Existing saved values remain unchanged.
- Expand Neural Color Strength to 0–200% and warn on hover that values above
  100% may oversaturate, leave the display gamut or strengthen haloing.
- Add a themed right-click **Reset** menu to every native RenoDX and custom
  Neural Rendering slider. It resets only the selected slider through the
  existing save path; resolution stages 100% until **Apply** is pressed.

## [v.1.0.5] - 2026-09-12

- Add a themed **UPDATE ALL** button beside the detected-game count. It updates
  only installations where the addon is already present, deduplicates shared
  destinations, backs up each previous addon and reports partial failures.
- Bulk update never installs into an unmodded game and does not change ReShade,
  NVIDIA DLSS files, settings or presets.
- Add a persistent in-game Multipass Motion selector below Hook Method. Reuse
  Game Motion is the recommended default; legacy zero motion and diagnostic
  zero-motion/reset behavior remain selectable.
- Reset pass history safely after changing the motion mode. User testing reports
  major ghosting/haloing improvements in BOTDW, TLOU2 and Cyberpunk. Cyberpunk's
  separate two-pass shadow shimmer remains unresolved.

## [v.1.0.4] - 2026-09-12

- Offer themed ReShade Only and ReShade + Shaders choices for install and
  reinstall. The existing automatic path remains shader-free; the second opens
  official ReShade Setup for interactive shader and add-on package selection.

## [v.1.0.3] - 2026-09-12

- Fix pass-count changes leaving later passes on native fallback at 100% NR
  resolution when Pass 1 uses neutral controls. Every pass now participates in
  transition tracking, including the native first-pass fast path.
- Give Pass 1 and each additional pass independent transfer, colour and sharpness
  controls across supported DX12 resolution scales. Remove per-pass hue-stable
  detail/coupling; default colour to 50% and sharpness to 0%, preserving saved
  active overrides and expanded/collapsed section preferences.
- Share sequential-pass scratch resources and recover independent controls after
  temporary allocation failure, retaining VRAM limits and real GPU-fence ownership.
  Report effective resolution when safe allocation requires native fallback.
- Keep sharpening independent of transfer, without reintroducing raw neural detail;
  invalidate interrupted dependent history when managed processing resumes.
- Add Reinstall ReShade and Reinstall actions with existing settings/backups kept,
  and a soft green PLAY border when both ReShade and the addon are installed.
- Bundle release addon 1.0.3, build 12 (Windows 1.0.3.12). Validate transitions,
  per-pass controls, 25–150% scaling, and lifecycle/API safeguards locally.
- Cyberpunk shimmering remains unresolved. The official addon reproduces the
  frozen-input temporal variation too; this update is not a shimmer fix.
- BOTDW's deferred focus-change freeze and experimental DX11/Vulkan limits remain.
  Updating the manager alone does not update games; close each game and Reinstall.

## [1.0.2-local.9] - 2026-09-12 (unpublished preview)

- Correct direct sharpening reintroducing neural noise after colour/transfer adjustment.
- Keep sharpening functional at zero neural transfer, using the incoming pass image.
- Invalidate interrupted/dependent pass history for recovery, preserving steady native
  first-pass history and existing memory/fence safeguards.
- Add SDR/HDR sharpening isolation and Cyberpunk-format 2 -> 1 -> 2 checks.
- Bundle addon 1.0.3-pass-controls.9. Saved controls and motion handling are unchanged;
  Cyberpunk visual acceptance remains outstanding. No published release is changed.

## [1.0.2-local.8] - 2026-09-12 (unpublished preview)

- Remove permanent control bypass after compact working-texture allocation fails.
- Keep trying existing fence-safe resources; retry new allocations after 250 ms.
- Ignore unused depth/UI working formats when matching compact native scratch.
- Reproduce Cyberpunk's larger motion format and logged memory-pressure conditions;
  verify recovery without scale/hook changes and read back final per-pass output.
- Preserve control math, motion handling, memory caps, real fences and manager
  behavior. Bundle addon 1.0.3-pass-controls.8; Cyberpunk acceptance still required.
- No installed games or published releases changed.

## [1.0.2-local.7] - 2026-09-12 (unpublished preview)

- Use compact native-resolution resources for per-pass controls at 100%.
- Keep transfer, colour and sharpness active through scaling admission fallback
  when the smaller native-control allocation fits; retain all memory/fence limits.
- Show effective NR resolution when it differs from the requested scale.
- Add deterministic pressure/transition tests and final-image comparisons for
  each control on each of three passes across 25–150%.
- Bundle addon 1.0.3-pass-controls.7. Cyberpunk acceptance still required; no
  published release, installed game or unrelated manager behavior changed.

## [1.0.2-local.6] - 2026-09-12 (unpublished preview)

- Fix reproduced multipass working-cache exhaustion: compatible sequential passes
  share scratch textures within one command recording, retaining separate immutable
  source views and existing real-fence retirement and memory limits.
- Keep independent transfer, colour and sharpness for Pass 1 and each later pass.
  Add per-pass resolve diagnostics; genuine resource shortages still fall back safely.
- Add a soft green PLAY border pulse only when ReShade and the addon are installed
  for the selected game. Remove the border otherwise; launch behavior is unchanged.
- Bundle addon 1.0.3-pass-controls.6. Cyberpunk acceptance remains outstanding;
  BOTDW focus-change freeze remains deferred. No public release is replaced.

## [1.0.2-local.5] - 2026-09-12 (unpublished preview)

- Enable Reinstall ReShade and Reinstall actions for installed games. ReShade
  uses the official update operation, keeps settings and saves proxy backups.
- Make the main detail/colour section control Pass 1 only; additional passes
  have independent transfer, colour and sharpness without an inheritance toggle.
- Remove per-pass hue-stable detail/coupling and ignore their retired values.
- Default colour to 50% and sharpness to 0%; preserve saved active overrides.
- Drain the old cache through real fences before admitting a larger pass group.
  Keep memory limits and fallback; Cyberpunk acceptance is not yet confirmed.
- Keep expanded/collapsed state, FrameGen protections and experimental API scope.
- Bundle addon 1.0.3-pass-controls.5. Public v.1.0.2 remains unchanged.

## [v.1.0.2] - 2026-09-12

- General frame-gen flickering fixes improved.
- Prevent nested DLSS callbacks from applying Neural Rendering twice to the
  same SR evaluation when the application frame counter changes between returns.
- Preserve configured multipass, NR resolution adjustments, separate source
  evaluations, retry behavior, and existing GPU resource protections.
- Exclude investigation-only Streamline/tag/queue probes from the release addon.
- Add customizable reconstruction/output controls for additional DX12 neural
  passes. Per-Pass Controls and new pass subsections start expanded; each section
  remembers its collapsed/expanded state across restarts and pass-count changes.
- Group Neural Transfer Strength, Neural Color Strength and Neural Sharpness in
  Neural Detail and Colour. All three work at every supported DX12 NR resolution,
  including 100%. Reconstruction Mode remains in Performance for scaled NR.
- Remove global hue-stable detail/coupling controls and ignore their old saved
  values. Explicit per-pass detail/coupling controls and saved values remain.
- Place Performance before Advanced/pass count and Per-Pass Controls. Debug,
  Runtime API, Links and About remain the final four, initially collapsed sections.
- Add current addon version/date/time to About and native Windows Details
  metadata (addon file version 1.0.3.4).
- Keep the manager's installation, backup, settings, and DLL handling unchanged.
  Reinstall the add-on for each game to apply the update.
- Known issue: BOTDW may freeze after alt-tab/unfocused rendering followed by NR
  toggles or settings changes. The freeze investigation is deferred, not fixed.

## [v.1.0.1] - 2026-09-11

- Install and restore the add-on and supplied DLSS files beside the selected
  game executable, including games with deeply nested binary directories.
- Detect ReShade, the add-on, and DLSS files in the executable's actual folder
  and reject add-on installation when ReShade is not present there.
- Update the included add-on with upstream FrameGen multipass routing, stable
  scaled-resource admission, and the ordinal-safe export lookup fix.
- Preserve GPU fence protections and hold at native 100% after genuine resource
  admission failure instead of repeatedly alternating render dimensions.

## [v.1.0.0] - 2026-09-11

- Initial public release of DLAssAss 5 Tool.
- Added game discovery, cover-art library and sortable folder views.
- Added graphics API detection and explicit multi-API selection.
- Added official ReShade installation without shader packages.
- Added validated user-supplied DLSS file handling.
- Added add-on installation, backups, restoration, and game launching.

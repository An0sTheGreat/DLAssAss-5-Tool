# Sustained multipass controls / manager local preview 6

Date: 2026-09-12. Local only. No installed game or published release changed.

## Reproduced failure

Cyberpunk's supplied 15:22 log loads pass-controls.5/config8 and begins with two
passes at 100%, 3440x1440. Adjusted passes initially succeed, then complete-group
reservation fails at 15:22:41.546 and native fallback bypasses the resolve controls.
The previous pass-count-increase drain alone cannot fix startup at two passes.

A production NR host reproduces this with three rotating SR outputs and three
command recordings in flight. Preview 5 performs 120 NR evaluations but only six
adjusted resolves before falling back. Results are preserved in
`build/framegen-fixture-pass5-pressure` (expected regression failure, exit 21).

## Change

- Compatible sequential passes share one scratch working set in the same command
  recording. Each pass snapshots its own input and resolves its own output using
  its independent transfer, colour and sharpness values. No extra NR calls added.
- Each source binding's views remain immutable and retire with the scratch owner.
  Sharing checks recording pins, pass order, frame/output continuity, device,
  allocation generation, dimensions and formats. Incompatible layouts retain the
  existing per-pass allocation path. Destroyed extra source resources invalidate
  the owner without discarding recording pins or unfinished fences.
- Keep separate pass histories, zero motion after Pass 1, the 512 MiB cache limit,
  VRAM reserve, pass-count transition drain and real queue-fence retirement.
- Log the actual resolve settings once per pass/stream generation for diagnosis.
- PLAY gets a soft green border pulse only when both ReShade and the addon are
  installed for the selection. It disappears otherwise; launching is unchanged.
  Existing WPF animation/style infrastructure is used, without a new dependency.

## Validation

- Final addon: three passes at 100%, 3440x1440, 60 frames, three rotating outputs
  and three recordings in flight: 180 NR evaluations, 168 adjusted resolves.
  After the startup drain, every checked frame resolves every configured pass.
- Two passes at 125%: 120 NR evaluations, 114 adjusted resolves; same strict
  per-frame check. Earlier preview-6 candidate (before diagnostic log adjustment)
  also passed two passes at 80% and 100%, and three passes at 100%.
- Final 400-frame scale/preset sweep across 50–150%: 390 actual NR evaluations,
  364 adjusted resolves, 26 transition-native evaluations. 10,240 synthetic FG
  callback forwards preserved; no NR inside those FG callbacks.
- Ten passes at 100%, 3440x1440 did NOT pass: all ten resolves recorded initially,
  then working-texture admission fell back and the host's 10-second GPU wait
  expired (exit 8). This high-pressure case remains unvalidated; its cause beyond
  the recorded admission/wait failure is not established. Results are retained in
  `build/framegen-fixture-pass6-final-ten`. No memory/fence protections were relaxed.
- Hardware and WARP each pass 495 production shader resolves, 24 three-pass
  transfer/colour chains and 45 retained internal detail/coupling shader cases.
  Tests cover sharpness, alpha, signed HDR and earlier-pass preservation.
- Recording/lifetime/multi-queue fence tests and additional-source invalidation
  test pass. DX11 rollback/registry/private-cleanup, nested source-boundary,
  ordinal export, PE/version and ImGui/configuration regressions pass.
- Manager tests parse the actual PLAY border XAML and check absent, ReShade-only,
  addon-only, both installed and selection-change states. Existing install,
  reinstall, backup, restore, discovery and API tests pass.

These are fixture and shader tests, not Cyberpunk game acceptance or real generated
frame validation. Genuine resource exhaustion can still select native fallback;
this does not promise unlimited passes/resolutions on every GPU. Experimental API
scope is unchanged. The BOTDW focus-change freeze plan remains deferred.

## Identity

- Addon: `1.0.3-pass-controls.6`, Windows version `1.0.3.6` (prerelease).
- SHA-256: `3C4F2B06F66BF42BBB0AE32BAD1F5D5E3FC5640A5171911E540B5AD7D294E22B`
- Manager: `1.0.2-local.6`, Windows version `1.0.2.6`.
- Package: `DLAssAss-5-Tool-local-6-win-x64.zip`. Executable/addon hashes are in
  `SHA256SUMS.txt`; the ZIP has a separate checksum sidecar. NVIDIA DLLs are not
  bundled; `DLSS Files/README.md` explains where users supply them.
- Close the game before using the manager's **Reinstall** action to update it.

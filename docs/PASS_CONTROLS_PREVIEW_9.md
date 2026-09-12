# Sharpening isolation and interrupted history / local preview 9

2026-09-12. Local package only; no installed game or published release changed.

## Evidence and scope

The supplied Cyberpunk 2077 log identifies 1.0.3-pass-controls.8. At
17:00:22.919 an 82624 KiB compact allocation is rejected with zero admissible
headroom. At 17:00:35.239 both passes recover at 100%, confirming preview 8's
recovery path works in that run. The separate shimmer is not explained away
by successful evaluation counters.

Frame samples from the supplied HDR recording were tone-mapped for inspection.
The visible settings include two passes at 100%, with 25% sharpness on both.
This does not prove sharpening is the only cause of the recorded shimmer.
BOTDW is reported working and is a regression case, not the reported problem.

## Reproduced defects and correction

1. Direct-mode sharpening took its high-frequency detail from raw NR output,
   after the transfer/colour resolve. A GPU test with alternating zero-luminance
   neural chroma proves that 25% sharpening reintroduces colour already rejected
   by Colour Strength = 0. This failed on the previous shader in SDR and HDR.
   Direct mode now reuses the matched-residual path's incoming-image spatial
   reference. No new denoiser, blur, history accumulation or motion logic is added.
2. Transfer Strength = 0 returned before sharpening, disabling the independent
   Sharpness slider. The shader now bypasses only the neural edit in that case.
   Transfer = 0 and Sharpness = 0 still preserves the input exactly, including alpha.
3. History bookkeeping tracked configuration generations but did not invalidate
   dependent passes when evaluation was bypassed, suppressed for allocation, or
   failed. Such discontinuities now mark the affected and later pass histories
   for reset when managed processing resumes. No feature release, wait, fence
   shortcut or GPU ownership change is introduced. A continuously neutral first
   pass does NOT repeatedly invalidate the second pass.

Transfer/colour resolve arithmetic and zero-sharpness image processing are
unchanged. Direct-mode output with nonzero sharpening intentionally changes.
Matched-residual sharpening with nonzero transfer retains its existing method.
Saved settings, defaults, per-pass UI, motion vectors and experimental API scope
are unchanged. Existing memory caps and safe fallback remain.

## Validation

- Hardware and WARP: the new alternating-chroma and zero-transfer sharpening
  tests pass in both resolve modes in SDR/HDR. Prior shader failed eight checks.
- Existing production shader coverage passes: 495 resolves, 24 three-pass chains,
  63 independent final-image comparisons across 25/70/99/100/101/125/150%, and
  45 retained shader cases. These use synthetic neural deltas, not game imagery.
- Production evaluation-wrapper test with a mocked vendor call: managed success
  retains history; native bypass and failure invalidate dependents; steady native
  Pass 1 does not repeatedly reset Pass 2. History capacity/device isolation and
  40,000 successful-record checks also pass.
- Actual-NR Cyberpunk-format pressure host: 171 evaluations, 138 resolves; both
  passes resolve on every settled recovery frame. Six final SR-output readbacks
  change when individual pass controls change. Synthetic images/FG inputs only.
- Actual-NR Cyberpunk-format 2 -> 1 -> 2 host: 100 expected evaluations, 83
  resolves; settled frames require the configured number of adjusted passes.
- BOTDW-format two passes at 70%: 120 evaluations, 114 resolves.
- Three passes with 90 MiB headroom and 100 -> 70 -> 101 -> 100%:
  240 evaluations, 192 resolves, effective-scale assertions pass.
- 400-frame preset/scale sweep: 390 evaluations, 364 resolves, 26 transition-native
  calls, 10,240 transparent synthetic FrameGen callbacks.
- Lifecycle/queue-fence, UI/configuration, DX11 isolation, nested-source, Vulkan
  ordinal, PE/version and manager regressions pass.

Fixtures do not create real generated frames or prove the recorded game's shimmer
is gone. Cyberpunk visual acceptance remains required. Actual memory exhaustion
can still temporarily bypass controls; this preview does not promise uninterrupted
processing at every pass count or supersampling setting.

## Package and install

Manager 1.0.2-local.9 / Windows 1.0.2.9.
Addon 1.0.3-pass-controls.9 / Windows 1.0.3.9 (prerelease).
Addon SHA-256: 8B291128D2BA1073A696EE8C3BF18455528F398A82E0859EFAEAF4658056ACAE.

Extract DLAssAss-5-Tool-local-9-win-x64.zip, supply your own DLLs in DLSS Files,
close Cyberpunk, and choose Reinstall for the addon. ReShade reinstall is not
needed. Internal SHA256SUMS.txt covers the executable and addon; a separate
sidecar covers the ZIP. No NVIDIA runtime DLLs are bundled.

For visual acceptance compare the same scene with both sharpness controls at
zero, then at the previous 25%, followed by 2 -> 1 -> 2. No saved presets have
been altered automatically, and no live game was operated during this work.

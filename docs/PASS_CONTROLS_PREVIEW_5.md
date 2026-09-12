# Independent pass controls / manager local preview 5

Date: 2026-09-12. Local only; public `v.1.0.2`, its tag and assets are unchanged.

## Implementation

- Main Neural Detail and Colour controls Pass 1. Passes 2–10 use their own
  transfer, colour and sharpness values, without a customization/inheritance gate.
- Defaults are transfer 100%, colour 50%, sharpness 0%. Existing saved base
  settings and enabled extra-pass overrides are preserved. Previously disabled
  overrides were inactive: they now start from the independent defaults.
- Global and per-pass hue-stable detail/coupling are neutralized, including old
  saved keys. Extra sections retain their saved expanded/collapsed states.
- The verified upstream loop already chains resolved outputs (native-SR loop
  at RVA 0x9B35A, next input 0x9B378, successful output rotation 0x9B550–0x9B567).
  No caller-loop patch or extra neural evaluations were added.
- Increasing pass count advances the working allocation epoch and uses the
  existing nonblocking real-fence drain before group admission. This prevents
  admission against an old, still-full smaller-group cache. It does not raise
  the 512 MiB ceiling, erase recording pins or retry blocked groups every frame.
- Manager enables Reinstall ReShade / Reinstall. ReShade uses official Setup's
  `--state update` operation, without shaders or deleting presets. Verified
  proxy backups remain as `.dlss5manager-*.bak`; foreign proxies are rejected.
  Addon reinstallation reuses the existing executable-relative backup/restore.

## Evidence and limits

Cyberpunk's supplied 14:52 log identifies addon pass-controls.4 and a generation
15, two-pass, 80% admission fallback. It does not reveal which allocation limit
failed. The larger-group cache-drain change addresses a concrete transition
hazard, but is **not yet accepted in Cyberpunk**. Genuine memory pressure still
uses native fallback and can bypass the resolve controls. No installed game was
modified. BOTDW's deferred focus-change freeze is not addressed.

Passed locally:

- Manager build and regression tests: API mapping/selection, enabled reinstall
  labels, addon replacement, settings preservation and restore of prior binary.
- 3,780 ImGui control-click cases at 25–150%; 144 layout cases; independent pass
  defaults/configuration, retired-key migration, all ten collapse-state headers.
- Production lifecycle/recording/multi-queue fence tests, including a full old
  cache held behind the new pass-count epoch without prematurely latching fallback.
- Hardware and WARP: 495 production shader resolves, 24 three-pass chains at
  75/100/125% with independent transfer/colour and preservation through later
  zero-transfer passes; existing sharpness, alpha and signed-HDR tests.
- Actual NR host: 390 evaluations / 372 working resolves at 100% with non-neutral
  controls; scale-churn run: 390 / 363 across 50–150%. Both preserved 10,240
  synthetic FG callback forwards, with no NR inside the FG callback. These are
  not actual generated frames or game acceptance runs.
- DX11 rollback/registry/replacement/private-cleanup, nested-source boundary
  regressions, all 65,536 ordinal export inputs, PE patch and version validation.

## Identity

- Addon: `build/pass-controls-5/renodx-dlss5-super-anus.addon64`
- Build ID: `1.0.3-pass-controls.5`; Windows file version `1.0.3.5` (prerelease).
- SHA-256: `1151F41F08855B3C4816762D6F811E805E6463F92C7315D1BBEC0F39A677CE58`
- Manager: `1.0.2-local.5`; Windows file version `1.0.2.5`.
- Package: `DLAssAss-5-Tool-local-5-win-x64.zip`, with executable/addon checksums
  in `SHA256SUMS.txt` and an empty `DLSS Files` folder containing instructions.

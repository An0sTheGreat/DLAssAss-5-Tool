# Native-control recovery / manager local preview 8

2026-09-12. Local only; no game installation, GitHub branch, tag or release changed.

## Evidence

Cyberpunk's 16:26 log identifies preview 7. At 16:26:38 it rejects an 82624 KiB
compact working set while another 82624 KiB is cached. DXGI reports 8774 MiB
usage against a 7211 MiB budget. At 16:27:50, only about 45 MiB is admissible
with an empty cache, still below the roughly 80.7 MiB request. The permanent
controls-blocked generation latch then prevents further reuse or admission attempts.

Cyberpunk uses RGBA16F motion (format 10); the earlier 90 MiB test used RG16F
(format 34) and required less memory. This preview's recovery test uses
3440x1440, R11G11B10F output and RGBA16F motion to match the relevant allocation.

The user reports that preview 7 works in BOTDW. Its supplied 16:28 log shows
two passes at 70% with motion format 34 and no comparable admission failures.
That is a working regression case, not proof of every scale in every game.

## Change

- Remove the permanent compact-allocation bypass. Always attempt matching live
  or fence-safe pooled resources, even during allocation backoff.
- After a failed compact allocation, wait 250 ms before attempting fresh
  allocation again. Existing resource reuse does not cancel this cooldown.
- Compact scratch does not allocate depth/UI textures, so those unused working
  formats no longer prevent compatible scratch reuse. Source identities and
  independent immutable views remain checked and retained.
- Preserve the 512 MiB cache cap, adaptive reserve, recording pins, real queue
  fences, scaled-generation fallback, control math and explicit later-pass
  zero motion. No cross-queue scratch shortcut, runtime patch or game-specific
  exception is added to the shipped addon.

## Validation

- Preview 7 fails the new pressure-recovery replay at frame 70: zero adjusted
  passes after headroom returns. Results: build/framegen-fixture-pass7-cp-budget-replay.
- Preview 8 final replay: warm one working set, rotate three outputs/recordings,
  inject the logged over-budget values, then about 45 MiB incremental headroom,
  then restore 512 MiB. No scale/hook/epoch changes are used to trigger recovery.
  Every frame 70-99 requires both NR evaluations and both resolves. Final result:
  171 NR evaluations, 136 resolves, 20,000 untouched synthetic FG callbacks.
  Unsafe later passes are intentionally suppressed during pressure; the test
  does not require them to run without memory.
- After recovery, change each of transfer, colour and sharpness separately on
  Pass 1 and Pass 2. All six final SR-output GPU readback comparisons differ.
  These use actual NR with non-flat synthetic input. They are output-change
  smoke checks, not a pixel-exact isolation proof for the temporal model.
- Deterministic production-shader tests provide the isolated control checks:
  63 final-image comparisons across three passes at 25/70/99/100/101/125/150%,
  plus 495 resolve cases, 24 chains and 45 retained shader cases. Hardware and
  WARP both pass, using synthetic neural deltas rather than the NR model.
- Two-pass 3440x1440 regression at requested 70/100/125%: each run completes 120
  NR evaluations; settled frames resolve both passes. Motion format 34 retained.
- Three passes, 90 MiB headroom, 100 -> 70 -> 101 -> 100%: 240 evaluations and
  192 resolves. Effective scale assertions confirm compact 100% fallback at 101%.
- 400-frame upstream/preset/scale sweep: 390 NR evaluations, 364 resolves,
  26 transition-native calls and 10,240 transparent synthetic FG forwards.
- Lifetime/pooling/multi-queue fence, config/ImGui, DX11 isolation, nested-source,
  Vulkan ordinal and PE/version checks pass. Manager regression tests pass.

The test host replaces its own DXGI-query boundary to model admission pressure;
it does not consume physical VRAM or patch an installed game. Fixtures do not
produce actual generated frames. The first preview-8 run passed recovery and
image checks but failed an aggregate assertion requiring all 200 evaluations
even during pressure. The corrected test permits documented pressure suppression
while strictly checking every settled recovery frame; the final run passes.

## Limits and install

Cyberpunk in-game acceptance remains outstanding. While memory stays exhausted,
controls may still temporarily fall back and some additional passes may be
suppressed. Recovery does not guarantee that requested supersampling fits.
Ten-pass pressure operation is not certified. BOTDW's separate focus-change
freeze plan remains deferred; DX11/Vulkan feature scope is unchanged.

Extract the entire local-8 ZIP, supply your own DLLs in DLSS Files, close the
game, then select Cyberpunk and choose Reinstall. Reinstalling ReShade is not
required for this addon change. No installed game was updated automatically.

- Manager: 1.0.2-local.8; Windows file version 1.0.2.8.
- Addon: 1.0.3-pass-controls.8; Windows file version 1.0.3.8 (prerelease).
- Addon SHA-256: FB42DDE1DBEF00733CFF3FB752D8AFC0D94D0794F1025898CE0AB4D6E33E4B66.
- ZIP: DLAssAss-5-Tool-local-8-win-x64.zip.
- Internal SHA256SUMS.txt verifies the manager executable and payload; the ZIP
  has a separate checksum sidecar. NVIDIA DLLs are not bundled.

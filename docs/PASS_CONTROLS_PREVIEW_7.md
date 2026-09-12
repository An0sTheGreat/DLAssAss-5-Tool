# Native-resolution controls / manager local preview 7

2026-09-12. Local only; no installed game, GitHub branch, tag or release changed.

## Evidence

Cyberpunk's 15:59 log loads pass-controls.6. Pass 1 resolves at 100%; adding Pass 2
rejects working-texture admission at 16:00:48.913. Both passes resolve at 70%.
Changing to 101%, then 100%, rejects admission again. This proves the controls
are bypassed by allocation fallback, not a hardcoded slider-disable threshold.
The old log does not identify the precise allocation limit in that game process.

## Implementation

- At native resolution, allocate only the input snapshot, NR output and zero-MV
  texture. The snapshot also serves as NR's input and the resolve's reference.
  Do not allocate duplicate work colour, depth or UI textures at 100%.
- Preserve original read-only depth/UI/first-pass motion resources, subrects,
  jitter and motion scaling. Additional passes still receive explicitly zeroed
  motion. Each pass snapshots its own incoming image and uses its own controls.
- If scaled allocation fails, retain a compact native-control route for that
  stream generation instead of bypassing transfer/colour/sharpness outright.
  If old scaled allocations block the compact route, drain them once through
  the existing nonblocking real-fence barrier before admitting compact resources.
- Keep the 512 MiB cache limit, adaptive VRAM reserve, immutable source views,
  recording pins, queue fences, independent histories and nested-source guard.
- Show effective NR resolution when it differs from the requested scale; log
  requested/cached/available memory and the selected fallback explicitly.
- Correct the stale startup message claiming sharpness is disabled at 100%.

## Important limits

The requested scale is still used when it can be safely allocated. This change
does NOT guarantee supersampling under memory pressure: a rejected higher scale
can run at effective 100%, with controls retained. If even compact resources or
required GPU views cannot be allocated, native fallback still bypasses controls.
Temporary native frames during transitions/fence retirement remain intentional.
No memory protection is bypassed to promise every pass count on every GPU.

Cyberpunk game acceptance remains outstanding. BOTDW's focus-change freeze is
still deferred. Experimental DX11/Vulkan scope and manager behavior are unchanged.

## Validation

- A test-owned host replaces only its embedded DXGI-query boundary with a
  deterministic headroom value; it does not consume physical VRAM to simulate
  pressure or patch an installed game. It uses actual NR, 3440x1440, three
  rotating SR outputs and three command recordings in flight.
- Two passes, 90 MiB available headroom, 100 -> 70 -> 101 -> 100%: controls remain
  active after transitions; 101% falls back to compact 100%. The scaled request
  rejects 104384 KiB against a 92160 KiB admission limit, reproducing the
  resolution-sensitive failure path. Two passes with ample headroom honor 101%.
- Final binary, three passes with the same pressure/transition sequence:
  240 NR evaluations, 192 adjusted resolves. Every checked settled frame resolves
  all three passes. 20,000 synthetic FG forwards preserved, with no NR inside them.
- Final binary, two passes at requested 150%, real memory query: 120 NR
  evaluations, 106 adjusted resolves. Scaled admission falls back; compact
  controls resume after the real-fence drain. The initial harness stopped at
  frame 8 during that drain. The completed run permits only explicitly reported
  drains, bounded to 12 frames; every settled frame must still resolve all passes.
- Hardware and WARP: 63 final-image readback comparisons independently vary
  transfer, colour and sharpness on each of three passes at 25/70/99/100/101/125/150%.
  Non-flat inputs test sharpening; all controls change final RGB without changing
  alpha. These are production shader tests with synthetic neural deltas, not
  game screenshots. Existing 495 resolve, 24 chain and 45 shader cases also pass.
- Lifecycle/recording/multi-queue fence, configuration, ImGui, DX11 isolation,
  nested-source, export-ordinal and PE/version checks pass. Manager regression
  tests preserve reinstall, backup/restore and conditional PLAY pulse behavior.

Fixtures do not generate real FG frames. Ten-pass high-pressure operation is not
certified by this preview; the previous ten-pass failure is not claimed fixed.

## Identity and installation

- Addon: `1.0.3-pass-controls.7`; Windows version `1.0.3.7` (prerelease).
- Addon SHA-256: `417A4B12A1B6017B609FFD172A22BBA665FC16F7403767F2FCDE41131586E595`
- Manager: `1.0.2-local.7`; Windows version `1.0.2.7`.
- ZIP: `DLAssAss-5-Tool-local-7-win-x64.zip`. Internal `SHA256SUMS.txt` verifies
  the executable and addon; the ZIP has a separate SHA-256 sidecar.
- Close the game, extract the complete tool, supply your own DLLs in `DLSS Files`,
  and use **Reinstall** for the selected game. Existing game installations have
  not been changed automatically. No NVIDIA DLLs are bundled.

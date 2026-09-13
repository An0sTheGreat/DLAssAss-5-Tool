# DLAssAss 5 Tool v.1.0.5

## Changes since v.1.0.4

- Add a themed **UPDATE ALL** button beside the detected-game count. It replaces
  only addons already installed in detected games, deduplicates shared install
  directories, keeps per-game backups, continues after failures and reports a
  final summary. ReShade, DLSS files, settings and unmodded games are untouched.
- Add a persistent **Multipass Motion** selector directly below Hook Method in
  the themed RenoDX/ReShade overlay.
- **Reuse Game Motion (Recommended)** is the default and supplies resampled game
  motion to every neural pass.
- **Zero Later-Pass Motion** retains the prior behavior. **Zero Motion + Reset
  History** retains the aggressive diagnostic behavior.
- Changing modes uses the existing native transition frame and resets pass
  history before managed processing resumes.

User testing reports that motion reuse substantially improves haloing and
ghosting in BOTDW, TLOU2 and Cyberpunk. Cyberpunk's separate shimmering-shadow
issue with two passes remains unresolved.

## Build identity

- Manager: **1.0.5**, Windows **1.0.5.0**.
- Bundled addon: **1.0.5-motion-runtime.1**, Windows **1.0.5.16**.
- Addon SHA-256: `750982FCF31AC8F912561AC72BDEBDE019DCCD3F427BAF3FE24ABF2F96B28139`.

No NVIDIA runtime DLLs are included. `SHA256SUMS.txt` inside the archive covers
the executable and addon; `SHA256SUMS-v.1.0.5.txt` covers the release ZIP.

## Validation

Policy tests cover all three modes, invalid saved-value fallback and the reuse
default. Manager tests cover addon-only replacement, backup/restore, untouched
DLSS/settings, refusal of unmodded games and header placement. UI tests cover
selector placement and the existing themed layout. The
full V6.6 lifecycle, WARP, DX11, Vulkan export, manager and archive-integrity
checks pass. Native build checks are not a substitute for live game testing.

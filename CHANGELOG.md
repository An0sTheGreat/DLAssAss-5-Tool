# Changelog

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

# Release channels

Every public release is published through both channels:

- `main` contains the complete DLAssAss 5 Tool source. Its release includes the
  self-contained Windows application archive and checksum file.
- `standalone-addon` contains the add-on source. Its release includes exactly
  one loose `renodx-dlss5-super-anus.addon64` asset for manual installation.

The two release notes must identify the matching add-on version and SHA-256.
Neither channel bundles NVIDIA runtime DLLs.

Manager tags use `v.<manager-version>` (for example, `v.1.0.7`). Standalone
add-on tags use `v<addon-file-version>` (for example, `v1.0.6.17`). The newest
manager application release remains the repository's **Latest** release.

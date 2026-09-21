# Bundled DLSS runtime files

Release builds include these pinned runtime files:

- `nvngx_dlss.dll` — DLSS Super Resolution
- `nvngx_dlssg.dll` — DLSS Frame Generation
- `nvngx_dlssnr.dll` — DLSS Ray Reconstruction / Neural Rendering

`nvngx_dlss.dll` and `nvngx_dlssg.dll` report NVIDIA signatures. The included
`nvngx_dlssnr.dll` is a modified community runtime, has an Authenticode hash
mismatch, and must not be represented as an official NVIDIA-signed binary.

The release publisher verifies these exact SHA-256 values before packaging:

- `nvngx_dlss.dll`: `3975567B8943C53ACCE397F2B72380092F84F162D00B0D2C7D08A1025C563983`
- `nvngx_dlssg.dll`: `FF6E90EB78B827927DFF5B4ECC6B1C870C2E9BCA29ED9F48C7D348CC9E170B82`
- `nvngx_dlssnr.dll`: `E67DEE209320CDAFE0E93E45675D7AA34323A53ACC57A72B2E40A181581C989A`

Existing game files are backed up before replacement. Use these files only
under terms that permit your use and redistribution.

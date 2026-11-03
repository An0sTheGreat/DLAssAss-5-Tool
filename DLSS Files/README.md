# Bundled DLSS runtime files

Release builds include these pinned runtime files:

- `nvngx_dlss.dll` — DLSS Super Resolution
- `nvngx_dlssg.dll` — DLSS Frame Generation
- `nvngx_dlssnr.dll` — DLSS Neural Rendering for RTX 20/30/40 series GPUs
- `RTX 50/nvngx_dlssnr.dll` — DLSS Neural Rendering for RTX 50 series (Blackwell) GPUs

The tool installs `RTX 50/nvngx_dlssnr.dll` when Windows reports an RTX 50 series
(or RTX PRO Blackwell) display adapter, and the top-level `nvngx_dlssnr.dll`
otherwise. Either file is installed under the name `nvngx_dlssnr.dll`.

- `nvngx_dlss.dll`, `nvngx_dlssg.dll` and `RTX 50/nvngx_dlssnr.dll` (version
  310.9.16.0) have valid NVIDIA Corporation Authenticode signatures. NVIDIA's
  310.9.16 Neural Rendering runtime requires a Blackwell GPU.
- The top-level `nvngx_dlssnr.dll` (version string 310.8.0.0) is the SF community
  repack of NVIDIA's 310.8 runtime. It removes NVIDIA's Blackwell-only
  architecture check, has an Authenticode hash mismatch, and must not be
  represented as an official NVIDIA-signed binary. On NVIDIA driver 617.42 every
  310.8 build (signed or repacked) evaluates about 40x slower than 310.9.16 and
  gives a much weaker neural result; this cannot be fixed by the add-on.

The release publisher verifies these exact SHA-256 values before packaging:

- `nvngx_dlss.dll`: `3975567B8943C53ACCE397F2B72380092F84F162D00B0D2C7D08A1025C563983`
- `nvngx_dlssg.dll`: `FF6E90EB78B827927DFF5B4ECC6B1C870C2E9BCA29ED9F48C7D348CC9E170B82`
- `nvngx_dlssnr.dll`: `E67DEE209320CDAFE0E93E45675D7AA34323A53ACC57A72B2E40A181581C989A`
- `RTX 50/nvngx_dlssnr.dll`: `2BC19BC09A7B89DD44F73E2AC0108FCC586E2154BDD0830F242F4F040E28789F`

Existing game files are backed up before replacement. Use these files only
under terms that permit your use and redistribution.

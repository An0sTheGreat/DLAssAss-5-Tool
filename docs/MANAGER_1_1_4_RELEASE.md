# DLAssAss 5 Tool 1.1.4

Manager 1.1.4 with add-on `1.1.4-nr-runtime-compat.1` (file version 1.1.4.34).

## Runtime identity

| File | Version | Signature | SHA-256 |
| --- | --- | --- | --- |
| `DLSS Files/RTX 50/nvngx_dlssnr.dll` | 310.9.16.0 (CL 39025366) | Valid, NVIDIA Corporation | `2BC19BC09A7B89DD44F73E2AC0108FCC586E2154BDD0830F242F4F040E28789F` |
| `DLSS Files/nvngx_dlssnr.dll` | 310.8.0.0 (CL 38718415), SF repack | Hash mismatch | `E67DEE209320CDAFE0E93E45675D7AA34323A53ACC57A72B2E40A181581C989A` |
| Add-on payload | 1.1.4.34 | — | `3AF920C783F77C1DAFF2F6865305375350D7EF575CD813632DFBD0123FA18870` |

310.9.16 and 310.8 export the same 55 functions and import the same modules. NVIDIA's
310.8/310.9 require a Blackwell GPU (architecture 0x1B0); the SF repack patches out that
check. Validated on GeForce RTX 5080 with driver 617.42.

## Root causes

- **Driver 617.42 and 310.8 (measured).** In an isolated D3D12 probe at 3440x1440, every
  310.8 build takes 380–660 ms per Neural Rendering evaluation; 310.9.16 takes 10–21 ms.
  In game the 310.8 effect degraded to a faint colour shift, so the add-on's resolution,
  transfer, colour, sharpness and multipass controls (which scale NR's own edit) appeared
  to do nothing. Using 310.9.16 restored them in Crimson Desert.
- **310.9 crash (fixed).** Games initialise NGX with ReShade's proxy device, so the NGX
  core's descriptor heaps are ReShade proxy objects. NVIDIA DLSSNR binds those heaps on
  the command list it is given; upstream gave it the native list and D3D12Core faulted in
  `SetDescriptorHeaps` during `CreateFeature`. The add-on now redirects the upstream
  snippet create/evaluate slots and records into the proxy list the game uses with NGX.
  The SF repack, identified by its patched architecture-check bytes, keeps the native
  list (it uses snippet-owned heaps and faults through the proxy).
- The DLSSNR parameter contract (keys, types, create/evaluate dimensions, scaling-ratio
  callback, intensity/tone, history reset, in-place output) is unchanged: 310.8 and 310.9.16
  produce identical outputs for every probed case.

## Validation

- `scripts/test/test-nr-contract.cmd`: 310.8 (signed), SF 310.8 and 310.9.16 across 50/75/
  100/150% create sizes, scaled evaluate, in-place, intensity/tone, scaling ratio, 30-frame
  history and 20 create/evaluate/release cycles; under a ReShade proxy device the native
  list crashes NVIDIA builds and the proxy list passes.
- Integrated FrameGen/NR fixture (`tools/run-framegen-fixture.ps1`) on both runtimes: one
  pass, 3 passes at 50% (1720x720 NR), 2 passes at 150% (5160x2160), 1-3 pass toggles at
  75%, and transfer/colour/sharpness knobs changing the final image; no fallbacks or
  crashes (310.9.16 scaled multipass repeated 8 times). Add-on 1.1.3 crashes with 310.9.16.
- Manager tests including RTX 50 runtime selection and restore.
- In game (Crimson Desert, RTX 5080, 617.42, 310.9.16): every evaluation succeeded with no
  fallback, scale changes, 2+ passes and Pass 1 edits took effect, and the game exited
  without a crash. The user confirmed the controls function again. That session used the
  preview build (`89A7C9AB…`); the final build adds only the teardown release guard, which is
  validated in fixtures, not yet in game.

## Limitations

- RTX 20/30/40 GPUs on driver 617.42 still get the slow, weak 310.8 result; this is a
  driver/runtime limitation.
- 310.9.16 sometimes frees a retained feature's handle before upstream releases it at device
  teardown (about 1 in 4 multipass-toggle fixture runs faulted). The add-on now skips releasing
  an already-unmapped handle and logs a warning; 18 later runs, including 6 that hit the
  guard, finished without a crash.
- Vulkan Neural Rendering Resolution other than 100% is still unsupported.

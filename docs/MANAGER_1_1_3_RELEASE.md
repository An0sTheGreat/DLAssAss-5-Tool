# DLAssAss 5 Tool v1.1.3

## Release changes

- Vulkan games with native DLSS: the neural pass range-compresses the game's HDR DLSS output (live `VulkanNrKnee`,
  default 80%, in `ReShade.ini` `[RenoDXNeuralResolution]`) and expands the result afterwards. This removes the
  corrupt rectangular tiles that appeared around very bright lights, and the network no longer reads and writes the
  same image.
- Vulkan: Neural Transfer Strength above 100% (up to 200%) and Neural Sharpness use the DX12 resolve arithmetic.
  Colour strength continues to work as before.
- Vulkan: Encoding (Auto / Linear BT.709 / sRGB / BT.2100 PQ / scRGB / scRGB-nl, read from the active preset) and
  Diffuse White (nits) now control the conversion into the neural network's domain. Auto treats the game's DLSS
  output as linear BT.709, as the overlay describes.
- Vulkan: the Hook Method status shows "Active" after a pass has run.
- Game detection accuracy:
  - Native DLSS is detected by scanning whole executables and integration modules for NGX/Streamline markers (ASCII and
    UTF-16, including statically linked NGX), instead of sampling the first and last 4 MB.
  - The graphics API comes from the game's own evidence first (ReShade.log, Unity, Unreal, CryEngine, SCS and
    Warframe-engine logs and settings, Ogre Plugins*.cfg) before static file evidence; Unity defaults to DX11.
  - The game executable is ranked (64-bit, installed ReShade, size, VR and engine-tool penalties) instead of taking the largest.
  - Games in inner binary folders (Bin64, LIVE\Bin64, Binaries\Win64) show the game's name; Steam games use their manifest title.
  - **Scan Libraries** reads Steam manifests (stale, uninstalled and redistributable entries are dropped) and also finds
    Epic, GOG and Xbox games; Unreal Engine and Tools folders are no longer listed as games.
  - Tools, launchers and leftovers are hidden by default; Show hidden lists them and Unhide is remembered.
- Detect the embedded feeder capability directly from the installed add-on and keep Integrated Feed status hidden for
  native-DLSS DX12 games (carried over from the unreleased 1.1.2 follow-ups).
- Known limitation: a Neural Rendering Resolution other than 100% keeps the native Vulkan output.
- The Encoding option order for PQ, scRGB and scRGB-nl was inferred from the overlay and is not yet verified in game.

## Build identity

- Manager: **1.1.3**, Windows **1.1.3.0**.
- Bundled add-on: **1.1.3-vulkan-neural-range.1**, Windows **1.1.3.33**.
- Add-on SHA-256: `4A53A3E7C1617864BD4894898B514E15265141EA29AE88B1C04EFF9A5CCEA19E`.

## Validation

The add-on passed its V6.6 static checks, Windows version validation, DX11 WARP/lifecycle regressions and the Vulkan
export lookup test. The Vulkan range shader (compress, in-place expand, resolve, six encodings, diffuse white,
NaN/inf handling, dispatch bounds) passed a GPU test on an NVIDIA GeForce RTX 5080 against a CPU reference.
Detection was checked against a 44-folder library report (before/after tables in `tests/DetectReport`) and regression tests.
In-game testing: Enshrouded (Vulkan, DLSS) at 100% resolution with transfer, colour, sharpness, encoding and
status label confirmed working.

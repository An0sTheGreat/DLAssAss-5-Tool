using Microsoft.Win32;
using System.Text.RegularExpressions;

namespace DLSS5ManAger.Core;

/// Picks the bundled Neural Rendering runtime for this PC. NVIDIA's signed
/// nvngx_dlssnr.dll 310.9.16 requires a Blackwell (RTX 50 series) GPU; older RTX GPUs
/// keep the SF 310.8 repack, which removes that architecture gate.
public static partial class GpuRuntime
{
    public const string Rtx50Folder = "RTX 50";
    private const string DisplayClass = @"SYSTEM\CurrentControlSet\Control\Class\{4d36e968-e325-11ce-bfc1-08002be10318}";

    [GeneratedRegex(@"RTX\s*(PRO\s*)?50\d\d|Blackwell", RegexOptions.IgnoreCase)]
    private static partial Regex BlackwellName();

    public static bool IsBlackwellName(string adapterName) => BlackwellName().IsMatch(adapterName);

    public static bool HasBlackwellGpu()
    {
        try
        {
            using var adapters = Registry.LocalMachine.OpenSubKey(DisplayClass);
            if (adapters is null) return false;
            foreach (var name in adapters.GetSubKeyNames())
            {
                using var adapter = adapters.OpenSubKey(name);
                if (adapter?.GetValue("DriverDesc") is string description && IsBlackwellName(description)) return true;
            }
        }
        catch (Exception exception) when (exception is UnauthorizedAccessException or System.Security.SecurityException) { }
        return false;
    }

    /// Source file for a bundled runtime: the NR runtime comes from the RTX 50 subfolder on Blackwell.
    public static string SourcePath(string dlssDirectory, string name, bool blackwell) =>
        blackwell && name.Equals("nvngx_dlssnr.dll", StringComparison.OrdinalIgnoreCase)
            ? Path.Combine(dlssDirectory, Rtx50Folder, name)
            : Path.Combine(dlssDirectory, name);
}

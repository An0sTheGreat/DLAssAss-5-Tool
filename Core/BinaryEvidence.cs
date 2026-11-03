using System.Collections.Concurrent;
using System.Text;

namespace DLSS5ManAger.Core;

/// <summary>
/// Streams a whole binary looking for NVIDIA NGX / Streamline integration markers, as ASCII and UTF-16 (the NGX SDK
/// loads feature DLLs by wide-string name). The old head/tail sampling missed markers in the middle of large
/// executables. Results are cached by path, size and write time.
/// </summary>
internal sealed record BinaryEvidence(
    bool NgxD3D11, bool NgxD3D12, bool NgxVulkan, bool DlssDllName, bool FrameGenDllName, bool RayReconstructionDllName,
    bool StreamlineInterposer, bool StreamlineDlss, int DlssSettingStrings = 0)
{
    public static readonly BinaryEvidence None = new(false, false, false, false, false, false, false, false);

    /// <summary>Evidence that the binary itself integrates DLSS super resolution.</summary>
    public bool NativeDlss => NgxD3D11 || NgxD3D12 || NgxVulkan || DlssDllName || StreamlineDlss || DlssSettingStrings >= 2;
    public bool NativeFrameGeneration => FrameGenDllName;

    public IEnumerable<string> Describe()
    {
        if (NgxD3D11) yield return "NGX D3D11 API";
        if (NgxD3D12) yield return "NGX D3D12 API";
        if (NgxVulkan) yield return "NGX Vulkan API";
        if (DlssDllName) yield return "loads nvngx_dlss.dll";
        if (StreamlineDlss) yield return "Streamline DLSS plugin";
        else if (StreamlineInterposer) yield return "Streamline interposer";
        if (DlssSettingStrings >= 2 && !(NgxD3D11 || NgxD3D12 || NgxVulkan || DlssDllName || StreamlineDlss))
            yield return $"{DlssSettingStrings} DLSS setting strings (statically linked NGX)";
        if (FrameGenDllName) yield return "loads nvngx_dlssg.dll";
        if (RayReconstructionDllName) yield return "loads nvngx_dlssd.dll";
    }

    private const int ChunkSize = 8 * 1024 * 1024;
    private const long MaximumFileSize = 1L << 30;
    private static readonly ConcurrentDictionary<(string Path, long Length, long Ticks), BinaryEvidence> Cache =
        new(StringComparer.OrdinalIgnoreCase.ToTupleComparer());

    private static readonly string[] Markers =
    [
        "NVSDK_NGX_D3D11_", "NVSDK_NGX_D3D12_", "NVSDK_NGX_VULKAN_", "nvngx_dlss.dll", "nvngx_dlssg.dll", "nvngx_dlssd.dll",
        "sl.interposer.dll", "sl.dlss.dll",
        // Engines that link NGX statically (for example Frostbite) keep no NGX names but still expose DLSS settings.
        "DLSS_Ultra_Performance", "DLSS_Performance", "DLSS_Balanced", "DLSS_Quality", "DLSSQuality", "DlssEnable"
    ];
    private static readonly byte[][] Ascii = Markers.Select(Encoding.ASCII.GetBytes).ToArray();
    private static readonly byte[][] Wide = Markers.Select(Encoding.Unicode.GetBytes).ToArray();
    private static readonly int Overlap = Wide.Max(pattern => pattern.Length) - 1;

    public static BinaryEvidence Scan(string path)
    {
        try
        {
            var info = new FileInfo(path);
            if (!info.Exists || info.Length == 0 || info.Length > MaximumFileSize) return None;
            return Cache.GetOrAdd((info.FullName, info.Length, info.LastWriteTimeUtc.Ticks), _ => ScanStream(info.FullName));
        }
        catch { return None; }
    }

    private static BinaryEvidence ScanStream(string path)
    {
        var found = new bool[Markers.Length];
        try
        {
            using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete,
                1 << 16, FileOptions.SequentialScan);
            var buffer = new byte[ChunkSize + Overlap];
            var carried = 0;
            while (true)
            {
                var read = stream.Read(buffer, carried, ChunkSize);
                if (read <= 0) break;
                var span = buffer.AsSpan(0, carried + read);
                for (var i = 0; i < Markers.Length; i++)
                    if (!found[i] && (span.IndexOf(Ascii[i]) >= 0 || span.IndexOf(Wide[i]) >= 0)) found[i] = true;
                carried = Math.Min(Overlap, span.Length);
                span[^carried..].CopyTo(buffer);
            }
        }
        catch { }
        return new BinaryEvidence(found[0], found[1], found[2], found[3], found[4], found[5], found[6], found[7],
            found.Skip(8).Count(hit => hit));
    }
}

internal static class EvidenceComparers
{
    public static IEqualityComparer<(string Path, long Length, long Ticks)> ToTupleComparer(this StringComparer comparer) =>
        new TupleComparer(comparer);

    private sealed class TupleComparer(StringComparer paths) : IEqualityComparer<(string Path, long Length, long Ticks)>
    {
        public bool Equals((string Path, long Length, long Ticks) x, (string Path, long Length, long Ticks) y) =>
            x.Length == y.Length && x.Ticks == y.Ticks && paths.Equals(x.Path, y.Path);
        public int GetHashCode((string Path, long Length, long Ticks) value) =>
            HashCode.Combine(paths.GetHashCode(value.Path), value.Length, value.Ticks);
    }
}

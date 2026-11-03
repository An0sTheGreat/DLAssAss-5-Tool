using System.Text;
using System.Text.RegularExpressions;

namespace DLSS5ManAger.Core;

/// <summary>
/// Graphics API evidence from the game's own logs and settings (the API actually in use), which is far more
/// reliable than guessing from imported DLL names. Strong patterns are explicit statements by the engine; weak
/// ones are counted tokens. A decision needs a clear winner, otherwise the caller falls back to static evidence.
/// </summary>
internal static class ApiEvidence
{
    private const RegexOptions Options = RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled;
    private const int StrongWeight = 20;

    private static readonly (Regex Pattern, string Api, int Weight)[] Patterns =
    [
        // Explicit selections / statements
        (new(@"graphicsDriver:\s*dx12\b", Options), "DX12", StrongWeight),
        (new(@"graphicsDriver:\s*dx11\b", Options), "DX11", StrongWeight),
        (new(@"graphicsDriver:\s*(vulkan|vk)\b", Options), "Vulkan", StrongWeight),
        (new(@"graphicsDriver:\s*(opengl|gl)\b", Options), "OpenGL", StrongWeight),
        (new(@"-force-(d3d12|dx12)\b", Options), "DX12", StrongWeight),
        (new(@"-force-(d3d11|dx11)\b", Options), "DX11", StrongWeight),
        (new(@"-force-(vulkan|vk)\b", Options), "Vulkan", StrongWeight),
        (new(@"-force-(glcore|opengl)\w*", Options), "OpenGL", StrongWeight),
        // Unity Player.log: "Direct3D:\n    Version:  Direct3D 11.0 [level 11.1]"
        (new(@"Version:\s+Direct3D\s+12", Options), "DX12", StrongWeight),
        (new(@"Version:\s+Direct3D\s+11", Options), "DX11", StrongWeight),
        (new(@"Version:\s+Direct3D\s+10", Options), "DX10", StrongWeight),
        (new(@"Version:\s+Direct3D\s+9", Options), "DX9", StrongWeight),
        (new(@"Version:\s+Vulkan\b", Options), "Vulkan", StrongWeight),
        (new(@"Version:\s+OpenGL\b", Options), "OpenGL", StrongWeight),
        // CryEngine ("Feature level: DX11.1 (SM 5.0)") and SCS ("Selected rendering device: dx11", "-rdevice dx11")
        (new(@"Feature level:\s*DX12", Options), "DX12", StrongWeight),
        (new(@"Feature level:\s*DX11", Options), "DX11", StrongWeight),
        (new(@"(Selected rendering device:|-rdevice)\s*dx12", Options), "DX12", StrongWeight),
        (new(@"(Selected rendering device:|-rdevice)\s*dx11", Options), "DX11", StrongWeight),
        (new(@"(Selected rendering device:|-rdevice)\s*(vulkan|vk)", Options), "Vulkan", StrongWeight),
        (new(@"(Selected rendering device:|-rdevice)\s*(opengl|gl)", Options), "OpenGL", StrongWeight),
        // Unreal settings and log
        (new(@"(PreferredRHI|DefaultGraphicsRHI)\s*=\s*(DefaultGraphicsRHI_)?(DX12|D3D12)\b", Options), "DX12", StrongWeight),
        (new(@"(PreferredRHI|DefaultGraphicsRHI)\s*=\s*(DefaultGraphicsRHI_)?(DX11|D3D11)\b", Options), "DX11", StrongWeight),
        (new(@"(PreferredRHI|DefaultGraphicsRHI)\s*=\s*(DefaultGraphicsRHI_)?Vulkan\b", Options), "Vulkan", StrongWeight),
        (new(@"LogD3D12RHI", Options), "DX12", 2),
        (new(@"LogD3D11RHI", Options), "DX11", 2),
        (new(@"LogVulkanRHI", Options), "Vulkan", 2),
        // Bracketed subsystem tags used by custom engines ("[vulkan] ...")
        (new(@"\[vulkan\]", Options), "Vulkan", 1),
        (new(@"\[d3d12\]", Options), "DX12", 1),
        (new(@"\[d3d11\]", Options), "DX11", 1)
    ];
    private static readonly Regex OgrePlugin = new(@"^\s*Plugin\s*=\s*RenderSystem_(?<name>\w+)", Options | RegexOptions.Multiline);

    public static (List<string> Apis, string Source) FromGameFiles(string directory, string? executablePath, string displayName)
    {
        var scores = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        var sources = new List<string>();

        foreach (var file in CandidateFiles(directory, executablePath, displayName))
        {
            var before = scores.Values.Sum();
            Score(SafeReadSampled(file.Path, file.Maximum), scores);
            if (scores.Values.Sum() != before) sources.Add(Path.GetFileName(file.Path));
        }
        // Ogre-based engines declare their render system in Plugins*.cfg.
        foreach (var cfg in SafeFiles(directory, "Plugins*.cfg"))
            foreach (Match match in OgrePlugin.Matches(SafeRead(cfg, 64 * 1024)))
                if (OgreApi(match.Groups["name"].Value) is { } api) { Add(scores, api, 15); sources.Add(Path.GetFileName(cfg)); }

        var decided = Decide(scores);
        return (decided, decided.Count == 0 ? "" : string.Join(", ", sources.Distinct().Take(3)));
    }

    internal static List<string> Decide(Dictionary<string, int> scores)
    {
        var ordered = scores.Where(item => item.Value > 0).OrderByDescending(item => item.Value).ToArray();
        if (ordered.Length == 0 || ordered[0].Value < 6) return [];
        if (ordered.Length == 1 || ordered[0].Value >= ordered[1].Value * 2) return [ordered[0].Key];
        return ordered.Where(item => item.Value * 10 >= ordered[0].Value * 3).Select(item => item.Key).ToList();
    }

    internal static void Score(string text, Dictionary<string, int> scores)
    {
        foreach (var (pattern, api, weight) in Patterns)
        {
            var count = Math.Min(pattern.Matches(text).Count, weight >= StrongWeight ? 1 : 12);
            if (count > 0) Add(scores, api, weight * count);
        }
    }

    private static void Add(Dictionary<string, int> scores, string api, int amount) =>
        scores[api] = scores.GetValueOrDefault(api) + amount;

    private static string? OgreApi(string renderSystem) => renderSystem.ToLowerInvariant() switch
    {
        var name when name.StartsWith("direct3d11") => "DX11",
        var name when name.StartsWith("direct3d9") => "DX9",
        var name when name.StartsWith("gl") => "OpenGL",
        _ => null
    };

    private readonly record struct EvidenceFile(string Path, int Maximum);

    private static IEnumerable<EvidenceFile> CandidateFiles(string directory, string? executablePath, string displayName)
    {
        var executableDirectory = executablePath is null ? directory : Path.GetDirectoryName(executablePath) ?? directory;
        var stem = executablePath is null ? "" : System.IO.Path.GetFileNameWithoutExtension(executablePath)
            .Replace("-Win64-Shipping", "", StringComparison.OrdinalIgnoreCase);
        var roots = new List<string> { executableDirectory, System.IO.Path.Combine(executableDirectory, "logs"),
            System.IO.Path.Combine(executableDirectory, "Logs"), directory, System.IO.Path.Combine(directory, "logs") };
        // Engines like CryEngine write Game.log one level above Bin64.
        if (System.IO.Path.GetDirectoryName(executableDirectory) is { } parentDirectory) roots.Add(parentDirectory);
        var local = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var profile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        foreach (var name in new[] { stem, displayName }.Where(name => name.Length > 0).Distinct(StringComparer.OrdinalIgnoreCase))
        {
            var perUser = System.IO.Path.Combine(local, name);
            roots.Add(perUser);
            roots.Add(System.IO.Path.Combine(perUser, "Saved", "Logs"));
            foreach (var config in SafeDirectories(System.IO.Path.Combine(perUser, "Saved", "Config")))
                roots.Add(config);
            roots.Add(System.IO.Path.Combine(profile, "Documents", "My Games", name));
            roots.Add(System.IO.Path.Combine(profile, "Documents", name));
        }
        // Unity writes Player.log under LocalLow\<company>\<product>; app.info holds both names.
        var data = System.IO.Path.Combine(executableDirectory, stem + "_Data", "app.info");
        if (File.Exists(data))
        {
            var lines = SafeRead(data, 4096).Split('\n', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
            if (lines.Length >= 2)
                roots.Add(System.IO.Path.Combine(profile, "AppData", "LocalLow", lines[0], lines[1]));
        }

        var yielded = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var root in roots.Distinct(StringComparer.OrdinalIgnoreCase))
        {
            foreach (var pattern in new[] { "*.log", "*.log.txt", "output_log.txt", "*.ini" })
                foreach (var file in SafeFiles(root, pattern).OrderByDescending(SafeWrite).Take(pattern == "*.ini" ? 3 : 4))
                    if (yielded.Add(file) && SafeLength(file) is > 0 and <= 16 * 1024 * 1024)
                        yield return new EvidenceFile(file, 1 << 20);
        }
    }

    /// <summary>First and last <paramref name="maximum"/> bytes: logs are appended to, so recent lines are at the end.</summary>
    private static string SafeReadSampled(string path, int maximum)
    {
        try
        {
            using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
            var head = new byte[(int)Math.Min(maximum, stream.Length)];
            var read = stream.Read(head, 0, head.Length);
            var text = Encoding.Latin1.GetString(head, 0, read);
            if (stream.Length <= maximum) return text;
            var tail = new byte[(int)Math.Min(maximum, stream.Length - maximum)];
            stream.Position = stream.Length - tail.Length;
            read = stream.Read(tail, 0, tail.Length);
            return text + "\n" + Encoding.Latin1.GetString(tail, 0, read);
        }
        catch { return ""; }
    }

    private static string SafeRead(string path, int maximum)
    {
        try
        {
            using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
            var buffer = new byte[Math.Min(maximum, (int)Math.Min(stream.Length, int.MaxValue))];
            var read = stream.Read(buffer, 0, buffer.Length);
            return Encoding.Latin1.GetString(buffer, 0, read);
        }
        catch { return ""; }
    }

    private static IEnumerable<string> SafeFiles(string directory, string pattern)
    {
        try { return Directory.Exists(directory) ? Directory.EnumerateFiles(directory, pattern).ToArray() : []; }
        catch { return []; }
    }

    private static IEnumerable<string> SafeDirectories(string directory)
    {
        try { return Directory.Exists(directory) ? Directory.EnumerateDirectories(directory).ToArray() : []; }
        catch { return []; }
    }

    private static DateTime SafeWrite(string path)
    {
        try { return File.GetLastWriteTimeUtc(path); }
        catch { return DateTime.MinValue; }
    }

    private static long SafeLength(string path)
    {
        try { return new FileInfo(path).Length; }
        catch { return 0; }
    }
}

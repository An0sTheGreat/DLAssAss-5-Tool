using System.IO.Compression;
using System.Net.Http;
using System.Security.Cryptography;

namespace DLSS5ManAger.Core;

public sealed class NonDlssSetup : IDisposable
{
    private readonly string _stagingDirectory;
    public IReadOnlyList<InstallSource> Files { get; }

    internal NonDlssSetup(string stagingDirectory, IReadOnlyList<InstallSource> files)
    {
        _stagingDirectory = stagingDirectory;
        Files = files;
    }

    public void Dispose()
    {
        try { Directory.Delete(_stagingDirectory, true); }
        catch { }
    }
}

public sealed class NonDlssSetupService
{
    private const long MaximumDownloadBytes = 32L * 1024 * 1024;
    private const string LumeniteHash = "43220F99FC0FFA0216E01EBD657180F8C9D043C939F760283B896EA257F1B6A2";
    private static readonly Uri LumeniteUrl = new(
        "https://codeload.github.com/umar-afzaal/LumeniteFX/zip/f8cbbb4eccfcb7adf0d74bb358ba349272e3c1e9");
    private static readonly HttpClient Client = new() { Timeout = TimeSpan.FromMinutes(2) };
    private readonly string _payloadDirectory;

    static NonDlssSetupService() => Client.DefaultRequestHeaders.UserAgent.ParseAdd("DLAssAss5Tool/1.1.2");

    public NonDlssSetupService(string? payloadDirectory = null) =>
        _payloadDirectory = payloadDirectory ?? Path.Combine(AppContext.BaseDirectory, "Payload");

    public static bool HasRequiredShaders(string executablePath) =>
        InstallerService.InstallDirectory(executablePath) is { } directory &&
        File.Exists(Path.Combine(directory, "reshade-shaders", "Shaders", "ReShade.fxh"));

    public async Task<NonDlssSetup> PrepareAsync(string executablePath)
    {
        var gameDirectory = InstallerService.InstallDirectory(executablePath)
            ?? throw new InvalidOperationException("The selected game executable does not exist.");
        if (!HasRequiredShaders(executablePath))
            throw new InvalidOperationException(
                "ReShade's standard shader package is required. Run ReShade Setup again and select the standard shader package.");

        var feedShader = Path.Combine(_payloadDirectory, "DLSS5_Feed.fx");
        if (!File.Exists(feedShader)) throw new FileNotFoundException("The integrated feed shader is missing.", feedShader);

        var staging = Path.Combine(Path.GetTempPath(), "DLAssAss5Tool-Lumenite-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(staging);
        try
        {
            var files = await DownloadLumeniteAsync(staging);
            files.Add(StageCopy(feedShader, staging, Path.Combine("reshade-shaders", "Shaders", "DLSS5_Feed.fx")));

            var reshadeIni = Path.Combine(gameDirectory, "ReShade.ini");
            var reshadeText = ReadText(reshadeIni);
            var presetTarget = ResolvePresetTarget(gameDirectory, GetIniValue(reshadeText, "GENERAL", "PresetPath"));
            files.Add(StageText(staging, "ReShade.ini", BuildReShadeIni(reshadeText)));
            files.Add(StageText(staging, presetTarget, BuildPreset(ReadText(Path.Combine(gameDirectory, presetTarget)))));
            files.Add(StageText(staging, "dlss5-feed.cfg",
                BuildFeedConfig(ReadText(Path.Combine(gameDirectory, "dlss5-feed.cfg")))));
            return new NonDlssSetup(staging, files);
        }
        catch
        {
            try { Directory.Delete(staging, true); } catch { }
            throw;
        }
    }

    private static async Task<List<InstallSource>> DownloadLumeniteAsync(string staging)
    {
        using var response = await Client.GetAsync(LumeniteUrl, HttpCompletionOption.ResponseHeadersRead);
        response.EnsureSuccessStatusCode();
        if (response.RequestMessage?.RequestUri is not { Scheme: "https", Host: "codeload.github.com" })
            throw new InvalidDataException("The Lumenite download redirected outside the official GitHub host.");
        if (response.Content.Headers.ContentLength is > MaximumDownloadBytes)
            throw new InvalidDataException("The Lumenite download was unexpectedly large.");
        await using var input = await response.Content.ReadAsStreamAsync();
        using var memory = new MemoryStream();
        var buffer = new byte[81920];
        while (await input.ReadAsync(buffer) is var read && read > 0)
        {
            if (memory.Length + read > MaximumDownloadBytes)
                throw new InvalidDataException("The Lumenite download exceeded the size limit.");
            await memory.WriteAsync(buffer.AsMemory(0, read));
        }
        var archiveBytes = memory.ToArray();
        if (!Convert.ToHexString(SHA256.HashData(archiveBytes)).Equals(LumeniteHash, StringComparison.OrdinalIgnoreCase))
            throw new InvalidDataException("The pinned Lumenite download failed SHA-256 verification.");

        var files = new List<InstallSource>();
        using var archive = new ZipArchive(new MemoryStream(archiveBytes), ZipArchiveMode.Read);
        foreach (var entry in archive.Entries.Where(entry => entry.Length > 0))
        {
            var name = entry.FullName.Replace('\\', '/');
            string? relative = name.EndsWith("/Shaders/lumenite_Kernel.fx", StringComparison.OrdinalIgnoreCase)
                ? Path.Combine("reshade-shaders", "Shaders", "lumenite_Kernel.fx")
                : name.Contains("/Shaders/include/", StringComparison.OrdinalIgnoreCase) &&
                  name.EndsWith(".fxh", StringComparison.OrdinalIgnoreCase)
                    ? Path.Combine("reshade-shaders", "Shaders", "include", name[(name.IndexOf("/Shaders/include/", StringComparison.OrdinalIgnoreCase) + 17)..])
                    : name.EndsWith("/Textures/lumenite_bluenoise256.png", StringComparison.OrdinalIgnoreCase)
                        ? Path.Combine("reshade-shaders", "Textures", "lumenite_bluenoise256.png")
                        : null;
            if (relative is null) continue;
            var destination = SafeStagePath(staging, relative);
            Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
            entry.ExtractToFile(destination, true);
            files.Add(new InstallSource(destination, relative));
        }
        if (!files.Any(file => file.TargetName.EndsWith("lumenite_Kernel.fx", StringComparison.OrdinalIgnoreCase)) ||
            !files.Any(file => file.TargetName.EndsWith("lumenite_bluenoise256.png", StringComparison.OrdinalIgnoreCase)) ||
            !files.Any(file => file.TargetName.EndsWith(".fxh", StringComparison.OrdinalIgnoreCase)))
            throw new InvalidDataException("The pinned Lumenite archive did not contain the required Kernel 2.0 files.");
        return files;
    }

    internal static string BuildReShadeIni(string text)
    {
        text = SetIniValue(text, "GENERAL", "EffectSearchPaths",
            MergeList(GetIniValue(text, "GENERAL", "EffectSearchPaths"), @".\reshade-shaders\Shaders"));
        text = SetIniValue(text, "GENERAL", "TextureSearchPaths",
            MergeList(GetIniValue(text, "GENERAL", "TextureSearchPaths"), @".\reshade-shaders\Textures"));
        return SetIniValue(text, "GENERAL", "PreprocessorDefinitions",
            MergeDefinitions(GetIniValue(text, "GENERAL", "PreprocessorDefinitions")));
    }

    internal static string BuildPreset(string text)
    {
        var techniques = MergeTechniques(GetIniValue(text, "", "Techniques"));
        var sorting = MergeTechniques(GetIniValue(text, "", "TechniqueSorting"));
        text = SetIniValue(text, "", "Techniques", techniques);
        text = SetIniValue(text, "", "TechniqueSorting", sorting);
        return SetIniValue(text, "DLSS5_Feed.fx", "PreprocessorDefinitions", "DLSS5_MV_PROVIDER=3");
    }

    internal static string BuildFeedConfig(string text)
    {
        text = SetIniValue(text, "", "managed_non_dlss", "1");
        text = SetIniValue(text, "", "enabled", "1");
        return SetIniValue(text, "", "mode", "2");
    }

    internal static string ResolvePresetTarget(string gameDirectory, string? configuredPath)
    {
        if (string.IsNullOrWhiteSpace(configuredPath)) return "ReShadePreset.ini";
        try
        {
            var value = configuredPath.Trim().Trim('"').Replace('/', Path.DirectorySeparatorChar);
            var root = Path.GetFullPath(gameDirectory).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
            var full = Path.GetFullPath(Path.IsPathRooted(value) ? value : Path.Combine(root, value));
            return full.StartsWith(root, StringComparison.OrdinalIgnoreCase)
                ? Path.GetRelativePath(root, full)
                : "ReShadePreset.ini";
        }
        catch { return "ReShadePreset.ini"; }
    }

    private static string MergeDefinitions(string? value) => string.Join(',', SplitList(value)
        .Where(item => !item.StartsWith("DLSS5_MV_PROVIDER=", StringComparison.OrdinalIgnoreCase))
        .Append("DLSS5_MV_PROVIDER=3"));

    private static string MergeTechniques(string? value) => string.Join(',', SplitList(value)
        .Where(item => !item.Equals("Lumenite_Kernel@lumenite_Kernel.fx", StringComparison.OrdinalIgnoreCase) &&
                       !item.Equals("DLSS5_Feed@DLSS5_Feed.fx", StringComparison.OrdinalIgnoreCase))
        .Concat(["Lumenite_Kernel@lumenite_Kernel.fx", "DLSS5_Feed@DLSS5_Feed.fx"]));

    private static string MergeList(string? value, string required) => string.Join(',', SplitList(value)
        .Where(item => !item.Equals(required, StringComparison.OrdinalIgnoreCase)).Append(required));

    private static IEnumerable<string> SplitList(string? value) => (value ?? "")
        .Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);

    private static string? GetIniValue(string text, string section, string key)
    {
        var activeSection = "";
        foreach (var raw in Lines(text))
        {
            var line = raw.Trim();
            if (line.StartsWith('[') && line.EndsWith(']')) activeSection = line[1..^1].Trim();
            else if (activeSection.Equals(section, StringComparison.OrdinalIgnoreCase))
            {
                var parts = line.Split('=', 2, StringSplitOptions.TrimEntries);
                if (parts.Length == 2 && parts[0].Equals(key, StringComparison.OrdinalIgnoreCase)) return parts[1];
            }
        }
        return null;
    }

    private static string SetIniValue(string text, string section, string key, string value)
    {
        var lines = Lines(text).ToList();
        var activeSection = "";
        var sectionFound = section.Length == 0;
        var insertAt = sectionFound ? lines.FindIndex(line => line.TrimStart().StartsWith('[')) : -1;
        if (insertAt < 0 && sectionFound) insertAt = lines.Count;
        for (var index = 0; index < lines.Count; ++index)
        {
            var line = lines[index].Trim();
            if (line.StartsWith('[') && line.EndsWith(']'))
            {
                if (activeSection.Equals(section, StringComparison.OrdinalIgnoreCase) && insertAt < 0) insertAt = index;
                activeSection = line[1..^1].Trim();
                if (activeSection.Equals(section, StringComparison.OrdinalIgnoreCase))
                {
                    sectionFound = true;
                    insertAt = index + 1;
                }
                continue;
            }
            if (!activeSection.Equals(section, StringComparison.OrdinalIgnoreCase)) continue;
            var parts = line.Split('=', 2, StringSplitOptions.TrimEntries);
            if (parts.Length == 2 && parts[0].Equals(key, StringComparison.OrdinalIgnoreCase))
            {
                lines[index] = $"{key}={value}";
                return string.Join(Environment.NewLine, lines) + Environment.NewLine;
            }
            insertAt = index + 1;
        }
        if (!sectionFound)
        {
            if (lines.Count > 0 && lines[^1].Length > 0) lines.Add("");
            lines.Add($"[{section}]");
            insertAt = lines.Count;
        }
        lines.Insert(Math.Max(0, insertAt), $"{key}={value}");
        return string.Join(Environment.NewLine, lines) + Environment.NewLine;
    }

    private static IEnumerable<string> Lines(string text) => text.Replace("\r\n", "\n").Split('\n')
        .Where((line, index) => index < text.Replace("\r\n", "\n").Split('\n').Length - 1 || line.Length > 0);

    private static string ReadText(string path)
    {
        try { return File.Exists(path) ? File.ReadAllText(path) : ""; }
        catch { return ""; }
    }

    private static InstallSource StageText(string staging, string target, string contents)
    {
        var path = SafeStagePath(staging, target);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, contents);
        return new InstallSource(path, target);
    }

    private static InstallSource StageCopy(string source, string staging, string target)
    {
        var path = SafeStagePath(staging, target);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.Copy(source, path, true);
        return new InstallSource(path, target);
    }

    private static string SafeStagePath(string staging, string relative)
    {
        var root = Path.GetFullPath(staging).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
        var path = Path.GetFullPath(Path.Combine(root, relative));
        if (!path.StartsWith(root, StringComparison.OrdinalIgnoreCase))
            throw new InvalidDataException("A staged file escaped the temporary directory.");
        return path;
    }
}

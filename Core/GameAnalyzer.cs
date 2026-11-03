using System.Runtime.InteropServices;
using System.Diagnostics;
using System.Reflection.PortableExecutable;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace DLSS5ManAger.Core;

public sealed class GameAnalyzer
{
    [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
    private static extern IntPtr ExtractAssociatedIcon(IntPtr instance, StringBuilder iconPath, ref ushort iconIndex);

    [DllImport("user32.dll")]
    private static extern bool DestroyIcon(IntPtr icon);

    private static readonly HashSet<string> IgnoredExecutables = new(StringComparer.OrdinalIgnoreCase)
    {
        "crashreporter.exe", "crashpad_handler.exe", "unins000.exe", "setup.exe", "launcher.exe"
    };
    private static readonly string[] IgnoredExecutableFragments =
        ["benchmark", "config", "crash", "creationkit", "ffmpeg", "installer", "redist", "setup", "unins", "updater",
         "helper", "subprocess", "patch", "safemode", "handler", "vcredist", "vc_redist", "dotnet", "ndp4", "dxsetup",
         "start_protected", "easyanticheat", "battleye", "beservice", "webview", "launcher"];
    // Executables below these folders are engine/redistributable tooling, never the game itself.
    private static readonly string[] ToolingPathSegments =
        ["\\Engine\\Binaries\\", "\\Engine\\Extras\\", "\\ThirdParty\\", "\\_CommonRedist\\", "\\Redist\\", "\\Redistributables\\",
         "\\EasyAntiCheat\\", "\\BattlEye\\", "\\Crashpad\\"];
    private static readonly Regex NonGameName = new(
        @"steamworks|steamvr|steam linux runtime|proton|redist|controller config|wallpaper[ _]?engine|lossless scaling|borderless gaming|" +
        @"ovr[ _]?(drop|advanced)|^dsx$|xxmi|hoyoplay|launcher|reshade|\bpatch\b|benchmark|dedicated server|\bsdk\b|\beditor\b|mod tools|" +
        @"workshop|3dmark|obs studio",
        RegexOptions.IgnoreCase | RegexOptions.Compiled | RegexOptions.CultureInvariant);
    private static readonly HashSet<string> SkippedDirectories = new(StringComparer.OrdinalIgnoreCase)
    {
        "_DLSS5_Backups", "__Installer", "_CommonRedist", "CommonRedist", "Redistributables", "EasyAntiCheat", "BattlEye", "Crashpad", "assets", "audio", "backup", "backups", "cache", "content", "data", "dlc", "libs", "localization", "mods", "movies", "packs", "redist", "screenshots", "tools"
    };
    private static readonly (string Api, string[] Tokens)[] ApiTokens =
    [
        ("Vulkan", ["vulkan-1.dll", "vulkan"]),
        ("DX12", ["d3d12.dll", "direct3d 12", "d3d12"]),
        ("DX11", ["d3d11.dll", "direct3d 11", "d3d11"]),
        ("DX10", ["d3d10.dll", "direct3d 10", "d3d10"]),
        ("DX9", ["d3d9.dll", "direct3d 9", "d3d9"]),
        ("OpenGL", ["opengl32.dll", "opengl"])
    ];
    private static readonly Regex AppIdPattern = new("\"appid\"\\s+\"(?<value>\\d+)\"", RegexOptions.IgnoreCase);
    private static readonly Regex ManifestName = new("\"name\"\\s+\"(?<value>[^\"]+)\"", RegexOptions.IgnoreCase);
    private static readonly Regex InstallDirPattern = new("\"installdir\"\\s+\"(?<value>[^\"]+)\"", RegexOptions.IgnoreCase);

    public GameEntry Analyze(string directory)
    {
        var fullPath = DirectoryPath.Normalize(directory);
        var files = EnumerateRelevantFiles(fullPath).ToArray();
        var names = files.Select(Path.GetFileName).OfType<string>()
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var executables = RankExecutables(files.Where(path => path.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
            .Where(path => !IsIgnoredExecutable(path)).ToArray(), files);
        var steam = FindSteamManifest(fullPath);
        var displayName = steam.Name is { Length: > 0 } ? steam.Name : GameNaming.Resolve(fullPath);

        var apis = DetectApis(names, executables, files, fullPath, displayName, out var apiSource);
        var graphicsApi = apis.Count == 0 ? "Unknown" : string.Join(" / ", apis);
        var executableDirectory = Path.GetDirectoryName(executables.FirstOrDefault());
        var installFiles = executableDirectory is null ? [] : files
            .Where(path => DirectoryPath.Comparer.Equals(Path.GetDirectoryName(path), executableDirectory))
            .ToArray();
        var installNames = installFiles.Select(Path.GetFileName).OfType<string>()
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var addonPath = installFiles.FirstOrDefault(path => path.EndsWith(".addon64", StringComparison.OrdinalIgnoreCase) &&
            Path.GetFileName(path).Contains("renodx-dlss", StringComparison.OrdinalIgnoreCase));
        var hasAddon = addonPath is not null;
        var hasIntegratedFeeder = addonPath is not null && ReadEvidence(addonPath)
            .Contains("embedded feeder overlay disabled", StringComparison.Ordinal);
        var reshadeModules = installFiles.Where(ReShadeService.IsReShadeModule).ToArray();
        var hasReShade = installNames.Contains("ReShade.ini") || installNames.Contains("ReShade.log") ||
            executableDirectory is not null && Directory.Exists(Path.Combine(executableDirectory, "reshade-shaders")) ||
            reshadeModules.Length > 0;
        var expectedProxy = ReShadeService.ExpectedProxyName(graphicsApi);
        var usesIntegratedFeeder = executableDirectory is not null &&
            HasEnabledSetting(Path.Combine(executableDirectory, "dlss5-feed.cfg"), "managed_non_dlss");
        var hasIntegratedFeederSetup = executableDirectory is not null &&
            File.Exists(Path.Combine(executableDirectory, "reshade-shaders", "Shaders", "DLSS5_Feed.fx")) &&
            File.Exists(Path.Combine(executableDirectory, "reshade-shaders", "Shaders", "lumenite_Kernel.fx"));
        var (hasNativeDlssSupport, nativeEvidence) = DetectNativeDlss(files, executables);
        var nonGameReason = executables.Length == 0 ? "no game executable found"
            : NonGameName.Match(displayName) is { Success: true } match ? $"name matches '{match.Value}'" : null;

        var game = new GameEntry
        {
            Name = displayName,
            GameDirectory = fullPath,
            ExecutablePath = executables.FirstOrDefault(),
            HasReShade = hasReShade,
            HasReShadeProxyMismatch = expectedProxy is not null && reshadeModules.Length > 0 &&
                !reshadeModules.Any(path => Path.GetFileName(path).Equals(expectedProxy, StringComparison.OrdinalIgnoreCase)),
            ReShadeModulePaths = reshadeModules,
            HasAddon = hasAddon,
            HasIntegratedFeeder = hasIntegratedFeeder,
            HasDlss = installNames.Contains("nvngx_dlss.dll"),
            HasDlssG = installNames.Contains("nvngx_dlssg.dll"),
            HasDlssNr = installNames.Contains("nvngx_dlssnr.dll"),
            UsesIntegratedFeeder = usesIntegratedFeeder,
            HasIntegratedFeederSetup = hasIntegratedFeederSetup,
            HasNativeDlssSupport = hasNativeDlssSupport,
            Is64Bit = Is64BitExecutable(executables.FirstOrDefault()),
            SteamAppId = steam.AppId,
            GraphicsApi = graphicsApi,
            ApiSource = apiSource,
            NativeDlssEvidence = nativeEvidence,
            NonGameReason = nonGameReason
        };
        game.IconSource = LoadIcon(game.ExecutablePath);

        game.Status = hasAddon ? "Installed" : hasReShade ? "Ready" : "ReShade not detected";
        game.Details = string.Join("  •  ", new[]
        {
            game.ExecutablePath is null ? "No game executable found" : Path.GetFileName(game.ExecutablePath),
            hasReShade ? "ReShade detected" : "ReShade not detected",
            game.UsesDx12 && game.HasDlss ? "DLSS SR"
                : usesIntegratedFeeder ? "Integrated DLSS 5 Feed"
                : game.RequiresIntegratedFeeder && game.HasDlss ? "Integrated feed repair needed"
                : game.HasDlss ? "DLSS SR" : "No DLSS SR",
            game.HasDlssG ? "DLSS FG" : "No DLSS FG",
            game.HasDlssNr ? "DLSS NR" : "No DLSS NR"
        });
        return game;
    }

    private static readonly Regex IntegrationModuleName = new(@"(^|[._\- ])(dlss|ngx|streamline)|^sl\.",
        RegexOptions.IgnoreCase | RegexOptions.Compiled | RegexOptions.CultureInvariant);
    private static readonly HashSet<string> EngineModules = new(StringComparer.OrdinalIgnoreCase)
        { "UnityPlayer.dll", "GameAssembly.dll", "engine.dll", "renderer.dll" };

    /// <summary>
    /// Native DLSS support means the game integrates NGX/Streamline itself. Evidence comes from Streamline/Unreal plugin
    /// files and from scanning the whole executable and its integration modules for NGX markers (ASCII and UTF-16).
    /// </summary>
    private static (bool Native, string Evidence) DetectNativeDlss(IEnumerable<string> files, IEnumerable<string> executables)
    {
        var fileArray = files.ToArray();
        var evidence = new List<string>();
        if (fileArray.Any(path => Path.GetFileName(path).Equals("sl.dlss.dll", StringComparison.OrdinalIgnoreCase)))
            evidence.Add("sl.dlss.dll present");
        if (fileArray.Any(path => path.Contains("\\Engine\\Plugins\\Runtime\\Nvidia\\DLSS\\", StringComparison.OrdinalIgnoreCase)))
            evidence.Add("Unreal DLSS plugin present");

        var primary = executables.FirstOrDefault();
        var scanned = new List<string>();
        if (primary is not null) scanned.Add(primary);
        if (primary is not null && Path.GetDirectoryName(primary) is { } directory)
            scanned.AddRange(fileArray.Where(path => path.EndsWith(".dll", StringComparison.OrdinalIgnoreCase) &&
                    DirectoryPath.Comparer.Equals(Path.GetDirectoryName(path), directory) &&
                    !Path.GetFileName(path).StartsWith("nvngx_", StringComparison.OrdinalIgnoreCase) && !ReShadeService.IsReShadeModule(path) &&
                    (IntegrationModuleName.IsMatch(Path.GetFileName(path)) || EngineModules.Contains(Path.GetFileName(path))))
                .OrderByDescending(SafeLength).Take(6));
        foreach (var path in scanned.Distinct(DirectoryPath.Comparer))
        {
            var found = BinaryEvidence.Scan(path);
            if (found.NativeDlss) evidence.AddRange(found.Describe().Select(item => $"{Path.GetFileName(path)}: {item}"));
        }
        return (evidence.Count > 0, string.Join("; ", evidence));
    }

    /// <summary>
    /// Best candidate first: 64-bit, beside Steam/graphics/DLSS files, larger, in a Binaries/bin folder; engine tooling,
    /// VR variants of a flat-screen executable and 32-bit helpers sort last.
    /// </summary>
    private static string[] RankExecutables(string[] executables, string[] files)
    {
        if (executables.Length <= 1) return executables;
        var siblings = files.GroupBy(path => Path.GetDirectoryName(path) ?? "", DirectoryPath.Comparer)
            .ToDictionary(group => group.Key, group => group.Select(Path.GetFileName).OfType<string>()
                .ToHashSet(StringComparer.OrdinalIgnoreCase), DirectoryPath.Comparer);
        var executableNames = executables.Select(Path.GetFileName).OfType<string>().ToHashSet(StringComparer.OrdinalIgnoreCase);
        string[] gameFiles = ["steam_api64.dll", "steam_api.dll", "ReShade.ini", "nvngx_dlss.dll", "dxgi.dll", "d3d11.dll", "d3d12.dll", "vulkan-1.dll"];
        var gameNames = GameNameKeys(files, executables);
        return executables.Select(path => (Path: path, Score: ScoreExecutable(path, siblings, executableNames, gameFiles, gameNames)))
            .OrderByDescending(item => item.Score).ThenByDescending(item => SafeLength(item.Path))
            .Select(item => item.Path).ToArray();
    }

    private static string[] GameNameKeys(string[] files, string[] executables)
    {
        // Folder names between the files' common root and the executables, flattened to letters and digits.
        var root = executables.Select(Path.GetDirectoryName).OfType<string>().OrderBy(path => path.Length).FirstOrDefault();
        var names = new List<string>();
        for (var directory = root is null ? null : new DirectoryInfo(root); directory is not null && names.Count < 3; directory = directory.Parent)
            names.Add(new string(directory.Name.Where(char.IsLetterOrDigit).ToArray()));
        return names.Where(name => name.Length >= 3).ToArray();
    }

    private static int ScoreExecutable(string path, Dictionary<string, HashSet<string>> siblings,
        HashSet<string> executableNames, string[] gameFiles, string[] gameNames)
    {
        var name = Path.GetFileNameWithoutExtension(path);
        var near = siblings.GetValueOrDefault(Path.GetDirectoryName(path) ?? "");
        var normalized = path.Replace('/', '\\');
        var score = Is64BitExecutable(path) ? 100 : 0;
        if (near is not null && near.Overlaps(gameFiles)) score += 40;
        score += (int)Math.Min(SafeLength(path) / (4L * 1024 * 1024), 40);
        if (normalized.Contains("\\Binaries\\Win64\\", StringComparison.OrdinalIgnoreCase)) score += 15;
        // Where the user already installed ReShade or the add-on is the executable they actually run.
        if (near is not null && (near.Contains("ReShade.ini") || near.Contains("ReShade.log") ||
            near.Any(file => file.EndsWith(".addon64", StringComparison.OrdinalIgnoreCase)))) score += 60;
        // The executable that carries the game's name beats tools and variants shipped beside it.
        var compact = new string(name.Where(char.IsLetterOrDigit).ToArray());
        // Only a tiebreaker between small launcher stubs (Unity variants); real game executables differ in size.
        if (SafeLength(path) < 32L * 1024 * 1024 && compact.Length >= 3 && gameNames.Any(game => game.Contains(compact, StringComparison.OrdinalIgnoreCase))) score += 30;
        if (ToolingPathSegments.Any(segment => normalized.Contains(segment, StringComparison.OrdinalIgnoreCase))) score -= 120;
        var flatName = Regex.Replace(name, @"[ _\-.]*vr$", "", RegexOptions.IgnoreCase);
        if (flatName.Length < name.Length && executableNames.Contains(flatName + ".exe")) score -= 60;
        return score;
    }

    internal static bool HasEnabledSetting(string path, string key)
    {
        try
        {
            return File.ReadLines(path).Any(line =>
            {
                var parts = line.Split('=', 2, StringSplitOptions.TrimEntries);
                return parts.Length == 2 && parts[0].Equals(key, StringComparison.OrdinalIgnoreCase) &&
                    parts[1].Equals("1", StringComparison.OrdinalIgnoreCase);
            });
        }
        catch { return false; }
    }

    private static bool Is64BitExecutable(string? path)
    {
        try
        {
            using var stream = File.OpenRead(path!);
            using var reader = new PEReader(stream);
            return reader.PEHeaders.CoffHeader.Machine == Machine.Amd64;
        }
        catch { return false; }
    }

    private static List<string> DetectApis(HashSet<string> names, IEnumerable<string> executables, IEnumerable<string> files,
        string directory, string displayName, out string source)
    {
        var executableArray = executables.Take(6).ToArray();
        var fileArray = files.ToArray();
        source = "";
        var active = DetectActiveApis(fileArray);
        if (active.Count > 0) { source = "ReShade.log"; return Ordered(active); }
        var (fromGame, gameSource) = ApiEvidence.FromGameFiles(directory, executableArray.FirstOrDefault(), displayName);
        if (fromGame.Count > 0) { source = gameSource; return Ordered(fromGame.ToHashSet(StringComparer.OrdinalIgnoreCase)); }
        source = "static file evidence";
        files = fileArray;

        if (names.Any(name => name.Equals("dxil.dll", StringComparison.OrdinalIgnoreCase) ||
                              name.Equals("D3D12Core.dll", StringComparison.OrdinalIgnoreCase))) return ["DX12"];
        if (names.Contains("UnityPlayer.dll") ||
            executableArray.Any(exe => File.Exists(Path.Combine(Path.GetDirectoryName(exe) ?? "",
                Path.GetFileNameWithoutExtension(exe) + "_Data", "globalgamemanagers")))) return ["DX11"];   // Unity's Windows default

        var imported = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var engineModules = files.Where(path => new[] { "UnityPlayer.dll", "GameAssembly.dll", "engine.dll", "renderer.dll" }
            .Contains(Path.GetFileName(path), StringComparer.OrdinalIgnoreCase));
        foreach (var path in executableArray.Take(1).Concat(engineModules).Take(5))
            AddApiMatches(string.Join('\n', ReadImportedDlls(path)), imported);
        if (imported.Count > 0) return Ordered(imported);

        var named = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var localFiles = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var executableNames = string.Join('\n', executableArray.Select(Path.GetFileName));
        if (executableNames.Contains("vulkan", StringComparison.OrdinalIgnoreCase) ||
            executableNames.Contains("x64vk", StringComparison.OrdinalIgnoreCase)) named.Add("Vulkan");
        foreach (var (api, clues) in new[]
        {
            ("DX12", new[] { "dx12", "d3d12" }), ("DX11", new[] { "dx11", "d3d11" }),
            ("DX10", new[] { "dx10", "d3d10" }), ("DX9", new[] { "dx9", "d3d9" }),
            ("OpenGL", new[] { "opengl" })
        })
            if (clues.Any(clue => executableNames.Contains(clue, StringComparison.OrdinalIgnoreCase)))
                named.Add(api);
        if (named.Count > 0) return Ordered(named);

        AddApiMatches(string.Join('\n', names), localFiles);
        if (names.Contains("UnityPlayer.dll") && !localFiles.Contains("Vulkan")) localFiles.Add("DX11");
        if (localFiles.Count > 0) return Ordered(localFiles);

        var binary = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        if (executableArray.FirstOrDefault() is { } executable) ScanApiEvidence(executable, binary);
        return Ordered(binary);
    }

    private static HashSet<string> DetectActiveApis(IEnumerable<string> files)
    {
        foreach (var log in files.Where(path => Path.GetFileName(path).Equals("ReShade.log", StringComparison.OrdinalIgnoreCase))
                     .OrderByDescending(SafeLastWrite))
        {
            var evidence = ReadEvidence(log);
            // The first statement is the game's own device; later ones are secondary (interop/overlay) devices.
            var explicitApi = Regex.Match(evidence, @"api:\s*(d3d12|d3d11|d3d10|d3d9|vulkan|opengl)\b", RegexOptions.IgnoreCase);
            if (explicitApi.Success) return [MapApi(explicitApi.Groups[1].Value)];
            var redirectedApi = Regex.Match(evidence,
                @"Redirecting (D3D12CreateDevice|D3D11CreateDevice|D3D10CreateDevice|Direct3DCreate9|vkCreate|wglCreateContext)",
                RegexOptions.IgnoreCase);
            if (redirectedApi.Success) return [MapApi(redirectedApi.Groups[1].Value)];
        }
        return new(StringComparer.OrdinalIgnoreCase);
    }

    private static string MapApi(string evidence) => evidence.ToLowerInvariant() switch
    {
        var value when value.Contains("d3d12") => "DX12",
        var value when value.Contains("d3d11") => "DX11",
        var value when value.Contains("d3d10") => "DX10",
        var value when value.Contains("d3d9") || value.Contains("direct3dcreate9") => "DX9",
        var value when value.Contains("vulkan") || value.Contains("vkcreate") => "Vulkan",
        _ => "OpenGL"
    };

    private static void ScanApiEvidence(string path, HashSet<string> result)
    {
        try
        {
            using var stream = File.OpenRead(path);
            var buffer = new byte[1024 * 1024];
            var tail = "";
            long remaining = Math.Min(stream.Length, 128L * 1024 * 1024);
            while (remaining > 0)
            {
                var read = stream.Read(buffer, 0, (int)Math.Min(buffer.Length, remaining));
                if (read == 0) break;
                var evidence = tail + Encoding.ASCII.GetString(buffer, 0, read);
                AddApiMatches(evidence, result);
                tail = evidence[^Math.Min(64, evidence.Length)..];
                remaining -= read;
            }
        }
        catch { }
    }

    private static string ReadEvidence(string path)
    {
        try
        {
            using var stream = File.OpenRead(path);
            const int sampleSize = 4 * 1024 * 1024;
            var first = new byte[Math.Min(sampleSize, checked((int)Math.Min(stream.Length, int.MaxValue)))];
            stream.ReadExactly(first);
            if (stream.Length <= sampleSize) return Encoding.ASCII.GetString(first);
            stream.Position = Math.Max(sampleSize, stream.Length - sampleSize);
            var last = new byte[checked((int)(stream.Length - stream.Position))];
            stream.ReadExactly(last);
            return Encoding.ASCII.GetString(first) + Encoding.ASCII.GetString(last);
        }
        catch { return ""; }
    }

    private static IEnumerable<string> ReadImportedDlls(string path)
    {
        try
        {
            using var stream = File.OpenRead(path);
            using var reader = new PEReader(stream, PEStreamOptions.LeaveOpen);
            var headers = reader.PEHeaders;
            var directory = headers.PEHeader?.ImportTableDirectory;
            if (directory is null || directory.Value.RelativeVirtualAddress == 0) return [];
            var descriptorOffset = RvaToOffset(headers, directory.Value.RelativeVirtualAddress);
            if (descriptorOffset < 0) return [];

            var names = new List<string>();
            using var binary = new BinaryReader(stream, Encoding.ASCII, true);
            stream.Position = descriptorOffset;
            while (stream.Position + 20 <= stream.Length)
            {
                var originalThunk = binary.ReadUInt32();
                var timestamp = binary.ReadUInt32();
                var forwarder = binary.ReadUInt32();
                var nameRva = binary.ReadUInt32();
                var firstThunk = binary.ReadUInt32();
                if ((originalThunk | timestamp | forwarder | nameRva | firstThunk) == 0) break;
                var nextDescriptor = stream.Position;
                var nameOffset = RvaToOffset(headers, checked((int)nameRva));
                if (nameOffset >= 0)
                {
                    stream.Position = nameOffset;
                    var bytes = new List<byte>(32);
                    while (stream.Position < stream.Length && bytes.Count < 260)
                    {
                        var value = binary.ReadByte();
                        if (value == 0) break;
                        bytes.Add(value);
                    }
                    if (bytes.Count > 0) names.Add(Encoding.ASCII.GetString(bytes.ToArray()));
                }
                stream.Position = nextDescriptor;
            }
            return names;
        }
        catch { return []; }
    }

    private static int RvaToOffset(PEHeaders headers, int rva)
    {
        foreach (var section in headers.SectionHeaders)
        {
            var size = Math.Max(section.VirtualSize, section.SizeOfRawData);
            if (rva >= section.VirtualAddress && rva < section.VirtualAddress + size)
                return rva - section.VirtualAddress + section.PointerToRawData;
        }
        return -1;
    }

    private static void AddApiMatches(string evidence, HashSet<string> result)
    {
        foreach (var (api, tokens) in ApiTokens)
            if (tokens.Any(token => evidence.Contains(token, StringComparison.OrdinalIgnoreCase))) result.Add(api);
        if (evidence.Contains("dxil.dll", StringComparison.OrdinalIgnoreCase)) result.Add("DX12");
    }

    private static List<string> Ordered(HashSet<string> result) =>
        ApiTokens.Select(item => item.Api).Where(result.Contains).ToList();

    private static IEnumerable<string> EnumerateRelevantFiles(string root)
    {
        var pending = new Queue<(string Path, int Depth)>();
        pending.Enqueue((root, 0));
        while (pending.TryDequeue(out var current))
        {
            foreach (var file in SafeFiles(current.Path)) yield return file;
            if (current.Depth >= 5) continue;
            foreach (var child in SafeDirectories(current.Path))
            {
                if (SkippedDirectories.Contains(Path.GetFileName(child)) || IsReparsePoint(child)) continue;
                pending.Enqueue((child, current.Depth + 1));
            }
        }
    }

    private static IEnumerable<string> SafeFiles(string directory)
    {
        try { return Directory.EnumerateFiles(directory).ToArray(); }
        catch { return []; }
    }

    private static IEnumerable<string> SafeDirectories(string directory)
    {
        try { return Directory.EnumerateDirectories(directory).ToArray(); }
        catch { return []; }
    }

    private static long SafeLength(string path)
    {
        try { return new FileInfo(path).Length; }
        catch { return 0; }
    }

    private static bool IsIgnoredExecutable(string path)
    {
        var name = Path.GetFileName(path);
        return IgnoredExecutables.Contains(name) ||
               IgnoredExecutableFragments.Any(fragment => name.Contains(fragment, StringComparison.OrdinalIgnoreCase)) ||
               ToolingPathSegments.Any(segment => path.Replace('/', '\\').Contains(segment, StringComparison.OrdinalIgnoreCase));
    }

    private static DateTime SafeLastWrite(string path)
    {
        try { return File.GetLastWriteTimeUtc(path); }
        catch { return DateTime.MinValue; }
    }

    private static bool IsReparsePoint(string path)
    {
        try { return File.GetAttributes(path).HasFlag(FileAttributes.ReparsePoint); }
        catch { return true; }
    }

    private static (string? AppId, string? Name) FindSteamManifest(string path)
    {
        try
        {
            for (var gameRoot = new DirectoryInfo(path); gameRoot.Parent?.Parent is not null; gameRoot = gameRoot.Parent)
            {
                if (!gameRoot.Parent.Name.Equals("common", StringComparison.OrdinalIgnoreCase) ||
                    !gameRoot.Parent.Parent!.Name.Equals("steamapps", StringComparison.OrdinalIgnoreCase)) continue;
                foreach (var manifest in Directory.EnumerateFiles(gameRoot.Parent.Parent.FullName, "appmanifest_*.acf"))
                {
                    var text = File.ReadAllText(manifest);
                    if (!string.Equals(InstallDirPattern.Match(text).Groups["value"].Value, gameRoot.Name,
                        StringComparison.OrdinalIgnoreCase)) continue;
                    var appId = AppIdPattern.Match(text).Groups["value"].Value;
                    var title = ManifestName.Match(text).Groups["value"].Value.Trim();
                    return (appId.Length == 0 ? null : appId, title.Length == 0 ? null : title);
                }
                return (null, null);
            }
        }
        catch { }
        return (null, null);
    }

    private static ImageSource? LoadIcon(string? executable)
    {
        if (executable is null) return null;
        IntPtr icon = IntPtr.Zero;
        try
        {
            var iconPath = new StringBuilder(executable, Math.Max(260, executable.Length + 1));
            ushort iconIndex = 0;
            icon = ExtractAssociatedIcon(IntPtr.Zero, iconPath, ref iconIndex);
            if (icon == IntPtr.Zero) return null;
            var image = Imaging.CreateBitmapSourceFromHIcon(icon, Int32Rect.Empty,
                BitmapSizeOptions.FromWidthAndHeight(32, 32));
            image.Freeze();
            return image;
        }
        catch { return null; }
        finally { if (icon != IntPtr.Zero) DestroyIcon(icon); }
    }
}

using Microsoft.Win32;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace DLSS5ManAger.Core;

public sealed class GameScanner
{
    private static readonly Regex SteamPath = new("\"path\"\\s+\"(?<path>[^\"]+)\"",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);
    private static readonly HashSet<string> ExcludedDirectories = new(StringComparer.OrdinalIgnoreCase)
    {
        "$Recycle.Bin", "$Windows.~BT", ".git", "node_modules", "ProgramData", "System Volume Information", "Windows", "WindowsApps",
        // Tooling and redistributables inside game folders are never games of their own.
        "Tools", "_CommonRedist", "Redist", "Redistributables", "ThirdParty", "EasyAntiCheat", "BattlEye", "Crashpad"
    };
    // Steam's own redistributables share steamapps\common with games.
    private static readonly HashSet<string> NonGameSteamApps = new() { "228980" };
    private static readonly Regex ManifestValue = new("\"(?<key>appid|installdir|StateFlags)\"\\s+\"(?<value>[^\"]*)\"",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);
    private static readonly HashSet<string> GameMarkers = new(StringComparer.OrdinalIgnoreCase)
    {
        "d3d9.dll", "d3d10.dll", "d3d11.dll", "d3d12.dll", "dxgi.dll", "GameAssembly.dll",
        "nvngx_dlss.dll", "nvngx_dlssg.dll", "nvngx_dlssnr.dll", "opengl32.dll", "ReShade.ini",
        "steam_api.dll", "steam_api64.dll", "UnityPlayer.dll", "vulkan-1.dll"
    };
    private static readonly HashSet<string> GameDataDirectories = new(StringComparer.OrdinalIgnoreCase)
    {
        "Binaries", "Content", "Data", "Game", "Paks", "StreamingAssets"
    };

    public IReadOnlyList<string> ScanSteamLibraries()
    {
        var libraries = new HashSet<string>(DirectoryPath.Comparer);
        var steam = FindSteamDirectory();
        if (steam is null) return [];

        libraries.Add(DirectoryPath.Normalize(steam));
        var libraryFile = Path.Combine(steam, "steamapps", "libraryfolders.vdf");
        try
        {
            if (File.Exists(libraryFile))
                foreach (Match match in SteamPath.Matches(File.ReadAllText(libraryFile)))
                    libraries.Add(DirectoryPath.Normalize(match.Groups["path"].Value.Replace("\\\\", "\\")));
        }
        catch { }

        var results = new List<string>();
        foreach (var library in libraries) results.AddRange(ScanSteamLibrary(library));
        return results.Select(DirectoryPath.Normalize).Distinct(DirectoryPath.Comparer)
            .OrderBy(Path.GetFileName, StringComparer.OrdinalIgnoreCase).ToArray();
    }

    /// <summary>Installed games of one Steam library folder (the one that contains steamapps).</summary>
    internal static IEnumerable<string> ScanSteamLibrary(string library)
    {
        var results = new List<string>();
        var steamApps = Path.Combine(library, "steamapps");
        var common = Path.Combine(steamApps, "common");
        if (!Directory.Exists(common)) return results;
        var manifestDirectories = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var manifest in SafeFiles(steamApps, "appmanifest_*.acf"))
        {
            var values = ReadManifest(manifest);
            if (!values.TryGetValue("installdir", out var installDirectory) || installDirectory.Length == 0) continue;
            manifestDirectories.Add(installDirectory);
            if (values.TryGetValue("appid", out var appId) && NonGameSteamApps.Contains(appId)) continue;
            // StateFlags bit 4 = fully installed; a manifest whose files were removed leaves only an empty shell.
            if (values.TryGetValue("StateFlags", out var flags) && int.TryParse(flags, out var state) && (state & 4) == 0) continue;
            var gameDirectory = Path.Combine(common, installDirectory);
            if (Directory.Exists(gameDirectory)) results.Add(gameDirectory);
        }
        // Folders without a manifest (manual copies) count only when they hold an executable.
        foreach (var folder in SafeDirectories(common))
            if (!manifestDirectories.Contains(Path.GetFileName(folder)) && ContainsExecutable(folder, 2)) results.Add(folder);
        return results;
    }

    /// <summary>Steam plus the Epic Games Store, GOG and Xbox app libraries.</summary>
    public IReadOnlyList<string> ScanKnownLibraries() => ScanSteamLibraries().Concat(ScanLauncherLibraries())
        .Select(DirectoryPath.Normalize).Distinct(DirectoryPath.Comparer)
        .OrderBy(Path.GetFileName, StringComparer.OrdinalIgnoreCase).ToArray();

    public IReadOnlyList<string> ScanLauncherLibraries()
    {
        var results = new List<string>();
        results.AddRange(ScanEpic());
        results.AddRange(ScanGog());
        results.AddRange(ScanXbox());
        return results.Where(Directory.Exists).Select(DirectoryPath.Normalize).Distinct(DirectoryPath.Comparer).ToArray();
    }

    private static IEnumerable<string> ScanEpic()
    {
        var manifests = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
            "Epic", "EpicGamesLauncher", "Data", "Manifests");
        foreach (var file in SafeFiles(manifests, "*.item"))
        {
            string? location = null;
            try
            {
                using var document = JsonDocument.Parse(File.ReadAllText(file));
                var item = document.RootElement;
                if (item.TryGetProperty("bIsApplication", out var isApplication) && isApplication.ValueKind == JsonValueKind.False) continue;
                if (item.TryGetProperty("AppCategories", out var categories) && categories.ValueKind == JsonValueKind.Array &&
                    !categories.EnumerateArray().Any(category => string.Equals(category.GetString(), "games", StringComparison.OrdinalIgnoreCase))) continue;
                if (item.TryGetProperty("InstallLocation", out var install)) location = install.GetString();
            }
            catch { }
            if (!string.IsNullOrWhiteSpace(location)) yield return location;
        }
    }

    private static IEnumerable<string> ScanGog()
    {
        var paths = new List<string>();
        try
        {
            using var games = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\WOW6432Node\GOG.com\Games");
            if (games is not null)
                foreach (var name in games.GetSubKeyNames())
                    using (var game = games.OpenSubKey(name))
                        if (game?.GetValue("path") is string path && !string.IsNullOrWhiteSpace(path)) paths.Add(path);
        }
        catch { }
        return paths;
    }

    private static IEnumerable<string> ScanXbox()
    {
        var paths = new List<string>();
        foreach (var drive in DriveInfo.GetDrives().Where(drive => drive.DriveType == DriveType.Fixed && drive.IsReady))
            foreach (var game in SafeDirectories(Path.Combine(drive.RootDirectory.FullName, "XboxGames")))
            {
                var content = Path.Combine(game, "Content");
                paths.Add(Directory.Exists(content) ? content : game);
            }
        return paths;
    }

    private static Dictionary<string, string> ReadManifest(string path)
    {
        var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        try
        {
            foreach (Match match in ManifestValue.Matches(File.ReadAllText(path)))
                values[match.Groups["key"].Value] = match.Groups["value"].Value;
        }
        catch { }
        return values;
    }

    private static bool ContainsExecutable(string directory, int depth)
    {
        try
        {
            if (Directory.EnumerateFiles(directory, "*.exe").Any()) return true;
            return depth > 0 && SafeDirectories(directory).Any(child => ContainsExecutable(child, depth - 1));
        }
        catch { return false; }
    }

    private static string[] SafeFiles(string directory, string pattern)
    {
        try { return Directory.Exists(directory) ? Directory.GetFiles(directory, pattern) : []; }
        catch { return []; }
    }

    public IReadOnlyList<string> ScanDirectory(string searchDirectory)
    {
        var root = DirectoryPath.Normalize(searchDirectory);
        var results = new HashSet<string>(DirectoryPath.Comparer);
        var pending = new Stack<string>();
        pending.Push(root);

        while (pending.TryPop(out var directory))
        {
            var children = SafeDirectories(directory).Where(child => !ShouldSkip(child)).ToArray();
            if (Path.GetFileName(directory).Equals("common", StringComparison.OrdinalIgnoreCase) &&
                Directory.GetParent(directory)?.Name.Equals("steamapps", StringComparison.OrdinalIgnoreCase) == true)
            {
                foreach (var child in children) results.Add(DirectoryPath.Normalize(child));
                continue;
            }

            var files = SafeFiles(directory);
            if (IsGameDirectory(files, children))
            {
                results.Add(DirectoryPath.Normalize(directory));
                continue;
            }

            foreach (var child in children) pending.Push(child);
        }

        return results.OrderBy(Path.GetFileName, StringComparer.OrdinalIgnoreCase).ToArray();
    }

    private static bool IsGameDirectory(IReadOnlyList<string> files, IReadOnlyList<string> children)
    {
        var executables = files.Where(path => path.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
            .Where(path => !Path.GetFileName(path).Contains("unins", StringComparison.OrdinalIgnoreCase)).ToArray();
        if (executables.Length == 0) return false;
        var names = files.Select(Path.GetFileName).OfType<string>().ToHashSet(StringComparer.OrdinalIgnoreCase);
        if (names.Overlaps(GameMarkers)) return true;
        if (children.Any(path => GameDataDirectories.Contains(Path.GetFileName(path))) && executables.Any(path => SafeLength(path) >= 4 * 1024 * 1024))
            return true;
        return executables.Any(path => SafeLength(path) >= 20 * 1024 * 1024);
    }

    private static string? FindSteamDirectory()
    {
        foreach (var key in new[]
        {
            @"HKEY_CURRENT_USER\Software\Valve\Steam",
            @"HKEY_LOCAL_MACHINE\SOFTWARE\WOW6432Node\Valve\Steam"
        })
        {
            var value = Registry.GetValue(key, "SteamPath", null) as string
                ?? Registry.GetValue(key, "InstallPath", null) as string;
            if (!string.IsNullOrWhiteSpace(value) && Directory.Exists(value)) return value;
        }
        return null;
    }

    private static bool ShouldSkip(string path)
    {
        var name = Path.GetFileName(path);
        if (ExcludedDirectories.Contains(name)) return true;
        // An Unreal Engine tree (Engine\Binaries, Plugins, Content ...) holds engine tools, never the game executable.
        if (name.Equals("Engine", StringComparison.OrdinalIgnoreCase) && Directory.Exists(Path.Combine(path, "Binaries")) &&
            (Directory.Exists(Path.Combine(path, "Plugins")) || Directory.Exists(Path.Combine(path, "Config")) ||
             Directory.Exists(Path.Combine(path, "Content")) || Directory.Exists(Path.Combine(path, "Extras")))) return true;
        try { return File.GetAttributes(path).HasFlag(FileAttributes.ReparsePoint); }
        catch { return true; }
    }

    private static string[] SafeDirectories(string directory)
    {
        try { return Directory.GetDirectories(directory); }
        catch { return []; }
    }

    private static string[] SafeFiles(string directory)
    {
        try { return Directory.GetFiles(directory); }
        catch { return []; }
    }

    private static long SafeLength(string path)
    {
        try { return new FileInfo(path).Length; }
        catch { return 0; }
    }
}

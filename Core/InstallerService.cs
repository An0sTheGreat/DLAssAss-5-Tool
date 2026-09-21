using System.Security.Cryptography;
using System.Text.Json;

namespace DLSS5ManAger.Core;

public sealed class InstallResult
{
    public bool Success { get; init; }
    public required string Message { get; init; }
}

public sealed class InstallManifest
{
    public DateTimeOffset CreatedUtc { get; set; }
    public string GameDirectory { get; set; } = "";
    public List<InstalledFile> Files { get; set; } = [];
}

public sealed class InstalledFile
{
    public string TargetName { get; set; } = "";
    public string? BackupName { get; set; }
    public string InstalledHash { get; set; } = "";
}

public sealed record InstallSource(string SourcePath, string TargetName);

public sealed class InstallerService
{
    public const string AddonName = "renodx-dlss5-super-anus.addon64";
    public static IReadOnlyList<string> RequiredDlssFiles { get; } =
        ["nvngx_dlss.dll", "nvngx_dlssg.dll", "nvngx_dlssnr.dll"];
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };
    private readonly string _backupRoot;
    private readonly string _payloadDirectory;

    public InstallerService(string backupRoot, string? payloadDirectory = null)
    {
        _backupRoot = backupRoot;
        _payloadDirectory = payloadDirectory ?? Path.Combine(AppContext.BaseDirectory, "Payload");
    }

    public InstallResult Install(string executablePath, string dlssDirectory, bool includeDlssFiles,
        IEnumerable<InstallSource>? additionalFiles = null)
    {
        var addon = Path.Combine(_payloadDirectory, AddonName);
        if (!File.Exists(addon))
            return Fail($"Manager payload is missing: {addon}");
        var sources = new List<InstallSource> { new(addon, AddonName) };
        if (includeDlssFiles)
        {
            var missing = RequiredDlssFiles.Where(name => !File.Exists(Path.Combine(dlssDirectory, name))).ToArray();
            if (missing.Length > 0) return Fail("Required bundled DLSS files are missing: " + string.Join(", ", missing));
            sources.AddRange(RequiredDlssFiles.Select(name => new InstallSource(Path.Combine(dlssDirectory, name), name)));
        }
        if (additionalFiles is not null) sources.AddRange(additionalFiles);
        return InstallFiles(executablePath, sources);
    }

    public InstallResult ConfigureNonDlss(string executablePath, IEnumerable<InstallSource> files) =>
        InstallFiles(executablePath, files.ToArray());

    private InstallResult InstallFiles(string executablePath, IReadOnlyCollection<InstallSource> sources)
    {
        var gameDirectory = InstallDirectory(executablePath);
        if (gameDirectory is null) return Fail("The selected game executable does not exist.");
        if (!HasReShade(gameDirectory))
            return Fail("ReShade was not found beside the selected game executable. Install ReShade first.");
        if (sources.Count == 0) return Fail("No files were selected for installation.");

        try
        {
            foreach (var source in sources)
            {
                if (!File.Exists(source.SourcePath)) return Fail($"Installation source is missing: {source.SourcePath}");
                _ = ResolveTarget(gameDirectory, source.TargetName);
            }
            if (sources.Select(source => source.TargetName).Distinct(StringComparer.OrdinalIgnoreCase).Count() != sources.Count)
                return Fail("The installation contains duplicate target files.");
        }
        catch (Exception exception) { return Fail($"Invalid installation target: {exception.Message}"); }

        var backupDirectory = Path.Combine(GameBackupDirectory(_backupRoot, gameDirectory),
            DateTimeOffset.UtcNow.ToString("yyyyMMdd-HHmmss-fff"));
        var manifest = new InstallManifest { CreatedUtc = DateTimeOffset.UtcNow, GameDirectory = gameDirectory };
        Directory.CreateDirectory(backupDirectory);

        try
        {
            foreach (var source in sources)
            {
                var target = ResolveTarget(gameDirectory, source.TargetName);
                string? backupName = null;
                if (File.Exists(target))
                {
                    backupName = $"{manifest.Files.Count:D3}.original";
                    File.Copy(target, Path.Combine(backupDirectory, backupName), true);
                }

                var installedFile = new InstalledFile { TargetName = source.TargetName, BackupName = backupName };
                manifest.Files.Add(installedFile);
                ReplaceFile(source.SourcePath, target);
                installedFile.InstalledHash = Hash(target);
            }

            File.WriteAllText(Path.Combine(backupDirectory, "manifest.json"),
                JsonSerializer.Serialize(manifest, JsonOptions));
            return new InstallResult
            {
                Success = true,
                Message = $"Installed {manifest.Files.Count} file(s). Backup: {backupDirectory}"
            };
        }
        catch (Exception exception)
        {
            RollBackPartial(manifest, backupDirectory);
            return Fail($"Installation failed and was rolled back: {exception.Message}");
        }
    }

    public InstallResult UpdateAddon(string executablePath)
    {
        var gameDirectory = InstallDirectory(executablePath);
        if (gameDirectory is null) return Fail("The selected game executable does not exist.");
        if (!File.Exists(Path.Combine(gameDirectory, AddonName)))
            return Fail("The add-on is not installed for this game; no files were changed.");
        return Install(executablePath, "", false);
    }

    public InstallResult RestoreLatest(string executablePath)
    {
        var gameDirectory = InstallDirectory(executablePath);
        if (gameDirectory is null) return Fail("The selected game executable does not exist.");
        var gameBackupRoot = GameBackupDirectory(_backupRoot, gameDirectory);
        var backupDirectory = Directory.Exists(gameBackupRoot)
            ? Directory.EnumerateDirectories(gameBackupRoot).OrderByDescending(path => path).FirstOrDefault()
            : null;
        if (backupDirectory is null) return Fail("No backup exists for this game.");

        try
        {
            var manifestPath = Path.Combine(backupDirectory, "manifest.json");
            var manifest = JsonSerializer.Deserialize<InstallManifest>(File.ReadAllText(manifestPath))
                ?? throw new InvalidDataException("Backup manifest is invalid.");
            var kept = new List<string>();

            foreach (var file in manifest.Files.AsEnumerable().Reverse())
            {
                var target = ResolveTarget(gameDirectory, file.TargetName);
                if (file.BackupName is not null)
                    ReplaceFile(ResolveBackup(backupDirectory, file.BackupName), target);
                else if (File.Exists(target) && Hash(target).Equals(file.InstalledHash, StringComparison.OrdinalIgnoreCase))
                    File.Delete(target);
                else if (File.Exists(target))
                    kept.Add(file.TargetName);
            }

            var suffix = kept.Count == 0 ? "" : $" Modified files kept: {string.Join(", ", kept)}.";
            return new InstallResult { Success = true, Message = "Latest installation restored." + suffix };
        }
        catch (Exception exception)
        {
            return Fail($"Restore failed: {exception.Message}");
        }
    }

    private static void RollBackPartial(InstallManifest manifest, string backupDirectory)
    {
        foreach (var file in manifest.Files.AsEnumerable().Reverse())
        {
            try
            {
                var target = ResolveTarget(manifest.GameDirectory, file.TargetName);
                if (file.BackupName is not null)
                    ReplaceFile(ResolveBackup(backupDirectory, file.BackupName), target);
                else if (File.Exists(target)) File.Delete(target);
            }
            catch { }
        }
    }

    private static void ReplaceFile(string source, string target)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(target)!);
        var temporary = target + $".dlss5manager-{Guid.NewGuid():N}.tmp";
        try
        {
            File.Copy(source, temporary, true);
            File.Move(temporary, target, true);
        }
        finally
        {
            if (File.Exists(temporary)) File.Delete(temporary);
        }
    }

    private static string ResolveTarget(string gameDirectory, string relativePath)
    {
        if (string.IsNullOrWhiteSpace(relativePath) || Path.IsPathRooted(relativePath))
            throw new InvalidDataException("Target paths must be relative to the game directory.");
        var root = Path.GetFullPath(gameDirectory).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
        var target = Path.GetFullPath(Path.Combine(root, relativePath));
        if (!target.StartsWith(root, StringComparison.OrdinalIgnoreCase))
            throw new InvalidDataException($"Target escapes the game directory: {relativePath}");
        return target;
    }

    private static string ResolveBackup(string backupDirectory, string backupName)
    {
        if (!Path.GetFileName(backupName).Equals(backupName, StringComparison.Ordinal))
            throw new InvalidDataException("Backup manifest contains an invalid file name.");
        return Path.Combine(backupDirectory, backupName);
    }

    private static string Hash(string path)
    {
        using var stream = File.OpenRead(path);
        return Convert.ToHexString(SHA256.HashData(stream));
    }

    internal static string? InstallDirectory(string? executablePath)
    {
        try
        {
            return executablePath is not null && File.Exists(executablePath)
                ? Path.GetDirectoryName(Path.GetFullPath(executablePath))
                : null;
        }
        catch { return null; }
    }

    internal static string GameBackupDirectory(string backupRoot, string gameDirectory) =>
        Path.Combine(backupRoot, SafeName(gameDirectory));

    private static bool HasReShade(string directory)
    {
        try
        {
            return File.Exists(Path.Combine(directory, "ReShade.ini")) ||
                File.Exists(Path.Combine(directory, "ReShade.log")) ||
                Directory.EnumerateFiles(directory).Any(ReShadeService.IsReShadeModule);
        }
        catch { return false; }
    }

    private static string SafeName(string path)
    {
        var invalid = Path.GetInvalidFileNameChars().ToHashSet();
        return new string(Path.GetFullPath(path).Select(character => invalid.Contains(character) ? '_' : character).ToArray());
    }

    private static InstallResult Fail(string message) => new() { Success = false, Message = message };
}

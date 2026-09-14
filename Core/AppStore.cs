using System.Text.Json;

namespace DLSS5ManAger.Core;

public sealed class ManagerSettings
{
    public List<string> GameDirectories { get; set; } = [];
    public List<string> SearchDirectories { get; set; } = [];
    public List<string> HiddenGameDirectories { get; set; } = [];
    public Dictionary<string, string> CustomGameNames { get; set; } = new(DirectoryPath.Comparer);
    public Dictionary<string, string> CustomArtworkPaths { get; set; } = new(DirectoryPath.Comparer);
    public string? DlssFilesDirectory { get; set; }
    public bool IsLibraryView { get; set; } = true;
    public double? WindowLeft { get; set; }
    public double? WindowTop { get; set; }
    public double? WindowWidth { get; set; }
    public double? WindowHeight { get; set; }
    public bool WindowMaximized { get; set; }
}

public sealed class AppStore
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    public string DataDirectory { get; }
    public string BackupDirectory => Path.Combine(DataDirectory, "Backups");
    public string LogPath => Path.Combine(DataDirectory, "manager.log");
    private string SettingsPath => Path.Combine(DataDirectory, "settings.json");

    public AppStore(string? dataDirectory = null)
    {
        DataDirectory = dataDirectory ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "DLSS5ManAger");
        Directory.CreateDirectory(DataDirectory);
        Directory.CreateDirectory(BackupDirectory);
    }

    public ManagerSettings Load()
    {
        try
        {
            var settings = File.Exists(SettingsPath)
                ? JsonSerializer.Deserialize<ManagerSettings>(File.ReadAllText(SettingsPath)) ?? new()
                : new();
            settings.GameDirectories = DirectoryPath.NormalizeDistinct(settings.GameDirectories);
            settings.SearchDirectories = DirectoryPath.NormalizeDistinct(settings.SearchDirectories);
            settings.HiddenGameDirectories = DirectoryPath.NormalizeDistinct(settings.HiddenGameDirectories);
            settings.CustomGameNames = NormalizeNames(settings.CustomGameNames);
            settings.CustomArtworkPaths = NormalizePaths(settings.CustomArtworkPaths);
            return settings;
        }
        catch
        {
            return new();
        }
    }

    public void Save(ManagerSettings settings)
    {
        settings.GameDirectories = DirectoryPath.NormalizeDistinct(settings.GameDirectories);
        settings.SearchDirectories = DirectoryPath.NormalizeDistinct(settings.SearchDirectories);
        settings.HiddenGameDirectories = DirectoryPath.NormalizeDistinct(settings.HiddenGameDirectories);
        settings.CustomGameNames = NormalizeNames(settings.CustomGameNames);
        settings.CustomArtworkPaths = NormalizePaths(settings.CustomArtworkPaths);
        Directory.CreateDirectory(DataDirectory);
        File.WriteAllText(SettingsPath, JsonSerializer.Serialize(settings, JsonOptions));
    }

    public void Log(string message)
    {
        Directory.CreateDirectory(DataDirectory);
        File.AppendAllText(LogPath, $"{DateTimeOffset.Now:yyyy-MM-dd HH:mm:ss zzz}  {message}{Environment.NewLine}");
    }

    private static Dictionary<string, string> NormalizeNames(Dictionary<string, string>? names)
    {
        var result = new Dictionary<string, string>(DirectoryPath.Comparer);
        foreach (var pair in names ?? [])
        {
            try
            {
                var name = pair.Value.Trim();
                if (name.Length > 0) result[DirectoryPath.Normalize(pair.Key)] = name;
            }
            catch { }
        }
        return result;
    }

    private static Dictionary<string, string> NormalizePaths(Dictionary<string, string>? paths)
    {
        var result = new Dictionary<string, string>(DirectoryPath.Comparer);
        foreach (var pair in paths ?? [])
            try
            {
                if (!string.IsNullOrWhiteSpace(pair.Value))
                    result[DirectoryPath.Normalize(pair.Key)] = Path.GetFullPath(pair.Value);
            }
            catch { }
        return result;
    }
}

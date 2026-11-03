using System.Text.RegularExpressions;

namespace DLSS5ManAger.Core;

/// <summary>
/// Display names for games whose folder is an inner binary directory (Bin64, Binaries\Win64, LIVE\Bin64 ...).
/// The directory itself stays the install target; only the name is resolved from the enclosing game folder.
/// </summary>
internal static class GameNaming
{
    private static readonly Regex GenericFolder = new(
        @"^(bin(ary|aries)?\d*|win(32|64)\w*|x(64|86|32)(_\w+)?|win_x(64|86)|live|ptu|eptu|public|downloaded|release|shipping|" +
        @"retail|client|game|content|resources|app|application|exe|executables?|program|programs|runtime)$",
        RegexOptions.IgnoreCase | RegexOptions.Compiled | RegexOptions.CultureInvariant);

    public static string Resolve(string directory)
    {
        var start = new DirectoryInfo(directory.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
        var fallback = start.Name.Length > 0 ? start.Name : directory;
        var candidate = start;
        while (candidate.Parent is { } parent)
        {
            // A Steam game root is the folder directly below steamapps\common.
            if (parent.Name.Equals("common", StringComparison.OrdinalIgnoreCase) &&
                parent.Parent?.Name.Equals("steamapps", StringComparison.OrdinalIgnoreCase) == true) break;
            if (GenericFolder.IsMatch(candidate.Name)) { candidate = parent; continue; }
            // Unreal layout: <Game>\Engine and <Game>\<Project>\Binaries\Win64. Move up from the project folder to <Game>.
            if (IsUnrealProject(candidate) && HasEngineFolder(parent)) { candidate = parent; continue; }
            break;
        }
        return candidate.Parent is null || candidate.Name.Length == 0 ? fallback : candidate.Name;
    }

    private static bool IsUnrealProject(DirectoryInfo directory)
    {
        try { return Directory.Exists(Path.Combine(directory.FullName, "Binaries")) && Directory.Exists(Path.Combine(directory.FullName, "Content")); }
        catch { return false; }
    }

    private static bool HasEngineFolder(DirectoryInfo directory)
    {
        try { return Directory.Exists(Path.Combine(directory.FullName, "Engine")); }
        catch { return false; }
    }
}

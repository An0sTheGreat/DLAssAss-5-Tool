// Read-only detection report: runs the manager's GameAnalyzer over game libraries and prints/saves what it detects,
// so detection accuracy can be judged against ground truth. Usage: DetectReport [--csv file] [--steam] [folder...]
using System.Diagnostics;
using System.Text;
using DLSS5ManAger.Core;

internal static class Program
{
    [STAThread]
    private static int Main(string[] args)
    {
        string? csvPath = null;
        var useSteam = false;
        var roots = new List<string>();
        for (var i = 0; i < args.Length; i++)
        {
            if (args[i] == "--csv" && i + 1 < args.Length) csvPath = args[++i];
            else if (args[i] == "--steam") useSteam = true;
            else roots.Add(args[i]);
        }
        var scanner = new GameScanner();
        var directories = new List<string>();
        if (useSteam || roots.Count == 0) directories.AddRange(scanner.ScanKnownLibraries());
        foreach (var root in roots) directories.AddRange(Directory.Exists(root) ? scanner.ScanDirectory(root) : []);
        directories = directories.Distinct(DirectoryPath.Comparer).OrderBy(Path.GetFileName, StringComparer.OrdinalIgnoreCase).ToList();

        var analyzer = new GameAnalyzer();
        var csv = new StringBuilder("game,directory,executable,api,is64,native_dlss,dlss_dll,dlssg_dll,dlssnr_dll,reshade,addon,steam_appid,seconds,non_game,api_source,native_dlss_evidence\n");
        Console.WriteLine($"{"Game",-38} {"Executable (relative)",-52} {"API",-18} {"64",-3} {"NatDLSS",-7} {"DLL",-4} {"NonGame / source",-30} s");
        foreach (var directory in directories)
        {
            var timer = Stopwatch.StartNew();
            var game = analyzer.Analyze(directory);
            timer.Stop();
            var exe = game.ExecutablePath is null ? "(none)" : Path.GetRelativePath(directory, game.ExecutablePath);
            Console.WriteLine($"{Trim(game.Name, 38),-38} {Trim(exe, 52),-52} {Trim(game.GraphicsApi, 18),-18} {(game.Is64Bit ? "y" : "n"),-3} " +
                $"{(game.HasNativeDlssSupport ? "yes" : "no"),-7} {(game.HasDlss ? "S" : "-")}{(game.HasDlssG ? "G" : "-")}{(game.HasDlssNr ? "N" : "-")}  {Trim(game.NonGameReason is null ? game.ApiSource : "NON-GAME: " + game.NonGameReason, 30),-30} {timer.Elapsed.TotalSeconds:0.0}");
            csv.AppendLine(string.Join(',', Quote(game.Name), Quote(directory), Quote(exe), Quote(game.GraphicsApi), game.Is64Bit, game.HasNativeDlssSupport,
                game.HasDlss, game.HasDlssG, game.HasDlssNr, game.HasReShade, game.HasAddon, game.SteamAppId ?? "", timer.Elapsed.TotalSeconds.ToString("0.00"),
                Quote(game.NonGameReason ?? ""), Quote(game.ApiSource), Quote(game.NativeDlssEvidence)));
        }
        Console.WriteLine($"{directories.Count} folders analysed.");
        if (csvPath is not null) File.WriteAllText(csvPath, csv.ToString());
        return 0;
    }

    private static string Trim(string value, int length) => value.Length <= length ? value : value[..(length - 1)] + "…";
    private static string Quote(string value) => '"' + value.Replace("\"", "\"\"") + '"';
}

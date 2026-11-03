using System.Text;
using DLSS5ManAger.Core;

/// <summary>Regression tests for game detection accuracy (names, native DLSS, API, executable choice, library scan).</summary>
internal static class DetectionTests
{
    private const int ChunkSize = 8 * 1024 * 1024;

    public static void Run()
    {
        var root = Path.Combine(Path.GetTempPath(), "DLAssAss5Detect-" + Guid.NewGuid().ToString("N"));
        try
        {
            Naming(root);
            NativeDlss(root);
            Api(root);
            Executables(root);
            Scanner(root);
            NonGames(root);
            Console.WriteLine("PASS: detection accuracy (display names, whole-file NGX evidence, API sources, executable ranking, library scan, non-games).");
        }
        finally
        {
            if (Directory.Exists(root)) Directory.Delete(root, true);
        }
    }

    private static void Naming(string root)
    {
        string Make(params string[] parts) { var path = Path.Combine([root, "naming", .. parts]); Directory.CreateDirectory(path); return path; }
        Require(GameNaming.Resolve(Make("StarCitizen", "LIVE", "Bin64")) == "StarCitizen", "LIVE\\Bin64 name was not resolved to the game folder.");
        Require(GameNaming.Resolve(Make("Crusader Kings III", "binaries")) == "Crusader Kings III", "binaries name was not resolved.");
        Require(GameNaming.Resolve(Make("Soulframe", "Downloaded", "Public")) == "Soulframe", "Downloaded\\Public name was not resolved.");
        Require(GameNaming.Resolve(Make("XXMI Launcher", "Resources", "Bin")) == "XXMI Launcher", "Resources\\Bin name was not resolved.");
        Require(GameNaming.Resolve(Make("Minecraft", "Content")) == "Minecraft", "Content folder name was not resolved.");
        var unreal = Make("The Blood of Example");
        Directory.CreateDirectory(Path.Combine(unreal, "Engine"));
        var project = Make("The Blood of Example", "Example");
        Directory.CreateDirectory(Path.Combine(project, "Content"));
        var win64 = Make("The Blood of Example", "Example", "Binaries", "Win64");
        Require(GameNaming.Resolve(win64) == "The Blood of Example", "Unreal Binaries\\Win64 name was not resolved to the game root.");
        Require(GameNaming.Resolve(Make("Plain Game")) == "Plain Game", "A normal folder name changed.");

        var steam = Path.Combine(root, "naming", "steamapps", "common", "KingdomComeDeliverance2", "Bin", "Win64MasterMasterSteamPGO");
        Directory.CreateDirectory(steam);
        File.WriteAllText(Path.Combine(root, "naming", "steamapps", "appmanifest_9.acf"),
            "\"AppState\" { \"appid\" \"9\" \"name\" \"Kingdom Come: Deliverance II\" \"installdir\" \"KingdomComeDeliverance2\" }");
        File.WriteAllText(Path.Combine(steam, "KingdomCome.exe"), "x");
        Require(new GameAnalyzer().Analyze(Path.Combine(root, "naming", "steamapps", "common", "KingdomComeDeliverance2")).Name ==
            "Kingdom Come: Deliverance II", "The Steam manifest title was not used as the game name.");
    }

    private static void NativeDlss(string root)
    {
        var analyzer = new GameAnalyzer();
        GameEntry Analyze(string name, Action<Stream> write)
        {
            var directory = Path.Combine(root, "dlss", name);
            Directory.CreateDirectory(directory);
            using (var stream = File.Create(Path.Combine(directory, "title.exe"))) write(stream);
            return analyzer.Analyze(directory);
        }
        static void Zeros(Stream stream, long count) { var block = new byte[1 << 20]; for (; count > 0; count -= block.Length) stream.Write(block, 0, (int)Math.Min(block.Length, count)); }

        var wide = Analyze("wide", stream => { Zeros(stream, 20 * 1024 * 1024); stream.Write(Encoding.Unicode.GetBytes("nvngx_dlss.dll")); Zeros(stream, 20 * 1024 * 1024); });
        Require(wide.HasNativeDlssSupport && wide.NativeDlssEvidence.Contains("nvngx_dlss.dll"),
            "A UTF-16 nvngx_dlss.dll name in the middle of a large executable was missed.");
        var ascii = Analyze("ascii", stream => { Zeros(stream, 30 * 1024 * 1024); stream.Write(Encoding.ASCII.GetBytes("NVSDK_NGX_VULKAN_Init")); Zeros(stream, 30 * 1024 * 1024); });
        Require(ascii.HasNativeDlssSupport && ascii.NativeDlssEvidence.Contains("Vulkan"), "An NGX Vulkan marker in the middle of an executable was missed.");
        var boundary = Analyze("boundary", stream => { Zeros(stream, ChunkSize - 5); stream.Write(Encoding.ASCII.GetBytes("NVSDK_NGX_D3D12_Init")); Zeros(stream, 1024); });
        Require(boundary.HasNativeDlssSupport, "A marker straddling the scan chunk boundary was missed.");
        var statics = Analyze("static", stream => { stream.Write(Encoding.ASCII.GetBytes("DLSS_Performance\0DlssEnable\0")); Zeros(stream, 1024); });
        Require(statics.HasNativeDlssSupport && statics.NativeDlssEvidence.Contains("setting strings"),
            "Statically linked NGX (two DLSS setting strings) was not recognised.");
        var one = Analyze("one", stream => { stream.Write(Encoding.ASCII.GetBytes("DLSS_Performance")); Zeros(stream, 1024); });
        Require(!one.HasNativeDlssSupport, "A single DLSS-like string must not count as native support.");
        var none = Analyze("none", stream => Zeros(stream, 40 * 1024 * 1024));
        Require(!none.HasNativeDlssSupport && none.NativeDlssEvidence.Length == 0, "A binary with no markers was reported as native DLSS.");
        // A DLSS DLL dropped beside the executable (for example by the manager) is not native support.
        var directory = Path.Combine(root, "dlss", "loose");
        Directory.CreateDirectory(directory);
        File.WriteAllBytes(Path.Combine(directory, "title.exe"), new byte[2048]);
        File.WriteAllText(Path.Combine(directory, "nvngx_dlss.dll"), "installed by the manager");
        Require(!analyzer.Analyze(directory).HasNativeDlssSupport, "A loose nvngx_dlss.dll was mistaken for native support.");
    }

    private static void Api(string root)
    {
        var analyzer = new GameAnalyzer();
        string Make(string name, Action<string> populate)
        {
            var directory = Path.Combine(root, "api", name);
            Directory.CreateDirectory(directory);
            populate(directory);
            return directory;
        }
        var cry = Make("cry", game =>
        {
            Directory.CreateDirectory(Path.Combine(game, "Bin64"));
            File.WriteAllBytes(Path.Combine(game, "Bin64", "title.exe"), new byte[2048]);
            File.WriteAllText(Path.Combine(game, "Game.log"), "Renderer initialization\n  - Feature level: DX11.1 (SM 5.0)\nVulkan Layer noise\n");
        });
        var cryAnalysis = analyzer.Analyze(Path.Combine(cry, "Bin64"));
        Require(cryAnalysis.GraphicsApi == "DX11" && cryAnalysis.ApiSource == "Game.log", $"CryEngine Game.log API not used: {cryAnalysis.GraphicsApi}/{cryAnalysis.ApiSource}.");
        var ogre = Make("ogre", game =>
        {
            File.WriteAllBytes(Path.Combine(game, "title.exe"), new byte[2048]);
            File.WriteAllText(Path.Combine(game, "Plugins_x64.cfg"), "# Plugin=RenderSystem_GL\nPlugin=RenderSystem_Direct3D11_x64\n");
            File.WriteAllText(Path.Combine(game, "opengl32.dll"), "x");
        });
        Require(analyzer.Analyze(ogre).GraphicsApi == "DX11", "Ogre Plugins*.cfg render system was not used.");
        var unity = Make("unity", game =>
        {
            File.WriteAllBytes(Path.Combine(game, "title.exe"), new byte[2048]);
            Directory.CreateDirectory(Path.Combine(game, "title_Data"));
            File.WriteAllText(Path.Combine(game, "title_Data", "globalgamemanagers"), "x");
        });
        Require(analyzer.Analyze(unity).GraphicsApi == "DX11", "A Unity player without a UnityPlayer.dll must default to DX11.");
        var scs = Make("scs", game =>
        {
            File.WriteAllBytes(Path.Combine(game, "title.exe"), new byte[2048]);
            File.WriteAllText(Path.Combine(game, "game.log.txt"), "[gfx] Selected rendering device: dx11\nusing vulkan-1.dll\n");
        });
        Require(analyzer.Analyze(scs).GraphicsApi == "DX11", "SCS game.log.txt rendering device was not used.");
        var firstWins = Make("reshade", game =>
        {
            File.WriteAllBytes(Path.Combine(game, "title.exe"), new byte[2048]);
            File.WriteAllText(Path.Combine(game, "ReShade.log"), "Initialized ... api: d3d11 ...\nlater secondary device api: d3d12\n");
        });
        Require(analyzer.Analyze(firstWins).GraphicsApi == "DX11", "The first ReShade.log device must win over later secondary devices.");

        // Decision rules
        Require(ApiEvidence.Decide(new() { ["Vulkan"] = 3, ["DX11"] = 3 }).Count == 0, "Weak mixed evidence must not decide an API.");
        Require(ApiEvidence.Decide(new() { ["Vulkan"] = 40, ["DX11"] = 2 }).SequenceEqual(["Vulkan"]), "A dominant API must win.");
        Require(ApiEvidence.Decide(new() { ["DX12"] = 20, ["DX11"] = 20 }).Count == 2, "Equal strong evidence must stay ambiguous.");
        var scores = new Dictionary<string, int>();
        ApiEvidence.Score("-windowMode:2 -graphicsDriver:dx12\nVersion:  Direct3D 12.0\nPreferredRHI=DX12", scores);
        Require(scores.Keys.SequenceEqual(["DX12"]), "Explicit engine statements were not scored as DX12.");
    }

    private static void Executables(string root)
    {
        var analyzer = new GameAnalyzer();
        var real = Environment.ProcessPath!;
        var vr = Path.Combine(root, "exe", "Koi Party");
        Directory.CreateDirectory(vr);
        foreach (var name in new[] { "Koi Party.exe", "Koi Party VR.exe", "Studio.exe" }) File.Copy(real, Path.Combine(vr, name));
        Require(Path.GetFileName(analyzer.Analyze(vr).ExecutablePath) == "Koi Party.exe", "The flat-screen game executable must beat its VR variant and tools.");

        var unreal = Path.Combine(root, "exe", "UnrealGame");
        Directory.CreateDirectory(Path.Combine(unreal, "Engine", "Binaries", "Win64"));
        Directory.CreateDirectory(Path.Combine(unreal, "Example", "Binaries", "Win64"));
        File.WriteAllBytes(Path.Combine(unreal, "Engine", "Binaries", "Win64", "BigTool.exe"), new byte[8 * 1024 * 1024]);
        File.Copy(real, Path.Combine(unreal, "Example", "Binaries", "Win64", "Example-Win64-Shipping.exe"));
        File.WriteAllBytes(Path.Combine(unreal, "Example", "Binaries", "Win64", "EpicWebHelper.exe"), new byte[8 * 1024 * 1024]);
        Require(Path.GetFileName(analyzer.Analyze(unreal).ExecutablePath) == "Example-Win64-Shipping.exe",
            "Engine tooling and helper executables must never be chosen as the game executable.");

        var twin = Path.Combine(root, "exe", "Twin");
        foreach (var folder in new[] { "x64", "x64_dx12" }) { Directory.CreateDirectory(Path.Combine(twin, "bin", folder)); File.Copy(real, Path.Combine(twin, "bin", folder, "twin.exe")); }
        File.WriteAllText(Path.Combine(twin, "bin", "x64_dx12", "ReShade.ini"), "[GENERAL]");
        Require(analyzer.Analyze(twin).ExecutablePath!.Contains("x64_dx12"), "The executable folder where ReShade is installed must be preferred.");
    }

    private static void Scanner(string root)
    {
        var library = Path.Combine(root, "scan", "library");
        var common = Path.Combine(library, "steamapps", "common");
        void Game(string folder, string? manifest, bool executable = true)
        {
            Directory.CreateDirectory(Path.Combine(common, folder));
            if (executable) File.WriteAllBytes(Path.Combine(common, folder, "title.exe"), new byte[1024]);
            if (manifest is not null) File.WriteAllText(Path.Combine(library, "steamapps", $"appmanifest_{folder.GetHashCode() & 0xFFFF}.acf"), manifest);
        }
        Game("Installed", "\"AppState\" { \"appid\" \"10\" \"installdir\" \"Installed\" \"StateFlags\" \"4\" }");
        Game("Updating", "\"AppState\" { \"appid\" \"11\" \"installdir\" \"Updating\" \"StateFlags\" \"1030\" }");
        Game("Uninstalled", "\"AppState\" { \"appid\" \"12\" \"installdir\" \"Uninstalled\" \"StateFlags\" \"1\" }");
        Game("Redist", "\"AppState\" { \"appid\" \"228980\" \"installdir\" \"Redist\" \"StateFlags\" \"4\" }");
        Game("Manual", null);
        Game("Leftover", null, executable: false);
        Directory.CreateDirectory(Path.Combine(common, "Leftover", "reshade-shaders"));
        var found = GameScanner.ScanSteamLibrary(library).Select(Path.GetFileName).ToHashSet();
        Require(found.SetEquals(["Installed", "Updating", "Manual"]),
            "Steam scan must keep installed/updating/manual games and drop uninstalled, redistributable and empty folders: " + string.Join(",", found));

        var tree = Path.Combine(root, "scan", "tree");
        void Binary(params string[] parts)
        {
            var directory = Path.Combine([tree, .. parts]);
            Directory.CreateDirectory(directory);
            File.WriteAllBytes(Path.Combine(directory, "title.exe"), new byte[1024]);
            File.WriteAllText(Path.Combine(directory, "dxgi.dll"), "marker");
        }
        Binary("Game", "Game", "Binaries", "Win64");
        Binary("Game", "Engine", "Binaries", "Win64");
        Directory.CreateDirectory(Path.Combine(tree, "Game", "Engine", "Plugins"));
        Binary("Game", "Tools", "Public");
        var scanned = new GameScanner().ScanDirectory(tree).Select(path => path.Replace('/', '\\')).ToArray();
        Require(scanned.Any(path => path.EndsWith("Game\\Game\\Binaries\\Win64", StringComparison.OrdinalIgnoreCase)), "The real game binary folder was not found.");
        Require(scanned.All(path => !path.Contains("\\Engine\\") && !path.Contains("\\Tools\\")),
            "Unreal Engine and Tools folders must not be reported as games: " + string.Join(";", scanned));
    }

    private static void NonGames(string root)
    {
        var analyzer = new GameAnalyzer();
        GameEntry Analyze(string name, bool executable)
        {
            var directory = Path.Combine(root, "nongame", name);
            Directory.CreateDirectory(directory);
            if (executable) File.WriteAllBytes(Path.Combine(directory, "app.exe"), new byte[2048]);
            return analyzer.Analyze(directory);
        }
        foreach (var name in new[] { "Wallpaper Engine", "Lossless Scaling", "Borderless Gaming", "OVR_AdvancedSettings", "XXMI Launcher" })
            Require(Analyze(name, true).IsLikelyNonGame, $"{name} should be classed as a non-game.");
        Require(Analyze("SteamVR", false).IsLikelyNonGame, "A folder without an executable should be classed as a non-game.");
        Require(!Analyze("Cyberpunk 2077", true).IsLikelyNonGame, "A real game was classed as a non-game.");
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}

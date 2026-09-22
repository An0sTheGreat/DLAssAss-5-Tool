using DLSS5ManAger.Core;
using System.Windows.Media;
using System.Windows.Media.Imaging;

PlayPulseTest.Run();

var root = Path.Combine(Path.GetTempPath(), "DLAssAss5Tool-" + Guid.NewGuid().ToString("N"));
var game = Path.Combine(root, "steamapps", "common", "Game");
var installDirectory = Path.Combine(game, "Bin", "Win64MasterMasterSteamPGO");
var executable = Path.Combine(installDirectory, "game.exe");
var payload = Path.Combine(root, "Payload");
var dlss = Path.Combine(root, "DLSS Files");
var backups = Path.Combine(root, "Backups");

try
{
    Directory.CreateDirectory(installDirectory);
    Directory.CreateDirectory(payload);
    Directory.CreateDirectory(dlss);
    File.WriteAllText(Path.Combine(root, "steamapps", "appmanifest_123.acf"),
        "\"AppState\" { \"appid\" \"123\" \"installdir\" \"Game\" }");
    File.WriteAllText(Path.Combine(installDirectory, "ReShade.ini"), "[GENERAL]");
    File.WriteAllText(Path.Combine(installDirectory, "dlss5-feed.cfg"), "managed_non_dlss = 1\n");
    File.Copy(Environment.ProcessPath!, executable);
    File.AppendAllText(executable, "d3d12.dll");
    File.WriteAllText(Path.Combine(installDirectory, "nvngx_dlss.dll"), "original sr runtime");
    File.WriteAllText(Path.Combine(installDirectory, "nvngx_dlssnr.dll"), "original");
    File.WriteAllText(Path.Combine(game, InstallerService.AddonName), "misplaced");
    File.WriteAllText(Path.Combine(game, "nvngx_dlssg.dll"), "misplaced");
    File.WriteAllText(Path.Combine(payload, InstallerService.AddonName), "addon embedded feeder overlay disabled");
    foreach (var name in InstallerService.RequiredDlssFiles)
        File.WriteAllText(Path.Combine(dlss, name), name == "nvngx_dlssnr.dll" ? "replacement" : name);

    var analyzer = new GameAnalyzer();
    var scanner = new GameScanner();
    var service = new InstallerService(backups, payload);
    var analysis = analyzer.Analyze(game);
    Require(analysis.HasReShade, "ReShade detection failed.");
    Require(analysis.HasDlss && analysis.UsesIntegratedFeeder && analysis.RequiresIntegratedFeeder &&
        analysis.SupportsIntegratedFeeder, "A loose DLSS DLL was mistaken for native game support.");
    Require(!analysis.HasIntegratedFeeder && !analysis.HasNativeDlssSupport,
        "Feeder capability or native DLSS support was falsely detected.");
    Require(analysis.Is64Bit, "64-bit executable detection failed.");
    Require(!analysis.HasAddon && !analysis.HasDlssG, "Files away from the selected executable were treated as installed.");
    Require(analysis.IconSource is not null, "Executable icon extraction failed.");
    Require(analysis.GraphicsApi.Contains("DX12"), "Executable API detection failed.");
    Require(analysis.SteamAppId == "123", "Steam AppID detection failed.");
    Require(scanner.ScanDirectory(root).Contains(DirectoryPath.Normalize(game), DirectoryPath.Comparer),
        "Recursive game discovery failed.");
    Require(DirectoryPath.NormalizeDistinct([game, game.ToLowerInvariant().Replace('\\', '/')]).Count == 1,
        "Equivalent Windows paths were not deduplicated.");
    Require(InstallerService.RequiredDlssFiles.Count == 3, "Required DLSS file set changed unexpectedly.");
    Require(CoverArtService.NormalizeTitle("Ghost of Tsushima DIRECTOR'S CUT") ==
        CoverArtService.NormalizeTitle("Ghost of Tsushima: Director’s Cut"), "Cover title normalization failed.");
    var reshade = ReShadeService.ParseLatestAddonInstaller("""
        <a href="/downloads/ReShade_Setup_6.7.0_Addon.exe">old</a>
        <a href="/downloads/ReShade_Setup_6.8.0_Addon.exe">latest</a>
        """);
    Require(reshade.Version == new Version(6, 8, 0) && reshade.DownloadUrl.Host == "reshade.me",
        "Official ReShade add-on release parsing failed.");
    Require(ReShadeService.ResolveInstallerApi("DX12") == "dxgi", "DX12 must use the DXGI ReShade proxy.");
    Require(ReShadeService.ResolveInstallerApi("DX11") == "dxgi", "DX11 must use the DXGI ReShade proxy.");
    Require(ReShadeService.ResolveInstallerApi("DX9") == "d3d9", "DX9 ReShade mapping failed.");
    Require(ReShadeService.InstallerArguments(executable, "dxgi", false, ReShadeInstallMode.ReShadeOnly)
            .SequenceEqual([executable, "--headless", "--api", "dxgi"]),
        "ReShade-only install arguments changed.");
    Require(ReShadeService.InstallerArguments(executable, "dxgi", true, ReShadeInstallMode.ReShadeOnly)
            .SequenceEqual([executable, "--headless", "--api", "dxgi", "--state", "update"]),
        "ReShade-only reinstall must retain headless update mode.");
    Require(ReShadeService.InstallerArguments(executable, "dxgi", false, ReShadeInstallMode.InteractivePackages)
            .SequenceEqual([executable, "--api", "dxgi"]),
        "Interactive install must show ReShade Setup.");
    Require(ReShadeService.InstallerArguments(executable, "dxgi", true, ReShadeInstallMode.InteractivePackages)
            .SequenceEqual([executable, "--api", "dxgi", "--state", "modify"]),
        "Interactive reinstall must open package modification.");
    foreach (var api in new[] { "DX9", "DX11", "DX12", "OpenGL", "Vulkan", "DX12 / DX11" })
    {
        analysis.GraphicsApi = api;
        analysis.HasReShade = analysis.HasAddon = true;
        Require(analysis.CanInstallReShade && analysis.ReShadeActionLabel == "REINSTALL RESHADE" &&
            analysis.AddonActionLabel == "REINSTALL", "Installed games must allow reinstallation.");
    }
    Require(ReShadeService.ResolveInstallerApi("DX12 / DX11") is null &&
        ReShadeService.SupportedGraphicsApis("DX12 / DX11").SequenceEqual(["DX12", "DX11"]),
        "Multi-API games must require an explicit supported API selection.");
    var reshadeIni = NonDlssSetupService.BuildReShadeIni(
        "[GENERAL]\nPreprocessorDefinitions=KEEP=1,DLSS5_MV_PROVIDER=1\nEffectSearchPaths=.\\existing\n");
    Require(reshadeIni.Contains("KEEP=1,DLSS5_MV_PROVIDER=3") &&
        reshadeIni.Contains(@".\reshade-shaders\Shaders") &&
        !reshadeIni.Contains("DLSS5_MV_PROVIDER=1"), "Global feed preprocessor configuration failed.");
    var preset = NonDlssSetupService.BuildPreset(
        "Techniques=Existing@Other.fx,DLSS5_Feed@DLSS5_Feed.fx\nTechniqueSorting=Sorted@Other.fx\n");
    Require(preset.IndexOf("Lumenite_Kernel@lumenite_Kernel.fx", StringComparison.Ordinal) <
        preset.IndexOf("DLSS5_Feed@DLSS5_Feed.fx", StringComparison.Ordinal) &&
        preset.Contains("TechniqueSorting=Sorted@Other.fx,Lumenite_Kernel@lumenite_Kernel.fx,DLSS5_Feed@DLSS5_Feed.fx") &&
        preset.Contains("[DLSS5_Feed.fx]") && preset.Contains("PreprocessorDefinitions=DLSS5_MV_PROVIDER=3"),
        "Required Lumenite/feed order or provider configuration failed.");
    Require(NonDlssSetupService.BuildFeedConfig("enabled=0\n").Contains("managed_non_dlss=1") &&
        NonDlssSetupService.ResolvePresetTarget(installDirectory, @"..\escape.ini") == "ReShadePreset.ini",
        "Managed feeder marker or preset path containment failed.");
    var store = new AppStore(Path.Combine(root, "Store"));
    var customArt = Path.Combine(root, "custom-art.png");
    var encoder = new PngBitmapEncoder();
    encoder.Frames.Add(BitmapFrame.Create(BitmapSource.Create(1, 1, 96, 96, PixelFormats.Bgra32,
        null, new byte[] { 0x00, 0xB9, 0x76, 0xFF }, 4)));
    using (var stream = File.Create(customArt)) encoder.Save(stream);
    var coverArt = new CoverArtService(Path.Combine(store.DataDirectory, "Covers"));
    var importedArt = coverArt.ImportCustomAsync(game, customArt).GetAwaiter().GetResult();
    Require(File.Exists(importedArt.Path) && importedArt.Image is not null,
        "Custom artwork was not validated and copied into managed storage.");
    Require(coverArt.LoadAsync(analysis, importedArt.Path).GetAwaiter().GetResult() is not null,
        "Persisted custom artwork was not loaded before remote artwork lookup.");
    store.Save(new ManagerSettings
    {
        GameDirectories = [game, game.ToLowerInvariant().Replace('\\', '/')],
        HiddenGameDirectories = [game, game.ToLowerInvariant().Replace('\\', '/')],
        CustomGameNames = new(DirectoryPath.Comparer) { [game.ToLowerInvariant().Replace('\\', '/')] = "Renamed Game" },
        CustomArtworkPaths = new(DirectoryPath.Comparer) { [game.ToLowerInvariant().Replace('\\', '/')] = importedArt.Path },
        IsLibraryView = false,
        WindowLeft = 140,
        WindowTop = 90,
        WindowWidth = 1080,
        WindowHeight = 720,
        WindowMaximized = true
    });
    var loaded = store.Load();
    Require(loaded.GameDirectories.Count == 1, "Persisted game paths were not deduplicated.");
    Require(loaded.HiddenGameDirectories.Count == 1, "Hidden game paths were not deduplicated.");
    Require(loaded.CustomGameNames.TryGetValue(game, out var customName) && customName == "Renamed Game",
        "Custom game name was not normalized and persisted.");
    Require(loaded.CustomArtworkPaths.TryGetValue(game, out var artworkPath) && artworkPath == importedArt.Path,
        "Custom artwork path was not normalized and persisted.");
    Require(!loaded.IsLibraryView, "View preference was not persisted.");
    Require(loaded.WindowLeft == 140 && loaded.WindowTop == 90 && loaded.WindowWidth == 1080 &&
        loaded.WindowHeight == 720 && loaded.WindowMaximized, "Window placement was not persisted.");
    var installedBridge = Path.Combine(installDirectory, "dlss5-bridge.addon64");
    File.WriteAllText(installedBridge, "old bridge");
    var stagedShader = Path.Combine(root, "staged.fx");
    File.WriteAllText(stagedShader, "shader");
    Require(!service.ConfigureNonDlss(executable, [new(stagedShader, @"..\escape.fx")]).Success,
        "Installer accepted a target outside the game directory.");
    var nestedShader = Path.Combine("reshade-shaders", "Shaders", "nested.fx");
    Require(service.ConfigureNonDlss(executable, [new(stagedShader, nestedShader)]).Success &&
        File.Exists(Path.Combine(installDirectory, nestedShader)), "Nested transactional installation failed.");
    Require(service.RestoreLatest(executable).Success && !File.Exists(Path.Combine(installDirectory, nestedShader)),
        "Nested transactional restore failed.");
    Require(service.Install(executable, dlss, true).Success, "Install failed.");
    Require(File.ReadAllText(Path.Combine(installDirectory, "nvngx_dlssnr.dll")) == "replacement", "DLSS replacement failed.");
    Require(File.Exists(Path.Combine(installDirectory, InstallerService.AddonName)), "Add-on was not installed beside the executable.");
    File.WriteAllText(Path.Combine(installDirectory, "dlss5-feed.cfg"), "managed_non_dlss=0\n");
    var legacyNonDlss = analyzer.Analyze(game);
    Require(legacyNonDlss.HasIntegratedFeeder && !legacyNonDlss.HasNativeDlssSupport &&
        legacyNonDlss.RequiresIntegratedFeeder && !legacyNonDlss.UsesIntegratedFeeder &&
        legacyNonDlss.DlssLabel == "Feed Repair Needed" && legacyNonDlss.DetailStatuses[^1].Exists,
        "Embedded feeder capability or non-DLSS setup requirement was not detected.");
    var nativeGame = Path.Combine(root, "NativeDlss");
    Directory.CreateDirectory(nativeGame);
    var nativeExecutable = Path.Combine(nativeGame, "native.exe");
    File.Copy(Environment.ProcessPath!, nativeExecutable);
    File.AppendAllText(nativeExecutable, "d3d12.dll NVSDK_NGX_D3D12_CreateFeature");
    File.WriteAllText(Path.Combine(nativeGame, "nvngx_dlss.dll"), "native runtime");
    var nativeAnalysis = analyzer.Analyze(nativeGame);
    Require(nativeAnalysis.HasNativeDlssSupport && !nativeAnalysis.RequiresIntegratedFeeder,
        "Native DLSS code evidence did not suppress automatic feeder setup.");
    Require(File.ReadAllText(installedBridge) == "old bridge", "Install modified an existing external DLSS 5 Bridge.");
    Require(File.ReadAllText(Path.Combine(game, InstallerService.AddonName)) == "misplaced", "Parent-folder files were modified.");
    Require(service.RestoreLatest(executable).Success, "Restore failed.");
    Require(File.ReadAllText(Path.Combine(installDirectory, "nvngx_dlssnr.dll")) == "original", "Original DLSS file was not restored.");
    Require(!File.Exists(Path.Combine(installDirectory, InstallerService.AddonName)), "New add-on was not removed by restore.");
    Require(File.ReadAllText(installedBridge) == "old bridge", "Restore modified an existing external DLSS 5 Bridge.");
    Require(service.Install(executable, dlss, true).Success, "Second installation failed.");
    File.WriteAllText(Path.Combine(payload, InstallerService.AddonName), "updated addon");
    Require(service.Install(executable, dlss, true).Success, "Reinstallation failed.");
    Require(File.ReadAllText(Path.Combine(installDirectory, InstallerService.AddonName)) == "updated addon", "Reinstall did not replace the binary.");
    Require(File.ReadAllText(Path.Combine(installDirectory, "ReShade.ini")) == "[GENERAL]", "Reinstall changed settings.");
    Require(service.RestoreLatest(executable).Success && File.ReadAllText(Path.Combine(installDirectory, InstallerService.AddonName)) == "addon embedded feeder overlay disabled",
        "Reinstall backup did not restore the previous add-on.");
    File.WriteAllText(Path.Combine(payload, InstallerService.AddonName), "bulk updated addon");
    File.WriteAllText(Path.Combine(installDirectory, "nvngx_dlssnr.dll"), "user changed");
    Require(service.UpdateAddon(executable).Success, "Add-on-only update failed.");
    Require(File.ReadAllText(Path.Combine(installDirectory, InstallerService.AddonName)) == "bulk updated addon",
        "Add-on-only update did not replace the installed add-on.");
    Require(File.ReadAllText(Path.Combine(installDirectory, "nvngx_dlssnr.dll")) == "user changed" &&
        File.ReadAllText(Path.Combine(installDirectory, "ReShade.ini")) == "[GENERAL]",
        "Add-on-only update changed DLSS or ReShade settings.");
    Require(service.RestoreLatest(executable).Success &&
        File.ReadAllText(Path.Combine(installDirectory, InstallerService.AddonName)) == "addon embedded feeder overlay disabled",
        "Add-on-only update backup did not restore the previous add-on.");
    File.Delete(Path.Combine(installDirectory, InstallerService.AddonName));
    var backupsBeforeSkippedUpdate = Directory.EnumerateDirectories(backups, "*", SearchOption.AllDirectories).Count();
    Require(!service.UpdateAddon(executable).Success &&
        !File.Exists(Path.Combine(installDirectory, InstallerService.AddonName)),
        "Add-on-only update touched a game without the add-on installed.");
    Require(Directory.EnumerateDirectories(backups, "*", SearchOption.AllDirectories).Count() == backupsBeforeSkippedUpdate,
        "Skipped add-on update created a backup or changed state.");
    File.Delete(Path.Combine(installDirectory, "ReShade.ini"));
    Require(!service.Install(executable, dlss, true).Success, "Install proceeded without ReShade beside the executable.");
    var mainWindow = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "MainWindow.xaml"));
    Require(mainWindow.Contains("Content=\"UPDATE ALL\"") &&
        mainWindow.IndexOf("game(s) detected", StringComparison.Ordinal) <
        mainWindow.IndexOf("Content=\"UPDATE ALL\"", StringComparison.Ordinal),
        "Update All is not positioned after the detected-game count.");
    Require(mainWindow.Contains("HasMultipleAddonInstallations") && mainWindow.Contains("ChangeArtwork_Click") &&
        mainWindow.Contains("IsLoadingOverlayVisible") && mainWindow.Contains("LoadingOverlayText") &&
        mainWindow.Contains("CornerRadius=\"10\"") &&
        mainWindow.Contains("PlayReadyPulse"), "New library UI states are missing from the window markup.");
    var dialog = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "ThemedDialog.xaml"));
    Require(dialog.Contains("Header=\"View details\"") && dialog.Contains("DetailsPanel") &&
        dialog.Contains("To view installed directories"),
        "Update All detail expander is missing from the themed dialog.");
    var mainWindowCode = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "MainWindow.xaml.cs"));
    Require(mainWindowCode.Contains("Analyzing Games...") &&
        mainWindowCode.Contains("Updating Selected Games...") &&
        mainWindowCode.Contains("Installed 1 file(s)") &&
        !mainWindowCode.Contains("Install ReShade Only") &&
        mainWindowCode.Contains("ReShadeInstallMode.InteractivePackages") &&
        !mainWindowCode.Contains("DLSS 5 Bridge"),
        "Operation overlay text or concise update result text is missing.");
    var dialogCode = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "ThemedDialog.xaml.cs"));
    Require(dialogCode.Contains("PrimaryButton.Content = \"YES\"") &&
        dialogCode.Contains("CancelButton.Content = \"NO\""), "Confirmation buttons must say Yes and No.");
    Console.WriteLine("PASS: paths, discovery, API/AppID analysis, covers, ReShade modes, preferences, install, external-bridge preservation, addon-only update/refusal, backup, and restore");
}
finally
{
    if (Directory.Exists(root)) Directory.Delete(root, true);
}

static void Require(bool condition, string message)
{
    if (!condition) throw new InvalidOperationException(message);
}

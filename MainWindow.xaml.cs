using DLSS5ManAger.Core;
using Microsoft.Win32;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;

namespace DLSS5ManAger;

public partial class MainWindow : Window, INotifyPropertyChanged
{
    private readonly AppStore _store = new();
    private readonly GameScanner _scanner = new();
    private readonly GameAnalyzer _analyzer = new();
    private readonly InstallerService _installer;
    private readonly CoverArtService _coverArt;
    private readonly ReShadeService _reshade = new();
    private ManagerSettings _settings;
    private GameEntry? _selectedGame;
    private string _activity = "Ready";
    private string _logText = "";
    private string _dlssFilesDirectory;
    private string _dlssStatusTitle = "Checking user-supplied DLLs…";
    private string _dlssStatusColor = "#9BA4B3";
    private bool _dlssFilesConfirmed;
    private bool _isLibraryView;
    private bool _showHiddenGames;
    private bool _hasHiddenGames;
    private bool _isLoadingOverlayVisible = true;
    private string _loadingOverlayText = "Analyzing Games...";
    private string? _sortProperty;
    private ListSortDirection _sortDirection = ListSortDirection.Ascending;

    public ObservableCollection<GameEntry> Games { get; } = [];
    public ObservableCollection<DlssFileStatus> DlssFileStatuses { get; } = [];
    public GameEntry? SelectedGame { get => _selectedGame; set => Set(ref _selectedGame, value); }
    public string Activity { get => _activity; set => Set(ref _activity, value); }
    public string LogText { get => _logText; set => Set(ref _logText, value); }
    public string DlssFilesDirectory { get => _dlssFilesDirectory; set => Set(ref _dlssFilesDirectory, value); }
    public string DlssStatusTitle { get => _dlssStatusTitle; set => Set(ref _dlssStatusTitle, value); }
    public string DlssStatusColor { get => _dlssStatusColor; set => Set(ref _dlssStatusColor, value); }
    public bool DlssFilesConfirmed { get => _dlssFilesConfirmed; set => Set(ref _dlssFilesConfirmed, value); }
    public bool IsLibraryView { get => _isLibraryView; set => Set(ref _isLibraryView, value); }
    public bool ShowHiddenGames { get => _showHiddenGames; set => Set(ref _showHiddenGames, value); }
    public bool HasHiddenGames { get => _hasHiddenGames; set => Set(ref _hasHiddenGames, value); }
    public bool IsLoadingOverlayVisible { get => _isLoadingOverlayVisible; set => Set(ref _isLoadingOverlayVisible, value); }
    public string LoadingOverlayText { get => _loadingOverlayText; set => Set(ref _loadingOverlayText, value); }
    public bool HasMultipleAddonInstallations => GetUpdateAllTargets().Length > 1;

    public event PropertyChangedEventHandler? PropertyChanged;

    public MainWindow()
    {
        _settings = _store.Load();
        _isLibraryView = _settings.IsLibraryView;
        InitializeComponent();
        RestoreWindowPlacement();
        _dlssFilesDirectory = _settings.DlssFilesDirectory
            ?? Path.Combine(AppContext.BaseDirectory, "DLSS Files");
        Directory.CreateDirectory(_dlssFilesDirectory);
        _store.Save(_settings);
        _installer = new InstallerService(_store.BackupDirectory);
        _coverArt = new CoverArtService(Path.Combine(_store.DataDirectory, "Covers"));
        DataContext = this;
        LibraryViewButton.IsChecked = IsLibraryView;
        FolderViewButton.IsChecked = !IsLibraryView;
        LoadLog();
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        ShowLoadingOverlay("Analyzing Games...");
        try
        {
            RefreshDlssStatus();
            await RefreshKnownGames(analysisCompleted: () => IsLoadingOverlayVisible = false);
        }
        finally { IsLoadingOverlayVisible = false; }
    }

    private void RestoreWindowPlacement()
    {
        if (_settings.WindowLeft is not { } left || _settings.WindowTop is not { } top ||
            _settings.WindowWidth is not { } width || _settings.WindowHeight is not { } height ||
            !double.IsFinite(left) || !double.IsFinite(top) ||
            !double.IsFinite(width) || !double.IsFinite(height)) return;

        width = Math.Clamp(width, MinWidth, Math.Max(MinWidth, SystemParameters.VirtualScreenWidth));
        height = Math.Clamp(height, MinHeight, Math.Max(MinHeight, SystemParameters.VirtualScreenHeight));
        left = Math.Clamp(left, SystemParameters.VirtualScreenLeft,
            SystemParameters.VirtualScreenLeft + Math.Max(0, SystemParameters.VirtualScreenWidth - width));
        top = Math.Clamp(top, SystemParameters.VirtualScreenTop,
            SystemParameters.VirtualScreenTop + Math.Max(0, SystemParameters.VirtualScreenHeight - height));
        WindowStartupLocation = WindowStartupLocation.Manual;
        Left = left;
        Top = top;
        Width = width;
        Height = height;
        if (_settings.WindowMaximized) WindowState = WindowState.Maximized;
    }

    private void Window_Closing(object? sender, CancelEventArgs e)
    {
        var bounds = WindowState == WindowState.Normal
            ? new Rect(Left, Top, ActualWidth, ActualHeight)
            : RestoreBounds;
        if (double.IsFinite(bounds.Left) && double.IsFinite(bounds.Top) &&
            double.IsFinite(bounds.Width) && double.IsFinite(bounds.Height) &&
            bounds.Width >= MinWidth && bounds.Height >= MinHeight)
        {
            _settings.WindowLeft = bounds.Left;
            _settings.WindowTop = bounds.Top;
            _settings.WindowWidth = bounds.Width;
            _settings.WindowHeight = bounds.Height;
        }
        _settings.WindowMaximized = WindowState == WindowState.Maximized;
        _store.Save(_settings);
    }

    private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ChangedButton != MouseButton.Left) return;
        if (e.ClickCount == 2) ToggleMaximize();
        else DragMove();
    }

    private void Minimize_Click(object sender, RoutedEventArgs e) => WindowState = WindowState.Minimized;

    private void Maximize_Click(object sender, RoutedEventArgs e) => ToggleMaximize();

    private void Close_Click(object sender, RoutedEventArgs e) => Close();

    private void ToggleMaximize() => WindowState = WindowState == WindowState.Maximized
        ? WindowState.Normal
        : WindowState.Maximized;

    private void Window_StateChanged(object? sender, EventArgs e)
    {
        var maximized = WindowState == WindowState.Maximized;
        MaximizeButton.Content = maximized ? "❐" : "□";
        MaximizeButton.ToolTip = maximized ? "Restore" : "Maximize";
        WindowBorder.BorderThickness = maximized ? new Thickness(0) : new Thickness(1);
    }

    private void FolderView_Click(object sender, RoutedEventArgs e) => SetLibraryView(false);

    private void LibraryView_Click(object sender, RoutedEventArgs e) => SetLibraryView(true);

    private void SetLibraryView(bool libraryView)
    {
        IsLibraryView = libraryView;
        LibraryViewButton.IsChecked = libraryView;
        FolderViewButton.IsChecked = !libraryView;
        if (_settings.IsLibraryView == libraryView) return;
        _settings.IsLibraryView = libraryView;
        _store.Save(_settings);
    }

    private async void ScanSteam_Click(object sender, RoutedEventArgs e)
    {
        await RunBusy("Scanning Steam libraries…", () =>
        {
            var discovered = _scanner.ScanSteamLibraries();
            AddDiscovered(discovered);
            _store.Save(_settings);
            return discovered.Count;
        }, count => $"Steam scan found {count} game folder(s).");
        await RefreshKnownGames();
    }

    private async void AddGame_Click(object sender, RoutedEventArgs e)
    {
        var picker = new OpenFolderDialog { Title = "Select a drive or directory to search for games", Multiselect = false };
        if (picker.ShowDialog(this) != true) return;
        var root = DirectoryPath.Normalize(picker.FolderName);
        await RunBusy($"Searching {root}…", () =>
        {
            var discovered = _scanner.ScanDirectory(root);
            if (!_settings.SearchDirectories.Contains(root, DirectoryPath.Comparer)) _settings.SearchDirectories.Add(root);
            AddDiscovered(discovered);
            _store.Save(_settings);
            return discovered.Count;
        }, count => $"Search found {count} probable game folder(s) under {root}.");
        await RefreshKnownGames();
    }

    private async void Refresh_Click(object sender, RoutedEventArgs e)
    {
        ShowLoadingOverlay("Analyzing Games...");
        try
        {
            RefreshDlssStatus();
            await RefreshKnownGames(SelectedGame?.GameDirectory,
                () => IsLoadingOverlayVisible = false);
        }
        finally { IsLoadingOverlayVisible = false; }
    }

    private async Task RefreshKnownGames(string? selectDirectory = null, Action? analysisCompleted = null)
    {
        var directories = DirectoryPath.NormalizeDistinct(_settings.GameDirectories.Where(Directory.Exists));
        _settings.GameDirectories = directories;
        _store.Save(_settings);
        Activity = "Analyzing game folders…";
        var entries = await Task.Run(() => directories.Select(_analyzer.Analyze).ToArray());
        var hidden = new HashSet<string>(_settings.HiddenGameDirectories, DirectoryPath.Comparer);
        foreach (var entry in entries)
        {
            ApplySavedMetadata(entry, hidden);
            entry.IsCoverLoading = true;
        }
        Games.Clear();
        foreach (var entry in entries.OrderBy(game => game.Name, StringComparer.OrdinalIgnoreCase)) Games.Add(entry);
        Notify(nameof(HasMultipleAddonInstallations));
        ApplyGameFilter();
        var view = CollectionViewSource.GetDefaultView(Games);
        var requested = Games.FirstOrDefault(game => string.Equals(game.GameDirectory,
            selectDirectory, StringComparison.OrdinalIgnoreCase));
        SelectedGame = requested is not null && view.Contains(requested) ? requested : FirstVisibleGame();
        Activity = "Loading cover art…";
        var coverLoading = Task.WhenAll(entries.Select(LoadCoverAsync));
        analysisCompleted?.Invoke();
        await coverLoading;
        Activity = "Ready";
    }

    private async Task LoadCoverAsync(GameEntry game)
    {
        _settings.CustomArtworkPaths.TryGetValue(game.GameDirectory, out var customPath);
        try { game.CoverSource = await _coverArt.LoadAsync(game, customPath); }
        finally { game.IsCoverLoading = false; }
    }

    private void ApplySavedMetadata(GameEntry game, HashSet<string>? hidden = null)
    {
        game.IsHidden = (hidden ?? new(_settings.HiddenGameDirectories, DirectoryPath.Comparer)).Contains(game.GameDirectory);
        if (_settings.CustomGameNames.TryGetValue(game.GameDirectory, out var name)) game.Name = name;
    }

    private async Task RefreshGame(GameEntry game)
    {
        Activity = $"Analyzing {game.Name}…";
        var refreshed = await Task.Run(() => _analyzer.Analyze(game.GameDirectory));
        ApplySavedMetadata(refreshed);
        refreshed.CoverSource = game.CoverSource;
        var index = Games.IndexOf(game);
        if (index >= 0) Games[index] = refreshed;
        Notify(nameof(HasMultipleAddonInstallations));
        ApplyGameFilter();
        SelectedGame = refreshed;
        Activity = "Ready";
    }

    private void ShowHidden_Click(object sender, RoutedEventArgs e)
    {
        ShowHiddenGames = ShowHiddenButton.IsChecked == true;
        ApplyGameFilter();
        SelectedGame = FirstVisibleGame();
    }

    private void ToggleGameHidden_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not MenuItem { CommandParameter: GameEntry game }) return;
        game.IsHidden = !game.IsHidden;
        _settings.HiddenGameDirectories.RemoveAll(path => DirectoryPath.Comparer.Equals(path, game.GameDirectory));
        if (game.IsHidden) _settings.HiddenGameDirectories.Add(game.GameDirectory);
        _store.Save(_settings);
        AppendLog($"{(game.IsHidden ? "Hidden" : "Unhidden")} game: {game.Name}");
        ApplyGameFilter();
        SelectedGame = FirstVisibleGame();
    }

    private void RenameGame_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not MenuItem { CommandParameter: GameEntry game }) return;
        var name = ThemedDialog.Prompt(this, "Rename game", "Enter the library name for this game:", game.Name)?.Trim();
        if (string.IsNullOrEmpty(name) || name.Equals(game.Name, StringComparison.Ordinal)) return;
        if (name.Length > 120 || name.Any(char.IsControl)) { Show("Game names must be 1–120 printable characters.", MessageBoxImage.Warning); return; }
        _settings.CustomGameNames[game.GameDirectory] = name;
        game.Name = name;
        _store.Save(_settings);
        CollectionViewSource.GetDefaultView(Games).Refresh();
        AppendLog($"Renamed game to: {name}");
    }

    private async void ChangeArtwork_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not MenuItem { CommandParameter: GameEntry game }) return;
        var picker = new OpenFileDialog
        {
            Title = $"Choose artwork for {game.Name}",
            Filter = "Image files|*.jpg;*.jpeg;*.png;*.bmp;*.gif;*.tif;*.tiff|All files|*.*",
            CheckFileExists = true,
            Multiselect = false
        };
        if (picker.ShowDialog(this) != true) return;

        game.IsCoverLoading = true;
        try
        {
            var artwork = await _coverArt.ImportCustomAsync(game.GameDirectory, picker.FileName);
            _settings.CustomArtworkPaths[game.GameDirectory] = artwork.Path;
            _store.Save(_settings);
            game.CoverSource = artwork.Image;
            AppendLog($"Changed artwork for {game.Name}.");
        }
        catch (Exception exception)
        {
            Show($"The selected artwork could not be used.\n\n{exception.Message}", MessageBoxImage.Error);
        }
        finally { game.IsCoverLoading = false; }
    }

    private void ApplyGameFilter()
    {
        HasHiddenGames = Games.Any(game => game.IsHidden);
        if (!HasHiddenGames)
        {
            ShowHiddenGames = false;
            ShowHiddenButton.IsChecked = false;
        }
        var view = CollectionViewSource.GetDefaultView(Games);
        view.Filter = item => item is GameEntry game && game.IsHidden == ShowHiddenGames;
        view.Refresh();
    }

    private GameEntry? FirstVisibleGame() => CollectionViewSource.GetDefaultView(Games)
        .Cast<object>().OfType<GameEntry>().FirstOrDefault();

    private async void Install_Click(object sender, RoutedEventArgs e)
    {
        var game = SelectedGame;
        if (game is null) { Show("Select a game first."); return; }
        var targetDirectory = InstallerService.InstallDirectory(game.ExecutablePath);
        if (targetDirectory is null) { Show("The selected game does not have a usable executable."); return; }
        if (!ThemedDialog.Confirm(this, game.HasAddon ? "Confirm reinstallation" : "Confirm installation",
            $"{(game.HasAddon ? "Reinstall" : "Install")} the add-on and available validated user-supplied DLSS files beside the selected executable:\n\n{targetDirectory}\n\nExisting files are backed up; your settings are kept.")) return;
        await RunOperation("Installing…", () => _installer.Install(game.ExecutablePath!,
            DlssFilesDirectory, true));
    }

    private async void UpdateAll_Click(object sender, RoutedEventArgs e)
    {
        var targets = GetUpdateAllTargets();
        if (targets.Length == 0)
        {
            Show("No detected games currently have the add-on installed.");
            return;
        }
        if (!ThemedDialog.Confirm(this, "Update all installed add-ons",
            $"Update the add-on in {targets.Length} detected game installation(s)?\n\nOnly {InstallerService.AddonName} will be replaced. ReShade, DLSS files, settings and games without the add-on will not be changed. Existing add-ons are backed up.")) return;

        var selectedDirectory = SelectedGame?.GameDirectory;
        var updated = new List<string>();
        var skipped = new List<string>();
        var failures = new List<string>();
        UpdateAllButton.IsEnabled = false;
        ShowLoadingOverlay("Updating Selected Games...");
        try
        {
            for (var index = 0; index < targets.Length; ++index)
            {
                var game = targets[index];
                var directory = InstallerService.InstallDirectory(game.ExecutablePath);
                if (directory is null || !File.Exists(Path.Combine(directory, InstallerService.AddonName)))
                {
                    var reason = $"{game.Name}: add-on is no longer installed.";
                    skipped.Add(reason);
                    AppendLog($"Update All skipped {reason}");
                    continue;
                }

                Activity = $"Updating add-on {index + 1}/{targets.Length}: {game.Name}…";
                InstallResult result;
                try { result = await Task.Run(() => _installer.UpdateAddon(game.ExecutablePath!)); }
                catch (Exception exception)
                {
                    failures.Add($"{game.Name}: {exception.Message}");
                    AppendLog($"Update All failed {game.Name}: {exception.Message}");
                    continue;
                }
                AppendLog($"Update All {game.Name}: {result.Message}");
                if (result.Success) updated.Add($"{game.Name}: Installed 1 file(s)");
                else failures.Add($"{game.Name}: {result.Message}");
            }

            var summary = $"Update All complete. Updated: {updated.Count}. Skipped: {skipped.Count}. Failed: {failures.Count}.";
            AppendLog(summary);
            await RefreshKnownGames(selectedDirectory, () =>
            {
                IsLoadingOverlayVisible = false;
                ThemedDialog.ShowDetails(this, "DLAssAss 5 Tool", summary, updated, skipped, failures,
                    failures.Count == 0 ? MessageBoxImage.Information : MessageBoxImage.Warning);
            });
        }
        finally
        {
            IsLoadingOverlayVisible = false;
            UpdateAllButton.IsEnabled = true;
            if (Activity != "Ready") Activity = "Ready";
        }
    }

    private GameEntry[] GetUpdateAllTargets() => Games
            .Where(game => game.HasAddon && game.ExecutablePath is not null)
            .Select(game => (Game: game, Directory: InstallerService.InstallDirectory(game.ExecutablePath)))
            .Where(target => target.Directory is not null)
            .GroupBy(target => target.Directory!, DirectoryPath.Comparer)
            .Select(group => group.First().Game)
            .ToArray();

    private async void InstallReShade_Click(object sender, RoutedEventArgs e)
    {
        var game = SelectedGame;
        if (game?.ExecutablePath is null) { Show("The selected game does not have a usable executable."); return; }
        if (!game.CanInstallReShade) return;
        const string reshadeOnly = "Install ReShade Only";
        const string reshadeWithShaders = "Install ReShade + Shaders";
        var choice = ThemedDialog.Choose(this, "Choose ReShade installation",
            "Choose how ReShade should be installed. ReShade Only uses the current automatic setup. ReShade + Shaders opens the official ReShade Setup so you can select the shader and add-on packages you want.",
            [reshadeOnly, reshadeWithShaders]);
        if (choice is null) return;
        var installMode = choice == reshadeOnly
            ? ReShadeInstallMode.ReShadeOnly
            : ReShadeInstallMode.InteractivePackages;
        var graphicsApi = SelectReShadeApi(game);
        if (graphicsApi is null) return;
        if (!ThemedDialog.Confirm(this, game.HasReShade ? "Reinstall ReShade" : "Install ReShade",
            $"Download and {(game.HasReShade ? "reinstall" : "install")} the latest official ReShade build with full add-on support for {graphicsApi} into:\n\n{game.ExecutablePath}\n\n{(installMode == ReShadeInstallMode.ReShadeOnly ? "No shaders will be downloaded." : "Official ReShade Setup will open so you can select shader and add-on packages before installation completes.")} Existing settings and presets are kept. The full add-on build is intended for single-player use.",
            MessageBoxImage.Warning)) return;

        Activity = "Checking the latest official ReShade release…";
        var result = await _reshade.InstallLatestAsync(game, graphicsApi, installMode);
        AppendLog(result.Message);
        Show(result.Message, result.Success ? MessageBoxImage.Information : MessageBoxImage.Error);
        await RefreshGame(game);
    }

    private string? SelectReShadeApi(GameEntry game)
    {
        var apis = ReShadeService.SupportedGraphicsApis(game.GraphicsApi);
        if (apis.Count == 0) { Show("No supported graphics API was detected for this game.", MessageBoxImage.Warning); return null; }
        if (apis.Count == 1) return apis[0];
        return ThemedDialog.Choose(this, "Choose ReShade graphics API",
            $"Detected graphics APIs: {string.Join(", ", apis)}\n\nSelect the API you will use:", apis);
    }

    private async void Restore_Click(object sender, RoutedEventArgs e)
    {
        var game = SelectedGame;
        if (game is null) { Show("Select a game first."); return; }
        if (!ThemedDialog.Confirm(this, "Confirm restore",
            "Restore the files from this game's latest manager backup?")) return;
        await RunOperation("Restoring…", () => _installer.RestoreLatest(game.ExecutablePath ?? ""));
    }

    private async Task RunOperation(string activity, Func<InstallResult> operation)
    {
        var game = SelectedGame;
        if (game is null) return;
        Activity = activity;
        var result = await Task.Run(operation);
        AppendLog(result.Message);
        Show(result.Message, result.Success ? MessageBoxImage.Information : MessageBoxImage.Error);
        await RefreshGame(game);
    }

    private async Task RunBusy<T>(string activity, Func<T> work, Func<T, string> completed)
    {
        Activity = activity;
        var result = await Task.Run(work);
        AppendLog(completed(result));
    }

    private void BrowseDlss_Click(object sender, RoutedEventArgs e)
    {
        var picker = new OpenFolderDialog { Title = "Select your DLSS Files folder", Multiselect = false,
            InitialDirectory = Directory.Exists(DlssFilesDirectory) ? DlssFilesDirectory : null };
        if (picker.ShowDialog(this) != true) return;
        DlssFilesDirectory = picker.FolderName;
        _settings.DlssFilesDirectory = picker.FolderName;
        _store.Save(_settings);
        RefreshDlssStatus();
        AppendLog($"DLSS Files folder changed to {picker.FolderName}");
    }

    private void OpenDlssFolder_Click(object sender, RoutedEventArgs e)
    {
        Directory.CreateDirectory(DlssFilesDirectory);
        OpenFolder(DlssFilesDirectory);
    }

    private void OpenGameFolder_Click(object sender, RoutedEventArgs e)
    {
        var game = (sender as MenuItem)?.CommandParameter as GameEntry ?? SelectedGame;
        if (game is not null) OpenFolder(game.GameDirectory);
    }

    private void Play_Click(object sender, RoutedEventArgs e)
    {
        var game = SelectedGame;
        if (game?.ExecutablePath is null || !File.Exists(game.ExecutablePath))
        {
            Show("The selected game does not have a usable executable.", MessageBoxImage.Warning);
            return;
        }
        Process.Start(new ProcessStartInfo(game.ExecutablePath)
        {
            UseShellExecute = true,
            WorkingDirectory = Path.GetDirectoryName(game.ExecutablePath) ?? game.GameDirectory
        });
    }

    private static void OpenFolder(string path) => Process.Start(new ProcessStartInfo(path) { UseShellExecute = true });

    private void AddDiscovered(IEnumerable<string> directories)
    {
        var known = new HashSet<string>(_settings.GameDirectories, DirectoryPath.Comparer);
        foreach (var directory in directories.Select(DirectoryPath.Normalize))
            if (known.Add(directory)) _settings.GameDirectories.Add(directory);
    }

    private void RefreshDlssStatus()
    {
        DlssFileStatuses.Clear();
        foreach (var name in InstallerService.RequiredDlssFiles)
            DlssFileStatuses.Add(new DlssFileStatus(name, File.Exists(Path.Combine(DlssFilesDirectory, name))));
        var confirmed = DlssFileStatuses.All(status => status.Exists);
        DlssFilesConfirmed = confirmed;
        DlssStatusTitle = confirmed ? "✓  User-supplied DLLs confirmed!" : "✕  User-supplied DLL mismatch:";
        DlssStatusColor = confirmed ? "#76B900" : "#FF5F63";
    }

    private void GridHeader_Click(object sender, RoutedEventArgs e)
    {
        if (e.OriginalSource is not GridViewColumnHeader { Tag: string property }) return;
        _sortDirection = _sortProperty == property && _sortDirection == ListSortDirection.Ascending
            ? ListSortDirection.Descending
            : ListSortDirection.Ascending;
        _sortProperty = property;
        var view = CollectionViewSource.GetDefaultView(Games);
        view.SortDescriptions.Clear();
        view.SortDescriptions.Add(new SortDescription(property, _sortDirection));
    }

    private void AppendLog(string message)
    {
        _store.Log(message);
        LoadLog();
    }

    private void LoadLog()
    {
        try { LogText = File.Exists(_store.LogPath) ? File.ReadAllText(_store.LogPath) : "No operations yet."; }
        catch { LogText = "Log unavailable."; }
    }

    private void Show(string message, MessageBoxImage image = MessageBoxImage.Information) =>
        ThemedDialog.Show(this, "DLAssAss 5 Tool", message, image);

    private void ShowLoadingOverlay(string text)
    {
        LoadingOverlayText = text;
        IsLoadingOverlayVisible = true;
    }

    private void Set<T>(ref T field, T value, [CallerMemberName] string? name = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return;
        field = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }

    private void Notify(string name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}

public sealed record DlssFileStatus(string Name, bool Exists)
{
    public string Symbol => Exists ? "✓" : "✕";
    public string Color => Exists ? "#76B900" : "#FF5F63";
}

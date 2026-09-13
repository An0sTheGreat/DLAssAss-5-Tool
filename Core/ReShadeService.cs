using System.Diagnostics;
using System.Net.Http;
using System.Text.RegularExpressions;

namespace DLSS5ManAger.Core;

public sealed class ReShadeService
{
    private const long MaximumInstallerBytes = 128L * 1024 * 1024;
    private static readonly Uri HomePage = new("https://reshade.me/");
    private static readonly HttpClient Client = new() { Timeout = TimeSpan.FromMinutes(2) };
    private static readonly Regex AddonLink = new(
        "href\\s*=\\s*[\\\"'](?<url>/downloads/ReShade_Setup_(?<version>\\d+(?:\\.\\d+){2,3})_Addon\\.exe)[\\\"']",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

    static ReShadeService() => Client.DefaultRequestHeaders.UserAgent.ParseAdd("DLAssAss5Tool/0.1");

    public async Task<ReShadeInstallResult> InstallLatestAsync(
        GameEntry game, string graphicsApi, ReShadeInstallMode installMode)
    {
        var executablePath = game.ExecutablePath;
        if (!File.Exists(executablePath) || !Path.GetExtension(executablePath).Equals(".exe", StringComparison.OrdinalIgnoreCase))
            return new(false, "The selected game does not have a valid executable.");
        var api = ResolveInstallerApi(graphicsApi);
        if (api is null)
            return new(false, "ReShade installation requires one unambiguous detected graphics API.");

        string? setupPath = null;
        var proxyBackups = new List<(string Original, string Backup)>();
        try
        {
            var release = ParseLatestAddonInstaller(await Client.GetStringAsync(HomePage));
            setupPath = Path.Combine(Path.GetTempPath(), $"ReShade_Setup_{release.Version}_Addon_{Guid.NewGuid():N}.exe");
            using (var response = await Client.GetAsync(release.DownloadUrl, HttpCompletionOption.ResponseHeadersRead))
            {
                response.EnsureSuccessStatusCode();
                var finalUrl = response.RequestMessage?.RequestUri;
                if (finalUrl?.Scheme != Uri.UriSchemeHttps ||
                    !finalUrl.Host.Equals("reshade.me", StringComparison.OrdinalIgnoreCase))
                    throw new InvalidDataException("The official download redirected outside reshade.me.");
                if (response.Content.Headers.ContentLength is > MaximumInstallerBytes)
                    throw new InvalidDataException("The official installer response was unexpectedly large.");
                await using var input = await response.Content.ReadAsStreamAsync();
                await using var output = File.Create(setupPath);
                await CopyToLimitedAsync(input, output);
            }
            ValidateInstaller(setupPath);

            var expectedProxy = ExpectedProxyName(graphicsApi);
            var expectedPath = expectedProxy is null ? null : Path.Combine(Path.GetDirectoryName(executablePath)!, expectedProxy);
            // Update mode bypasses Setup's first-install collision check.
            // Never allow it to overwrite another mod's proxy.
            if (expectedPath is not null && File.Exists(expectedPath) && !IsReShadeModule(expectedPath))
                throw new InvalidOperationException($"{expectedProxy} already exists and is not a verified ReShade module.");
            if (expectedPath is not null && File.Exists(expectedPath))
            {
                var backup = expectedPath + $".dlss5manager-{Guid.NewGuid():N}.bak";
                File.Copy(expectedPath, backup);
                proxyBackups.Add((expectedPath, backup));
            }
            if (expectedProxy is not null)
            {
                foreach (var path in game.ReShadeModulePaths.Where(path =>
                             !Path.GetFileName(path).Equals(expectedProxy, StringComparison.OrdinalIgnoreCase)))
                {
                    if (!IsReShadeModule(path)) continue;
                    var backup = path + $".dlss5manager-{Guid.NewGuid():N}.bak";
                    File.Move(path, backup);
                    proxyBackups.Add((path, backup));
                }
            }

            var start = new ProcessStartInfo(setupPath) { UseShellExecute = true, Verb = "runas" };
            foreach (var argument in InstallerArguments(executablePath, api, game.HasReShade, installMode))
                start.ArgumentList.Add(argument);
            using var process = Process.Start(start) ?? throw new InvalidOperationException("ReShade Setup did not start.");
            await process.WaitForExitAsync();
            if (process.ExitCode != 0)
                throw new InvalidOperationException($"ReShade Setup {release.Version} exited with code {process.ExitCode}.");
            if (installMode == ReShadeInstallMode.InteractivePackages && !game.HasReShade &&
                !InstallationPresent(executablePath, graphicsApi))
                throw new OperationCanceledException("ReShade Setup closed without installing ReShade.");
            // Keep successful reinstall backups for manual recovery.
            proxyBackups.Clear();
            return installMode == ReShadeInstallMode.ReShadeOnly
                ? new(true, $"ReShade {release.Version} with full add-on support was installed for {graphicsApi} without shaders.")
                : new(true, $"ReShade Setup {release.Version} completed for {graphicsApi} with your selected shader and add-on packages.");
        }
        catch (Exception ex)
        {
            var restoreErrors = RestoreProxyBackups(proxyBackups);
            return new(false, "ReShade installation failed: " + ex.Message + restoreErrors);
        }
        finally
        {
            if (setupPath is not null)
                try { File.Delete(setupPath); } catch { }
        }
    }

    internal static IReadOnlyList<string> InstallerArguments(
        string executablePath, string api, bool hasReShade, ReShadeInstallMode installMode)
    {
        var arguments = new List<string> { executablePath };
        if (installMode == ReShadeInstallMode.ReShadeOnly) arguments.Add("--headless");
        arguments.AddRange(["--api", api]);
        if (hasReShade)
            arguments.AddRange(["--state", installMode == ReShadeInstallMode.ReShadeOnly ? "update" : "modify"]);
        return arguments;
    }

    private static bool InstallationPresent(string executablePath, string graphicsApi)
    {
        var directory = Path.GetDirectoryName(executablePath)!;
        var proxy = ExpectedProxyName(graphicsApi);
        return proxy is not null
            ? IsReShadeModule(Path.Combine(directory, proxy))
            : File.Exists(Path.Combine(directory, "ReShade.ini"));
    }

    internal static string? ResolveInstallerApi(string graphicsApi)
    {
        var apis = graphicsApi.Split('/', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        if (apis.Length != 1) return null;
        return apis[0].ToUpperInvariant() switch
        {
            "DX9" => "d3d9",
            "DX10" or "DX11" or "DX12" => "dxgi",
            "OPENGL" => "opengl",
            "VULKAN" => "vulkan",
            _ => null
        };
    }

    internal static IReadOnlyList<string> SupportedGraphicsApis(string graphicsApi) => graphicsApi
        .Split('/', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
        .Where(api => ResolveInstallerApi(api) is not null)
        .Distinct(StringComparer.OrdinalIgnoreCase)
        .ToArray();

    internal static string? ExpectedProxyName(string graphicsApi) => ResolveInstallerApi(graphicsApi) switch
    {
        "d3d9" => "d3d9.dll",
        "dxgi" => "dxgi.dll",
        "opengl" => "opengl32.dll",
        _ => null
    };

    internal static bool IsReShadeModule(string path)
    {
        if (!new[] { "d3d9.dll", "d3d10.dll", "d3d11.dll", "d3d12.dll", "dxgi.dll", "opengl32.dll" }
            .Contains(Path.GetFileName(path), StringComparer.OrdinalIgnoreCase)) return false;
        try { return FileVersionInfo.GetVersionInfo(path).ProductName?.Equals("ReShade", StringComparison.OrdinalIgnoreCase) == true; }
        catch { return false; }
    }

    private static string RestoreProxyBackups(IEnumerable<(string Original, string Backup)> backups)
    {
        var errors = new List<string>();
        foreach (var (original, backup) in backups.Reverse())
        {
            try { if (File.Exists(backup)) File.Copy(backup, original, true); }
            catch (Exception ex) { errors.Add(ex.Message); }
        }
        return errors.Count == 0 ? "" : " ReShade proxy restoration also failed: " + string.Join("; ", errors);
    }

    internal static ReShadeRelease ParseLatestAddonInstaller(string html)
    {
        var releases = AddonLink.Matches(html).Select(match =>
        {
            var version = Version.Parse(match.Groups["version"].Value);
            var url = new Uri(HomePage, match.Groups["url"].Value);
            return new ReShadeRelease(version, url);
        }).Where(release => release.DownloadUrl.Scheme == Uri.UriSchemeHttps &&
            release.DownloadUrl.Host.Equals("reshade.me", StringComparison.OrdinalIgnoreCase)).ToArray();
        return releases.OrderByDescending(release => release.Version).FirstOrDefault()
            ?? throw new InvalidDataException("The official full add-on installer link was not found.");
    }

    private static void ValidateInstaller(string path)
    {
        var info = new FileInfo(path);
        if (info.Length < 1024 * 1024 || info.Length > MaximumInstallerBytes)
            throw new InvalidDataException("The official installer response had an unexpected size.");
        using var stream = File.OpenRead(path);
        if (stream.ReadByte() != 'M' || stream.ReadByte() != 'Z')
            throw new InvalidDataException("The official installer response was not a Windows executable.");
    }

    private static async Task CopyToLimitedAsync(Stream input, Stream output)
    {
        var buffer = new byte[81920];
        long total = 0;
        int read;
        while ((read = await input.ReadAsync(buffer)) != 0)
        {
            total += read;
            if (total > MaximumInstallerBytes)
                throw new InvalidDataException("The official installer response exceeded the size limit.");
            await output.WriteAsync(buffer.AsMemory(0, read));
        }
    }
}

public sealed record ReShadeRelease(Version Version, Uri DownloadUrl);
public sealed record ReShadeInstallResult(bool Success, string Message);
public enum ReShadeInstallMode { ReShadeOnly, InteractivePackages }

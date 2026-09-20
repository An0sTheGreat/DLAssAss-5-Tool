using System.Net.Http;
using System.Security.Cryptography;
using System.Text.Json;

namespace DLSS5ManAger.Core;

public sealed class Dlss5BridgeService
{
    public const string AssetName = "dlss5-bridge.addon64";
    private const long MaximumAddonBytes = 16L * 1024 * 1024;
    private static readonly Uri LatestReleaseApi = new("https://api.github.com/repos/NIGos/dlss5-bridge/releases/latest");
    private static readonly HttpClient Client = CreateClient();

    public async Task<Dlss5BridgeDownload> DownloadLatestAsync()
    {
        using var releaseResponse = await Client.GetAsync(LatestReleaseApi, HttpCompletionOption.ResponseHeadersRead);
        releaseResponse.EnsureSuccessStatusCode();
        var releaseUrl = releaseResponse.RequestMessage?.RequestUri;
        if (releaseUrl?.Scheme != Uri.UriSchemeHttps ||
            !releaseUrl.Host.Equals("api.github.com", StringComparison.OrdinalIgnoreCase))
            throw new InvalidDataException("The latest Bridge release lookup redirected outside GitHub.");
        var release = ParseLatestRelease(await releaseResponse.Content.ReadAsStringAsync());
        var temporaryDirectory = Path.Combine(Path.GetTempPath(), $"DLAssAss5Tool-Bridge-{Guid.NewGuid():N}");
        var path = Path.Combine(temporaryDirectory, AssetName);
        Directory.CreateDirectory(temporaryDirectory);

        try
        {
            using var response = await Client.GetAsync(release.DownloadUrl, HttpCompletionOption.ResponseHeadersRead);
            response.EnsureSuccessStatusCode();
            var finalUrl = response.RequestMessage?.RequestUri;
            if (finalUrl is null || !IsGitHubDownload(finalUrl))
                throw new InvalidDataException("The official Bridge download redirected outside GitHub.");
            if (response.Content.Headers.ContentLength is > MaximumAddonBytes)
                throw new InvalidDataException("The official Bridge response was unexpectedly large.");

            await using (var input = await response.Content.ReadAsStreamAsync())
            await using (var output = File.Create(path))
                await CopyToLimitedAsync(input, output);
            ValidateAddon(path, release);
            return new Dlss5BridgeDownload(release.Version, path);
        }
        catch
        {
            DeleteTemporaryDownload(path);
            throw;
        }
    }

    internal static bool RequiresBridge(string? graphicsApi) =>
        graphicsApi?.Equals("DX11", StringComparison.OrdinalIgnoreCase) == true;

    internal static Dlss5BridgeRelease ParseLatestRelease(string json)
    {
        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;
        if (root.GetProperty("draft").GetBoolean() || root.GetProperty("prerelease").GetBoolean())
            throw new InvalidDataException("GitHub did not return a stable Bridge release.");
        var version = root.GetProperty("tag_name").GetString();
        if (string.IsNullOrWhiteSpace(version))
            throw new InvalidDataException("The official Bridge release did not include a version.");

        foreach (var asset in root.GetProperty("assets").EnumerateArray())
        {
            if (!asset.GetProperty("name").GetString()!.Equals(AssetName, StringComparison.Ordinal)) continue;
            var urlText = asset.GetProperty("browser_download_url").GetString();
            var size = asset.GetProperty("size").GetInt64();
            var digest = asset.TryGetProperty("digest", out var digestElement) ? digestElement.GetString() : null;
            if (!Uri.TryCreate(urlText, UriKind.Absolute, out var url) ||
                url.Scheme != Uri.UriSchemeHttps || !url.Host.Equals("github.com", StringComparison.OrdinalIgnoreCase))
                throw new InvalidDataException("The official Bridge asset URL was invalid.");
            if (size <= 0 || size > MaximumAddonBytes)
                throw new InvalidDataException("The official Bridge asset had an unexpected size.");
            if (digest is null || !digest.StartsWith("sha256:", StringComparison.OrdinalIgnoreCase) ||
                digest.Length != 71 || !digest[7..].All(Uri.IsHexDigit))
                throw new InvalidDataException("The official Bridge asset did not include a valid SHA-256 digest.");
            return new Dlss5BridgeRelease(version, url, size, digest[7..]);
        }
        throw new InvalidDataException($"The official Bridge release did not include {AssetName}.");
    }

    internal static void DeleteTemporaryDownload(string path)
    {
        try { if (File.Exists(path)) File.Delete(path); } catch { }
        try
        {
            var directory = Path.GetDirectoryName(path);
            if (directory is not null && Directory.Exists(directory)) Directory.Delete(directory);
        }
        catch { }
    }

    private static HttpClient CreateClient()
    {
        var client = new HttpClient { Timeout = TimeSpan.FromMinutes(2) };
        client.DefaultRequestHeaders.UserAgent.ParseAdd("DLAssAss5Tool/1.1");
        client.DefaultRequestHeaders.Accept.ParseAdd("application/vnd.github+json");
        return client;
    }

    private static bool IsGitHubDownload(Uri url) => url.Scheme == Uri.UriSchemeHttps &&
        (url.Host.Equals("github.com", StringComparison.OrdinalIgnoreCase) ||
         url.Host.EndsWith(".githubusercontent.com", StringComparison.OrdinalIgnoreCase));

    private static void ValidateAddon(string path, Dlss5BridgeRelease release)
    {
        var info = new FileInfo(path);
        if (info.Length != release.Size || info.Length > MaximumAddonBytes)
            throw new InvalidDataException("The downloaded Bridge size did not match the official release.");
        using (var stream = File.OpenRead(path))
            if (stream.ReadByte() != 'M' || stream.ReadByte() != 'Z')
                throw new InvalidDataException("The official Bridge response was not a Windows add-on.");
        using var hashStream = File.OpenRead(path);
        var hash = Convert.ToHexString(SHA256.HashData(hashStream));
        if (!hash.Equals(release.Sha256, StringComparison.OrdinalIgnoreCase))
            throw new InvalidDataException("The downloaded Bridge failed SHA-256 verification.");
    }

    private static async Task CopyToLimitedAsync(Stream input, Stream output)
    {
        var buffer = new byte[81920];
        long total = 0;
        int read;
        while ((read = await input.ReadAsync(buffer)) != 0)
        {
            total += read;
            if (total > MaximumAddonBytes)
                throw new InvalidDataException("The official Bridge response exceeded the size limit.");
            await output.WriteAsync(buffer.AsMemory(0, read));
        }
    }
}

public sealed record Dlss5BridgeRelease(string Version, Uri DownloadUrl, long Size, string Sha256);

public sealed class Dlss5BridgeDownload(string version, string path) : IDisposable
{
    public string Version { get; } = version;
    public string Path { get; } = path;
    public void Dispose() => Dlss5BridgeService.DeleteTemporaryDownload(Path);
}

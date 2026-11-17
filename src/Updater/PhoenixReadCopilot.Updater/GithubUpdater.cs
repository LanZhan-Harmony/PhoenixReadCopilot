using System.Net.Http.Headers;
using Serde;
using Serde.Json;

namespace PhoenixReadCopilot.Updater;

/// <summary>
/// Self-updater for apps distributed via GitHub Releases. Lists the repo's releases,
/// selects the asset for the configured platform (per <see cref="UpdaterOptions"/>),
/// downloads and verifies it, then hands off to the new binary to replace the running
/// one in place.
/// <para>
/// Draft releases are skipped; prereleases are flagged. When GitHub reports an asset
/// <c>digest</c> (e.g. <c>sha256:…</c>) it is verified against the download.
/// </para>
/// <para>
/// Works with <b>private</b> repositories: supply an <c>authToken</c> and the updater
/// authenticates the API call and downloads through the authenticated asset endpoint
/// (with <c>Accept: application/octet-stream</c>) rather than the public
/// <c>browser_download_url</c>. The token is fetched per request via a delegate so
/// short-lived / rotating credentials (GitHub App installation tokens, fine-grained
/// PATs) refresh automatically.
/// </para>
/// </summary>
public sealed class GitHubUpdater : Updater
{
    private const string ApiVersion = "2022-11-28";

    private readonly string _owner;
    private readonly string _repo;
    private readonly Func<CancellationToken, Task<string?>>? _authToken;
    private readonly HttpClient _http;

    /// <param name="owner">Repository owner (user or org).</param>
    /// <param name="repo">Repository name.</param>
    /// <param name="options">Identity, version, platform, and swap settings.</param>
    /// <param name="authToken">
    /// Optional callback returning a bearer token for the GitHub API. Required for
    /// private repositories; omit for public ones. Awaited once per request so a
    /// consumer can fetch/cache/refresh rotating, short-lived tokens however it likes.
    /// </param>
    /// <param name="http">Optional pre-configured <see cref="HttpClient"/> (e.g. with proxy/handlers).</param>
    public GitHubUpdater(
        string owner,
        string repo,
        UpdaterOptions options,
        Func<CancellationToken, Task<string?>>? authToken = null,
        HttpClient? http = null
    )
        : base(options)
    {
        _owner = owner;
        _repo = repo;
        _authToken = authToken;
        _http = http ?? new HttpClient();
        if (_http.DefaultRequestHeaders.UserAgent.Count == 0)
        {
            _http.DefaultRequestHeaders.UserAgent.Add(
                new ProductInfoHeaderValue("selfupdater", "1.0")
            );
        }
    }

    private bool IsPrivate => _authToken is not null;

    internal override async Task<IReadOnlyList<SourceAsset>> GetAssetsAsync(CancellationToken ct)
    {
        var url = $"https://api.github.com/repos/{_owner}/{_repo}/releases";
        using HttpRequestMessage req = new(HttpMethod.Get, url);
        req.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github+json"));
        req.Headers.Add("X-GitHub-Api-Version", ApiVersion);
        await AddAuthAsync(req, ct).ConfigureAwait(false);

        string json;
        try
        {
            using HttpResponseMessage resp = await _http.SendAsync(req, ct).ConfigureAwait(false);
            if (!resp.IsSuccessStatusCode)
            {
                return [];
            }

            json = await resp.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
        }
        catch (Exception e) when (e is not OperationCanceledException)
        {
            return [];
        }

        List<GitHubRelease> releases;
        try
        {
            releases = JsonSerializer.Deserialize(json, List<GitHubRelease>.Deserialize);
        }
        catch (Exception)
        {
            return [];
        }

        List<SourceAsset> assets = [];
        foreach (GitHubRelease release in releases)
        {
            if (release.Draft == true)
            {
                continue;
            }

            foreach (GitHubAsset a in release.Assets)
            {
                // For private repos download through the authenticated asset API
                // endpoint; for public repos the browser URL needs no credentials.
                var location = IsPrivate ? (a.Url ?? a.BrowserDownloadUrl) : a.BrowserDownloadUrl;
                assets.Add(
                    new SourceAsset(
                        a.Name,
                        location,
                        ParseDigest(a.Digest),
                        a.Size,
                        release.Prerelease ?? false
                    )
                );
            }
        }

        return assets;
    }

    internal override async Task<Stream> OpenAssetAsync(SourceAsset asset, CancellationToken ct)
    {
        using HttpRequestMessage req = new(HttpMethod.Get, asset.Location);
        // The asset API returns a 302 to a signed CDN URL when asked for octets.
        // HttpClient drops the Authorization header on the cross-host redirect, so
        // sending it here is safe for both public and private downloads.
        req.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/octet-stream"));
        await AddAuthAsync(req, ct).ConfigureAwait(false);

        HttpResponseMessage resp = await _http
            .SendAsync(req, HttpCompletionOption.ResponseHeadersRead, ct)
            .ConfigureAwait(false);
        resp.EnsureSuccessStatusCode();
        return await resp.Content.ReadAsStreamAsync(ct).ConfigureAwait(false);
    }

    /// <summary>GitHub reports asset digests as "&lt;algo&gt;:&lt;hex&gt;"; we use the SHA-256 hex.</summary>
    private static string? ParseDigest(string? digest)
    {
        if (string.IsNullOrEmpty(digest))
        {
            return null;
        }

        const string prefix = "sha256:";
        return digest.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)
            ? digest[prefix.Length..]
            : null;
    }

    private async Task AddAuthAsync(HttpRequestMessage req, CancellationToken ct)
    {
        if (_authToken is null)
        {
            return;
        }

        if (await _authToken(ct).ConfigureAwait(false) is { Length: > 0 } token)
        {
            req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }
    }
}

[GenerateSerde]
internal sealed partial record GitHubRelease
{
    [SerdeMemberOptions(Rename = "tag_name")]
    public required string TagName { get; init; }

    public bool? Draft { get; init; }

    public bool? Prerelease { get; init; }

    public required List<GitHubAsset> Assets { get; init; }
}

[GenerateSerde]
internal sealed partial record GitHubAsset
{
    public required string Name { get; init; }

    /// <summary>The asset API URL, used for authenticated (private) downloads.</summary>
    public string? Url { get; init; }

    [SerdeMemberOptions(Rename = "browser_download_url")]
    public required string BrowserDownloadUrl { get; init; }

    /// <summary>GitHub-published content digest, e.g. <c>sha256:…</c> (may be absent).</summary>
    public string? Digest { get; init; }

    public long? Size { get; init; }
}

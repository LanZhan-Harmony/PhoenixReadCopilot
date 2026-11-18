using System.Net.Http.Headers;
using System.Runtime.InteropServices;
using System.Text.Json;

namespace PhoenixReadCopilot.Updater;

/// <summary>
/// PhoenixReadCopilot 的 GitHub Release 更新检查器.
/// <para>
/// 资源文件命名约定: <c>PhoenixReadCopilot_{version}_{arch}.zip</c>
/// (如 <c>PhoenixReadCopilot_0.1.0.0_x64.zip</c>).
/// </para>
/// <para>
/// 同时获取 Release 和 Pre-Release，取版本最新的一个.
/// 下载时通过 <see cref="GitHubMirrorService"/> 自动选择最快的 GitHub 镜像加速节点.
/// </para>
/// </summary>
public sealed class GitHubUpdater
{
    private const string Owner = "LanZhan-Harmony";
    private const string Repo = "PhoenixReadCopilot";
    private const string AppName = "PhoenixReadCopilot";

    private readonly HttpClient _http;
    private readonly GitHubMirrorService _mirrorService;
    private readonly string _architecture;
    private readonly TextWriter _log;

    /// <param name="http">可选的预配置 <see cref="HttpClient"/>.</param>
    /// <param name="log">日志输出.</param>
    public GitHubUpdater(HttpClient? http = null, TextWriter? log = null)
    {
        _http = http ?? CreateDefaultHttpClient();
        _log = log ?? Console.Out;
        _mirrorService = new GitHubMirrorService(_http, _log);
        _architecture = GetCurrentArchitecture();
    }

    /// <summary>
    /// 检查是否有新版本可用.
    /// 同时获取 Release 和 Pre-Release，取版本号最大的.
    /// </summary>
    /// <param name="currentVersion">当前应用版本.</param>
    /// <param name="ct">取消令牌.</param>
    /// <returns>若有新版本返回 <see cref="AppRelease"/>，否则 null.</returns>
    public async Task<AppRelease?> CheckForUpdateAsync(
        Version currentVersion,
        CancellationToken ct = default
    )
    {
        _log.WriteLine($"检查更新 (当前版本 {currentVersion}, 架构 {_architecture})...");

        List<GitHubRelease> releases = await FetchReleasesAsync(ct);
        if (releases.Count == 0)
        {
            _log.WriteLine("无法获取版本信息.");
            return null;
        }

        AppRelease? newest = FindNewestRelease(releases);
        if (newest is null)
        {
            _log.WriteLine($"未找到适用于 {_architecture} 架构的版本.");
            return null;
        }

        if (newest.Version <= currentVersion)
        {
            _log.WriteLine($"已是最新版本 (远程最新 {newest.Version}).");
            return null;
        }

        _log.WriteLine(
            $"发现新版本: {newest.Version} (当前 {currentVersion}){(newest.IsPrerelease ? " [预发布]" : "")}."
        );
        return newest;
    }

    /// <summary>
    /// 下载指定版本的更新包到目标路径.
    /// 自动通过镜像加速服务选择最快节点下载.
    /// </summary>
    /// <param name="release">要下载的发行版.</param>
    /// <param name="destPath">下载保存的本地路径.</param>
    /// <param name="progress">下载进度回调.</param>
    /// <param name="ct">取消令牌.</param>
    /// <returns>下载是否成功.</returns>
    public async Task<bool> DownloadUpdateAsync(
        AppRelease release,
        string destPath,
        IProgress<(long Downloaded, long Total)>? progress = null,
        CancellationToken ct = default
    )
    {
        _log.WriteLine($"开始下载更新 {release.Version}...");
        return await _mirrorService.DownloadWithFastestMirrorAsync(
            release.Asset.DownloadUrl,
            destPath,
            progress,
            ct
        );
    }

    private async Task<List<GitHubRelease>> FetchReleasesAsync(CancellationToken ct)
    {
        // 获取所有 releases (包含 pre-release)
        string url = $"https://api.github.com/repos/{Owner}/{Repo}/releases";
        using HttpRequestMessage req = new(HttpMethod.Get, url);
        req.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github+json"));
        req.Headers.Add("X-GitHub-Api-Version", "2022-11-28");
        req.Headers.UserAgent.Add(new ProductInfoHeaderValue("PhoenixReadCopilot", "1.0"));

        try
        {
            using HttpResponseMessage resp = await _http.SendAsync(req, ct);
            if (!resp.IsSuccessStatusCode)
            {
                _log.WriteLine($"GitHub API 返回 {resp.StatusCode}.");
                return [];
            }

            string json = await resp.Content.ReadAsStringAsync(ct);
            return JsonSerializer.Deserialize<List<GitHubRelease>>(json) ?? [];
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _log.WriteLine($"请求 GitHub API 失败: {ex.Message}");
            return [];
        }
    }

    /// <summary>
    /// 从所有 release 中找到版本号最大的、且有匹配当前架构资源的 release.
    /// </summary>
    private AppRelease? FindNewestRelease(List<GitHubRelease> releases)
    {
        AppRelease? best = null;

        foreach (GitHubRelease release in releases)
        {
            if (release.Draft)
            {
                continue;
            }

            // 从 tag_name 解析版本号
            // tag 格式: Pre-Release-0.1.0.0 或 Release-0.1.0.0 或 v0.1.0.0 或 0.1.0.0
            Version? version = ParseVersionFromTag(release.TagName);
            if (version is null)
            {
                continue;
            }

            // 查找匹配当前架构的 zip 资源
            SourceAsset? matchingAsset = FindMatchingAsset(release, version);
            if (matchingAsset is null)
            {
                continue;
            }

            AppRelease candidate = new(version, matchingAsset, release.Prerelease);
            if (best is null || candidate.Version > best.Version)
            {
                best = candidate;
            }
        }

        return best;
    }

    /// <summary>
    /// 从 release 的资源中查找匹配当前架构的 zip 文件.
    /// 资源命名: PhoenixReadCopilot_{version}_{arch}.zip
    /// </summary>
    private SourceAsset? FindMatchingAsset(GitHubRelease release, Version version)
    {
        // 期望的资源文件名
        string expectedName = $"{AppName}_{version}_{_architecture}.zip";

        foreach (GitHubAsset asset in release.Assets)
        {
            if (asset.Name.Equals(expectedName, StringComparison.OrdinalIgnoreCase))
            {
                return new SourceAsset(
                    asset.Name,
                    asset.BrowserDownloadUrl,
                    release.TagName,
                    asset.Size,
                    release.Prerelease
                );
            }
        }

        return null;
    }

    /// <summary>
    /// 从 tag name 中解析版本号.
    /// 支持格式: Pre-Release-0.1.0.0, Release-0.1.0.0, v0.1.0.0, 0.1.0.0
    /// </summary>
    internal static Version? ParseVersionFromTag(string tagName)
    {
        if (string.IsNullOrWhiteSpace(tagName))
        {
            return null;
        }

        ReadOnlySpan<char> tag = tagName.AsSpan().Trim();

        // 移除常见前缀
        ReadOnlySpan<char> preReleasePrefix = "Pre-Release-";
        ReadOnlySpan<char> releasePrefix = "Release-";
        ReadOnlySpan<char> vPrefix = "v";

        if (tag.StartsWith(preReleasePrefix, StringComparison.OrdinalIgnoreCase))
        {
            tag = tag[preReleasePrefix.Length..];
        }
        else if (tag.StartsWith(releasePrefix, StringComparison.OrdinalIgnoreCase))
        {
            tag = tag[releasePrefix.Length..];
        }
        else if (tag.StartsWith(vPrefix, StringComparison.OrdinalIgnoreCase) && tag.Length > 1)
        {
            tag = tag[vPrefix.Length..];
        }

        return Version.TryParse(tag, out Version? version) ? version : null;
    }

    /// <summary>
    /// 获取当前进程的架构标识 (x64, x86, ARM64).
    /// </summary>
    private static string GetCurrentArchitecture()
    {
        return RuntimeInformation.ProcessArchitecture switch
        {
            Architecture.X64 => "x64",
            Architecture.X86 => "x86",
            Architecture.Arm64 => "ARM64",
            _ => "x64",
        };
    }

    private static HttpClient CreateDefaultHttpClient()
    {
        HttpClient http = new();
        http.DefaultRequestHeaders.UserAgent.Add(
            new ProductInfoHeaderValue("PhoenixReadCopilot", "1.0")
        );
        return http;
    }
}

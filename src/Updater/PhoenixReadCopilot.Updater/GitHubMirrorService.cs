using System.Diagnostics;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace PhoenixReadCopilot.Updater;

/// <summary>
/// GitHub 镜像加速代理节点信息.
/// </summary>
public sealed class MirrorNode
{
    /// <summary>节点名称.</summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>节点代理 URL 前缀 (如 https://gh-proxy.com).</summary>
    public string Url { get; init; } = string.Empty;

    /// <summary>测速延迟 (毫秒). -1 表示不可用.</summary>
    public long Latency { get; set; } = -1;
}

/// <summary>
/// GitHub 镜像加速服务，从 github.akams.cn 获取镜像节点列表，并通过测速选择最快的节点下载.
/// </summary>
public sealed class GitHubMirrorService
{
    private const string MirrorApiUrl = "https://api.akams.cn/github";
    private const string OfficialGitHub = "https://github.com";

    /// <summary>内置的备用镜像节点列表，当 API 不可用时使用.</summary>
    private static readonly string[] FallbackMirrors =
    [
        "https://github.tbap.top",
        "https://gh.dpik.top",
        "https://gh-proxy.com",
        "https://ghproxy.icu",
        "https://ghfast.top",
        "https://github.chenc.dev",
    ];

    private readonly HttpClient _http;
    private readonly TextWriter _log;

    public GitHubMirrorService(HttpClient http, TextWriter? log = null)
    {
        _http = http;
        _log = log ?? Console.Out;
    }

    /// <summary>
    /// 获取所有可用的镜像节点（包括官方 GitHub 源）.
    /// 优先从 API 获取，失败时使用内置备用列表.
    /// </summary>
    public async Task<List<MirrorNode>> GetMirrorNodesAsync(CancellationToken ct = default)
    {
        List<MirrorNode> nodes = [];

        // 始终加入官方 GitHub 源
        nodes.Add(new MirrorNode { Name = "GitHub (官方)", Url = OfficialGitHub });

        try
        {
            List<MirrorNode> apiNodes = await FetchMirrorNodesFromApiAsync(ct);
            nodes.AddRange(apiNodes);
        }
        catch (Exception ex)
        {
            _log.WriteLine($"从镜像 API 获取节点失败: {ex.Message}，使用内置备用列表.");
            foreach (string url in FallbackMirrors)
            {
                nodes.Add(new MirrorNode { Name = new Uri(url).Host, Url = url });
            }
        }

        return nodes;
    }

    /// <summary>
    /// 对所有节点进行测速，返回按延迟排序的节点列表（不可用的排在最后）.
    /// </summary>
    /// <param name="nodes">待测速的节点列表.</param>
    /// <param name="testUrl">用于测速的完整 URL（不含镜像前缀的 GitHub 路径）.</param>
    /// <param name="ct">取消令牌.</param>
    public async Task<List<MirrorNode>> SpeedTestAsync(
        List<MirrorNode> nodes,
        string testUrl,
        CancellationToken ct = default
    )
    {
        Task[] tasks = new Task[nodes.Count];
        for (int i = 0; i < nodes.Count; i++)
        {
            int index = i;
            tasks[i] = TestNodeLatencyAsync(nodes[index], testUrl, ct);
        }

        await Task.WhenAll(tasks);

        // 可用的按延迟排序，不可用的排在最后
        nodes.Sort(
            (a, b) =>
            {
                if (a.Latency < 0 && b.Latency < 0)
                {
                    return 0;
                }

                if (a.Latency < 0)
                {
                    return 1;
                }

                if (b.Latency < 0)
                {
                    return -1;
                }

                return a.Latency.CompareTo(b.Latency);
            }
        );

        return nodes;
    }

    /// <summary>
    /// 构造通过镜像节点代理的下载 URL.
    /// </summary>
    /// <param name="mirrorBaseUrl">镜像节点基础 URL (如 https://gh-proxy.com).</param>
    /// <param name="originalUrl">原始 GitHub 下载 URL.</param>
    /// <returns>代理后的下载 URL.</returns>
    public static string BuildMirrorDownloadUrl(string mirrorBaseUrl, string originalUrl)
    {
        // 如果镜像就是官方 GitHub，直接返回原始 URL
        if (
            mirrorBaseUrl.Equals(OfficialGitHub, StringComparison.OrdinalIgnoreCase)
            || string.IsNullOrEmpty(mirrorBaseUrl)
        )
        {
            return originalUrl;
        }

        // 镜像格式: https://gh-proxy.com/https://github.com/owner/repo/releases/download/tag/file.zip
        string trimmed = mirrorBaseUrl.TrimEnd('/');
        return $"{trimmed}/{originalUrl}";
    }

    /// <summary>
    /// 选择最快的可用镜像节点，并通过该镜像下载文件.
    /// </summary>
    /// <param name="originalDownloadUrl">原始 GitHub 下载 URL.</param>
    /// <param name="destPath">下载保存的本地路径.</param>
    /// <param name="progress">下载进度回调 (已下载字节数, 总字节数).</param>
    /// <param name="ct">取消令牌.</param>
    /// <returns>下载是否成功.</returns>
    public async Task<bool> DownloadWithFastestMirrorAsync(
        string originalDownloadUrl,
        string destPath,
        IProgress<(long Downloaded, long Total)>? progress = null,
        CancellationToken ct = default
    )
    {
        List<MirrorNode> nodes = await GetMirrorNodesAsync(ct);
        _log.WriteLine($"获取到 {nodes.Count} 个镜像节点，开始测速...");

        // 使用 HEAD 请求测速
        nodes = await SpeedTestAsync(nodes, originalDownloadUrl, ct);

        foreach (MirrorNode node in nodes)
        {
            if (node.Latency >= 0)
            {
                _log.WriteLine($"  {node.Name} ({node.Url}): {node.Latency}ms");
            }
            else
            {
                _log.WriteLine($"  {node.Name} ({node.Url}): 不可用");
            }
        }

        // 尝试从最快的节点下载，失败则 fallback 到下一个
        foreach (MirrorNode node in nodes)
        {
            if (node.Latency < 0)
            {
                continue;
            }

            string downloadUrl = BuildMirrorDownloadUrl(node.Url, originalDownloadUrl);
            _log.WriteLine($"尝试从 {node.Name} 下载: {downloadUrl}");

            try
            {
                await DownloadFileAsync(downloadUrl, destPath, progress, ct);
                _log.WriteLine($"下载完成 (通过 {node.Name}).");
                return true;
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _log.WriteLine($"从 {node.Name} 下载失败: {ex.Message}，尝试下一个节点...");
            }
        }

        _log.WriteLine("所有镜像节点均下载失败.");
        return false;
    }

    private async Task TestNodeLatencyAsync(MirrorNode node, string testUrl, CancellationToken ct)
    {
        string url = BuildMirrorDownloadUrl(node.Url, testUrl);
        Stopwatch sw = Stopwatch.StartNew();
        try
        {
            using HttpRequestMessage req = new(HttpMethod.Head, url);
            req.Headers.UserAgent.Add(new ProductInfoHeaderValue("PhoenixReadCopilot", "1.0"));

            using CancellationTokenSource cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            cts.CancelAfter(TimeSpan.FromSeconds(10));

            using HttpResponseMessage resp = await _http.SendAsync(
                req,
                HttpCompletionOption.ResponseHeadersRead,
                cts.Token
            );

            sw.Stop();

            // 只要能连上就算可用（某些镜像 HEAD 可能返回非 200 但 GET 有效，放宽条件）
            node.Latency = sw.ElapsedMilliseconds;
        }
        catch
        {
            node.Latency = -1;
        }
    }

    private async Task DownloadFileAsync(
        string url,
        string destPath,
        IProgress<(long Downloaded, long Total)>? progress,
        CancellationToken ct
    )
    {
        using HttpRequestMessage req = new(HttpMethod.Get, url);
        req.Headers.UserAgent.Add(new ProductInfoHeaderValue("PhoenixReadCopilot", "1.0"));

        using HttpResponseMessage resp = await _http
            .SendAsync(req, HttpCompletionOption.ResponseHeadersRead, ct)
            .ConfigureAwait(false);
        resp.EnsureSuccessStatusCode();

        long totalBytes = resp.Content.Headers.ContentLength ?? -1;
        await using Stream srcStream = await resp.Content.ReadAsStreamAsync(ct);
        await using FileStream destStream = new(
            destPath,
            FileMode.Create,
            FileAccess.Write,
            FileShare.None,
            bufferSize: 81920
        );

        byte[] buffer = new byte[81920];
        long totalRead = 0;
        int bytesRead;
        while ((bytesRead = await srcStream.ReadAsync(buffer, ct)) > 0)
        {
            await destStream.WriteAsync(buffer.AsMemory(0, bytesRead), ct);
            totalRead += bytesRead;
            progress?.Report((totalRead, totalBytes));
        }
    }

    private async Task<List<MirrorNode>> FetchMirrorNodesFromApiAsync(CancellationToken ct)
    {
        using HttpRequestMessage req = new(HttpMethod.Get, MirrorApiUrl);
        req.Headers.UserAgent.Add(new ProductInfoHeaderValue("PhoenixReadCopilot", "1.0"));

        using CancellationTokenSource cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        cts.CancelAfter(TimeSpan.FromSeconds(5));

        using HttpResponseMessage resp = await _http.SendAsync(req, cts.Token);
        resp.EnsureSuccessStatusCode();

        string json = await resp.Content.ReadAsStringAsync(cts.Token);
        MirrorApiResponse? apiResp = JsonSerializer.Deserialize<MirrorApiResponse>(json);

        if (apiResp?.Data is not { Count: > 0 })
        {
            return [];
        }

        List<MirrorNode> nodes = [];
        foreach (MirrorApiNode node in apiResp.Data)
        {
            if (string.IsNullOrWhiteSpace(node.Url))
            {
                continue;
            }

            nodes.Add(
                new MirrorNode
                {
                    Name = node.Name ?? new Uri(node.Url).Host,
                    Url = node.Url.TrimEnd('/'),
                }
            );
        }

        return nodes;
    }

    private sealed class MirrorApiResponse
    {
        [JsonPropertyName("code")]
        public int Code { get; set; }

        [JsonPropertyName("data")]
        public List<MirrorApiNode>? Data { get; set; }
    }

    private sealed class MirrorApiNode
    {
        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("url")]
        public string Url { get; set; } = string.Empty;
    }
}

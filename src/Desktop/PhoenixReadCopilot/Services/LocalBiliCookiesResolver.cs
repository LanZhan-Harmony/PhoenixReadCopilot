using PhoenixReadCopilot.Helpers;
using Richasy.BiliKernel.Bili.Authorization;

namespace PhoenixReadCopilot.Services;

/// <summary>
/// 本地 Cookie 解析器（适用于非打包和打包环境）.
/// </summary>
public sealed class LocalBiliCookiesResolver : IBiliCookiesResolver
{
    private readonly string _filePath = Path.Combine(PathHelper.SettingsFolderPath, "cookies.json");
    private Dictionary<string, string>? _cacheCookies;

    /// <inheritdoc/>
    public IDictionary<string, string> GetCookies()
    {
        if (_cacheCookies != null)
        {
            return _cacheCookies;
        }

        try
        {
            if (File.Exists(_filePath))
            {
                string json = File.ReadAllText(_filePath);
                _cacheCookies = JsonAotSerializer.Deserialize<Dictionary<string, string>>(json);
            }
        }
        catch
        {
            _cacheCookies = null;
        }

        return _cacheCookies ?? [];
    }

    /// <inheritdoc/>
    public string GetCookieString()
    {
        IDictionary<string, string> cookies = GetCookies();
        if (cookies == null || cookies.Count == 0)
        {
            return string.Empty;
        }

        return string.Join("; ", cookies.Select(c => $"{c.Key}={c.Value}"));
    }

    /// <inheritdoc/>
    public void SaveCookies(IDictionary<string, string> cookies)
    {
        _cacheCookies = cookies is Dictionary<string, string> dict ? dict : [with(cookies)];
        try
        {
            Directory.CreateDirectory(PathHelper.SettingsFolderPath);
            string json = JsonAotSerializer.Serialize(_cacheCookies);
            File.WriteAllText(_filePath, json);
        }
        catch { }
    }

    /// <inheritdoc/>
    public void RemoveCookies()
    {
        _cacheCookies = null;
        try
        {
            if (File.Exists(_filePath))
            {
                File.Delete(_filePath);
            }
        }
        catch { }
    }
}

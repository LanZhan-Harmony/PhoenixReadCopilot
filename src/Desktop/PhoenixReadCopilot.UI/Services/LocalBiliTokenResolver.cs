using PhoenixReadCopilot.UI.Helpers;
using Richasy.BiliKernel.Bili.Authorization;
using Richasy.BiliKernel.Models.Authorization;

namespace PhoenixReadCopilot.UI.Services;

/// <summary>
/// 本地 Token 解析器（适用于非打包和打包环境）.
/// </summary>
public sealed class LocalBiliTokenResolver : IBiliTokenResolver
{
    private readonly string _filePath = Path.Combine(PathHelper.SettingsFolderPath, "token.json");
    private BiliToken? _cacheToken;

    /// <inheritdoc/>
    public BiliToken? GetToken()
    {
        if (_cacheToken != null)
        {
            return _cacheToken;
        }

        try
        {
            if (File.Exists(_filePath))
            {
                var json = File.ReadAllText(_filePath);
                _cacheToken = JsonAotSerializer.Deserialize<BiliToken>(json);
            }
        }
        catch
        {
            _cacheToken = null;
        }

        return _cacheToken;
    }

    /// <inheritdoc/>
    public void SaveToken(BiliToken token)
    {
        _cacheToken = token;
        try
        {
            Directory.CreateDirectory(PathHelper.SettingsFolderPath);
            var json = JsonAotSerializer.Serialize(token);
            File.WriteAllText(_filePath, json);
        }
        catch { }
    }

    /// <inheritdoc/>
    public void RemoveToken()
    {
        _cacheToken = null;
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

using PhoenixReadCopilot.UI.ViewModels.Items;
using System.Globalization;
using System.Reflection;
using System.Text.RegularExpressions;
using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.UI.Xaml;
using PhoenixReadCopilot.UI.Helpers;
using PhoenixReadCopilot.UI.Models.Constants;
using PhoenixReadCopilot.UI.Models.Settings;
using PhoenixReadCopilot.UI.Services;
using WinUIKernel.Share.Toolkits;

namespace PhoenixReadCopilot.UI.Toolkits;

/// <summary>
/// 应用工具组.
/// </summary>
internal sealed partial class AppToolkit : IAppToolkit
{
    private readonly AppSettings _settings;

    public AppToolkit(ISettingsService settingsService)
    {
        _settings = settingsService.Settings;
    }

    /// <inheritdoc/>
    public ApplicationTheme GetCurrentTheme()
    {
        var localTheme = _settings.AppTheme;
        return localTheme == ElementTheme.Default ? Application.Current.RequestedTheme
            : localTheme == ElementTheme.Light ? ApplicationTheme.Light
            : ApplicationTheme.Dark;
    }

    /// <inheritdoc/>
    public string GetPackageVersion()
    {
        if (RuntimeHelper.IsMSIX)
        {
            var appVersion = Windows.ApplicationModel.Package.Current.Id.Version;
            return $"{appVersion.Major}.{appVersion.Minor}.{appVersion.Build}.{appVersion.Revision}";
        }

        var version =
            Assembly.GetEntryAssembly()?.GetName().Version
            ?? Assembly.GetExecutingAssembly().GetName().Version;
        return $"{version?.Major}.{version?.Minor}.{version?.Build}.{version?.Revision}";
    }

    /// <inheritdoc/>
    public void ResetControlTheme(FrameworkElement element) =>
        element.RequestedTheme = _settings.AppTheme;

    /// <summary>
    /// 格式化时长.
    /// </summary>
    /// <returns>时长文本，如 01:33.</returns>
    public static string FormatDuration(TimeSpan duration) =>
        duration.TotalHours >= 1 ? duration.ToString(@"hh\:mm\:ss") : duration.ToString(@"mm\:ss");

    /// <summary>
    /// 格式化数量.
    /// </summary>
    /// <returns>数量文本，如 233万.</returns>
    public static string FormatCount(long count)
    {
        if (count < 0)
        {
            return string.Empty;
        }

        if (count >= 100_000_000)
        {
            var unit = Strings.Resources.Billion;
            return Math.Round(count / 100_000_000d, 2) + unit;
        }
        else if (count >= 10_000)
        {
            var unit = Strings.Resources.TenThousands;
            return Math.Round(count / 10_000d, 2) + unit;
        }

        return count.ToString();
    }

    /// <summary>
    /// 将颜色代码转换为Windows.UI.Color对象.
    /// </summary>
    /// <param name="hexCode">颜色代码.</param>
    /// <returns><see cref="Windows.UI.Color"/>.</returns>
    public static Windows.UI.Color HexToColor(string hexCode)
    {
        // 去除可能包含的 # 符号
        if (hexCode.StartsWith("#"))
        {
            hexCode = hexCode[1..];
        }

        if (int.TryParse(hexCode, out var intValue))
        {
            hexCode = intValue.ToString("X2");
        }

        var color = default(Windows.UI.Color);
        if (hexCode.Length == 4)
        {
            hexCode = "00" + hexCode;
        }

        if (hexCode.Length == 6)
        {
            color.R = byte.Parse(hexCode[..2], NumberStyles.HexNumber);
            color.G = byte.Parse(hexCode.Substring(2, 2), NumberStyles.HexNumber);
            color.B = byte.Parse(hexCode.Substring(4, 2), NumberStyles.HexNumber);
            color.A = 255;
        }

        if (hexCode.Length == 8)
        {
            color.R = byte.Parse(hexCode.Substring(2, 2), NumberStyles.HexNumber);
            color.G = byte.Parse(hexCode.Substring(4, 2), NumberStyles.HexNumber);
            color.B = byte.Parse(hexCode.Substring(6, 2), NumberStyles.HexNumber);
            color.A = byte.Parse(hexCode[..2], NumberStyles.HexNumber);
        }

        return color;
    }

    /// <summary>
    /// 获取偏好解码模式.
    /// </summary>
    /// <returns>解码标识符.</returns>
    public static string GetPreferCodecId()
    {
        var preferCodec = ItemServices.Settings.PreferCodec;
        return preferCodec switch
        {
            PreferCodecType.H265 => "hev",
            PreferCodecType.Av1 => "av01",
            _ => "avc",
        };
    }

    /// <summary>
    /// 是否为P2P地址.
    /// </summary>
    /// <returns>检查结果.</returns>
    public static bool IsP2PUrl(string url)
    {
        var uri = new Uri(url);
        return P2PRegex().IsMatch(uri.Host);
    }

    [GeneratedRegex(@"(mcdn.bilivideo.(cn|com)|szbdyd.com)")]
    private static partial Regex P2PRegex();
}

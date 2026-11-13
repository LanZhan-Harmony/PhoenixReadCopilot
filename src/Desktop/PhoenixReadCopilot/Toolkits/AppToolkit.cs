using System.Globalization;
using System.Reflection;
using Microsoft.UI.Xaml;
using PhoenixReadCopilot.Core.Contracts.Services;
using PhoenixReadCopilot.Core.Enums;
using PhoenixReadCopilot.Core.Models.Settings;
using PhoenixReadCopilot.Extensions;
using PhoenixReadCopilot.Helpers;
using PhoenixReadCopilot.ViewModels.Items;
using Windows.ApplicationModel;
using Windows.UI;
using WinUIKernel.Share.Toolkits;

namespace PhoenixReadCopilot.Toolkits;

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
        AppTheme localTheme = _settings.AppTheme;
        return localTheme == AppTheme.Default ? Application.Current.RequestedTheme
            : localTheme == AppTheme.Light ? ApplicationTheme.Light
            : ApplicationTheme.Dark;
    }

    /// <inheritdoc/>
    public string GetPackageVersion()
    {
        if (RuntimeHelper.IsMSIX)
        {
            PackageVersion appVersion = Package.Current.Id.Version;
            return $"{appVersion.Major}.{appVersion.Minor}.{appVersion.Build}.{appVersion.Revision}";
        }

        Version? version = Assembly.GetEntryAssembly()?.GetName().Version
            ?? Assembly.GetExecutingAssembly().GetName().Version;
        return $"{version?.Major}.{version?.Minor}.{version?.Build}.{version?.Revision}";
    }

    /// <inheritdoc/>
    public void ResetControlTheme(FrameworkElement element) =>
        element.RequestedTheme = _settings.AppTheme.ToElementTheme();

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
            string unit = Strings.Resources.Billion;
            return Math.Round(count / 100_000_000d, 2) + unit;
        }
        else if (count >= 10_000)
        {
            string unit = Strings.Resources.TenThousands;
            return Math.Round(count / 10_000d, 2) + unit;
        }

        return count.ToString();
    }

    /// <summary>
    /// 将颜色代码转换为Windows.UI.Color对象.
    /// </summary>
    /// <param name="hexCode">颜色代码.</param>
    /// <returns><see cref="Color"/>.</returns>
    public static Color HexToColor(string hexCode)
    {
        // 去除可能包含的 # 符号
        if (hexCode.StartsWith('#'))
        {
            hexCode = hexCode[1..];
        }

        if (int.TryParse(hexCode, out int intValue))
        {
            hexCode = intValue.ToString("X2");
        }

        Color color = default;
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
        PreferCodecType preferCodec = ItemServices.Settings.PreferCodec;
        return preferCodec switch
        {
            PreferCodecType.H265 => "hev",
            PreferCodecType.Av1 => "av01",
            _ => "avc",
        };
    }
}

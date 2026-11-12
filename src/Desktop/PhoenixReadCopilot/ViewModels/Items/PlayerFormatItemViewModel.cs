using CommunityToolkit.Mvvm.ComponentModel;
using PhoenixReadApi;
using PhoenixReadCopilot.Core.Models.Player;
using WinUIKernel.Share.ViewModels;

namespace PhoenixReadCopilot.ViewModels.Items;

/// <summary>
/// 播放器画质项视图模型.
/// </summary>
public sealed partial class PlayerFormatItemViewModel : ViewModelBase<PlayerFormatInformation>
{
    [ObservableProperty]
    public partial bool IsEnabled { get; set; }

    /// <summary>
    /// 红果媒体源对象.
    /// </summary>
    public PlaybackMedia? PlaybackMedia { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="PlayerFormatItemViewModel"/> class.
    /// </summary>
    public PlayerFormatItemViewModel(PlayerFormatInformation data)
        : base(data)
    {
        bool userIsVip = ItemServices.Account.MyProfile?.IsVip ?? false;
        IsEnabled = !data.NeedVip || (data.NeedVip && userIsVip);
    }

    /// <summary>
    /// 使用红果 PlaybackMedia 初始化画质项.
    /// </summary>
    public PlayerFormatItemViewModel(PlaybackMedia media)
        : base(
            new PlayerFormatInformation(
                GetQualityScore(media),
                FormatDescription(media),
                NeedVip: false,
                NeedLogin: false
            )
        )
    {
        PlaybackMedia = media;
        IsEnabled = true;
    }

    /// <summary>
    /// 获取画质综合得分或分辨率数值用于排序.
    /// </summary>
    public static int GetQualityScore(PlaybackMedia media)
    {
        int res = GetEffectiveResolution(media);
        if (res > 0)
        {
            return res;
        }

        if (media.Quality > 0)
        {
            return media.Quality;
        }

        return (int)media.Definition;
    }

    /// <summary>
    /// 计算短剧视频有效短边分辨率（竖屏取宽，横屏取高）.
    /// </summary>
    public static int GetEffectiveResolution(PlaybackMedia media)
    {
        if (media is { Width: > 0, Height: > 0 })
        {
            return Math.Min(media.Width, media.Height);
        }

        if (media.Height > 0)
        {
            return media.Height;
        }

        return (int)media.Definition;
    }

    /// <summary>
    /// 获取用于画质选项去重的分组键.
    /// </summary>
    public static string GetFormatKey(PlaybackMedia media)
    {
        string desc = FormatDescription(media);
        return desc;
    }

    /// <summary>
    /// 生成标准易懂的清晰度文案.
    /// </summary>
    public static string FormatDescription(PlaybackMedia media)
    {
        int res = GetEffectiveResolution(media);
        string text;

        if (res >= 1080)
        {
            text = "1080P 超清";
        }
        else if (res >= 720)
        {
            text = "720P 高清";
        }
        else if (res >= 540)
        {
            text = "540P 高清";
        }
        else if (res >= 480)
        {
            text = "480P 标清";
        }
        else if (res >= 360)
        {
            text = "360P 流畅";
        }
        else
        {
            text = media.Definition switch
            {
                MediaDefinition.P1080 => "1080P 超清",
                MediaDefinition.P720 => "720P 高清",
                MediaDefinition.P540 => "540P 高清",
                MediaDefinition.P480 => "480P 标清",
                MediaDefinition.P360 => "360P 流畅",
                _ => !string.IsNullOrWhiteSpace(media.QualityName)
                && !media.QualityName.Equals("normal", StringComparison.OrdinalIgnoreCase)
                    ? media.QualityName
                    : "标清",
            };
        }

        if (media.Codec == MediaCodec.ByteVC1)
        {
            text += " (H.265)";
        }

        return text;
    }
}

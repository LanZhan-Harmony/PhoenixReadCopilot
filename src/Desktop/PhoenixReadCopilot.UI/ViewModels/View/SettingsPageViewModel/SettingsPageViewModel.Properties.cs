using CommunityToolkit.Mvvm.ComponentModel;
using PhoenixReadCopilot.UI.Models.Constants;

namespace PhoenixReadCopilot.UI.ViewModels.View;

/// <summary>
/// 设置页面视图模型.
/// </summary>
public sealed partial class SettingsPageViewModel
{
    private bool _isInitialized;

    [ObservableProperty]
    public partial ElementTheme AppTheme { get; set; }

    [ObservableProperty]
    public partial string AppThemeText { get; set; }

    [ObservableProperty]
    public partial bool IsTopNavShown { get; set; }

    [ObservableProperty]
    public partial bool IsAutoPlayWhenLoaded { get; set; }

    [ObservableProperty]
    public partial bool IsAutoPlayNextRecommendVideo { get; set; }

    [ObservableProperty]
    public partial PlayerDisplayMode DefaultPlayerDisplayMode { get; set; }

    [ObservableProperty]
    public partial bool AutoPlayNext { get; set; }

    [ObservableProperty]
    public partial bool PlayNextWithoutTip { get; set; }

    [ObservableProperty]
    public partial bool EndWithPlaylist { get; set; }

    [ObservableProperty]
    public partial PreferCodecType PreferCodec { get; set; }

    [ObservableProperty]
    public partial PreferQualityType PreferQuality { get; set; }

    [ObservableProperty]
    public partial PreferDecodeType PreferDecode { get; set; }

    [ObservableProperty]
    public partial PlayerType PlayerType { get; set; }

    [ObservableProperty]
    public partial MTCBehavior MTCBehavior { get; set; }

    [ObservableProperty]
    public partial double SingleFastForwardAndRewindSpan { get; set; }

    [ObservableProperty]
    public partial bool PlayerSpeedEnhancement { get; set; }

    [ObservableProperty]
    public partial bool GlobalPlayerSpeed { get; set; }

    [ObservableProperty]
    public partial string PackageVersion { get; set; }

    [ObservableProperty]
    public partial string Copyright { get; set; }

    [ObservableProperty]
    public partial bool IsCopyScreenshot { get; set; }

    [ObservableProperty]
    public partial bool IsOpenScreenshotFile { get; set; }

    [ObservableProperty]
    public partial bool AutoLoadHistory { get; set; }

    [ObservableProperty]
    public partial bool IsNotificationEnabled { get; set; }

    [ObservableProperty]
    public partial bool IsVideoMomentNotificationEnabled { get; set; }

    [ObservableProperty]
    public partial bool BottomProgressVisible { get; set; }

    [ObservableProperty]
    public partial bool NoP2P { get; set; }

    [ObservableProperty]
    public partial string DefaultDownloadPath { get; set; }

    [ObservableProperty]
    public partial bool DownloadWithDanmaku { get; set; }

    [ObservableProperty]
    public partial bool OpenFolderAfterDownload { get; set; }

    [ObservableProperty]
    public partial bool UseExternalBBDown { get; set; }

    [ObservableProperty]
    public partial bool OnlyCopyCommandWhenDownload { get; set; }

    [ObservableProperty]
    public partial bool WithoutCredentialWhenGenDownloadCommand { get; set; }

    [ObservableProperty]
    public partial bool FilterAISubtitle { get; set; }

    [ObservableProperty]
    public partial List<PlayerDisplayMode> PlayerDisplayModeCollection { get; set; }

    [ObservableProperty]
    public partial List<PreferCodecType> PreferCodecCollection { get; set; }

    [ObservableProperty]
    public partial List<PreferQualityType> PreferQualityCollection { get; set; }

    [ObservableProperty]
    public partial List<PreferDecodeType> PreferDecodeCollection { get; set; }

    [ObservableProperty]
    public partial List<PlayerType> PlayerTypeCollection { get; set; }

    [ObservableProperty]
    public partial List<MTCBehavior> MTCBehaviorCollection { get; set; }

    [ObservableProperty]
    public partial bool ShowSearchRecommend { get; set; }

    [ObservableProperty]
    public partial bool IsPopularNavVisible { get; set; }

    [ObservableProperty]
    public partial bool IsMomentNavVisible { get; set; }

    [ObservableProperty]
    public partial bool IsVideoNavVisible { get; set; }

    [ObservableProperty]
    public partial bool IsAnimeNavVisible { get; set; }

    [ObservableProperty]
    public partial bool IsCinemaNavVisible { get; set; }

    [ObservableProperty]
    public partial bool IsArticleNavVisible { get; set; }
}

using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.UI.Xaml;
using PhoenixReadCopilot.Core.Enums;

namespace PhoenixReadCopilot.Models.Settings;

/// <summary>
/// 应用程序全局设置模型.
/// </summary>
public sealed partial class AppSettings : ObservableObject
{
    // ================= 主题与外观 =================
    [ObservableProperty]
    public partial ElementTheme AppTheme { get; set; } = ElementTheme.Default;

    [ObservableProperty]
    public partial bool IsTopNavBarShown { get; set; } = true;

    // ================= 播放与行为 =================
    [ObservableProperty]
    public partial bool ShouldAutoPlay { get; set; } = false;

    [ObservableProperty]
    public partial bool AutoPlayNextRecommendVideo { get; set; } = false;

    [ObservableProperty]
    public partial bool AutoPlayNext { get; set; } = true;

    [ObservableProperty]
    public partial double SingleFastForwardAndRewindSpan { get; set; } = 15.0;

    [ObservableProperty]
    public partial bool IsPlayerSpeedEnhancement { get; set; } = false;

    [ObservableProperty]
    public partial bool IsPlayerSpeedShare { get; set; } = true;

    [ObservableProperty]
    public partial double PlayerSpeed { get; set; } = 1.0;

    [ObservableProperty]
    public partial int PlayerVolume { get; set; } = 100;

    [ObservableProperty]
    public partial bool AutoLoadHistory { get; set; } = true;

    [ObservableProperty]
    public partial bool ShouldReportProgress { get; set; } = true;

    [ObservableProperty]
    public partial PlayerDisplayMode DefaultPlayerDisplayMode { get; set; } =
        PlayerDisplayMode.Default;

    [ObservableProperty]
    public partial PreferCodecType PreferCodec { get; set; } = PreferCodecType.H264;

    [ObservableProperty]
    public partial PreferQualityType PreferQuality { get; set; } = PreferQualityType.Auto;

    [ObservableProperty]
    public partial PlayerType PlayerType { get; set; } = PlayerType.FFmpeg;

    [ObservableProperty]
    public partial MTCBehavior MTCBehavior { get; set; } = MTCBehavior.Manual;

    [ObservableProperty]
    public partial bool IsBottomProgressVisible { get; set; } = true;

    [ObservableProperty]
    public partial bool CopyAfterScreenshot { get; set; } = true;

    [ObservableProperty]
    public partial bool OpenAfterScreenshot { get; set; } = true;

    // ================= 弹幕与字幕 =================
    [ObservableProperty]
    public partial bool IsShowDanmaku { get; set; } = true;

    [ObservableProperty]
    public partial double DanmakuOpacity { get; set; } = 1.0;

    [ObservableProperty]
    public partial double DanmakuArea { get; set; } = 1.0;

    [ObservableProperty]
    public partial double DanmakuFontSize { get; set; } = 24.0;

    [ObservableProperty]
    public partial double DanmakuSpeed { get; set; } = 1.0;

    [ObservableProperty]
    public partial string DanmakuFontFamily { get; set; } = string.Empty;

    [ObservableProperty]
    public partial bool IsDanmakuBold { get; set; } = false;

    [ObservableProperty]
    public partial DanmakuLocation DanmakuLocation { get; set; } = DanmakuLocation.Scroll;

    [ObservableProperty]
    public partial bool IsDanmakuStandardSize { get; set; } = true;

    [ObservableProperty]
    public partial string DanmakuColor { get; set; } = string.Empty;

    [ObservableProperty]
    public partial bool IsSubtitleEnabled { get; set; } = false;

    [ObservableProperty]
    public partial bool FilterAISubtitle { get; set; } = true;

    // ================= 下载设置 =================
    [ObservableProperty]
    public partial string DownloadFolder { get; set; } =
        Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.MyVideos),
            "PhoenixRead Downloads"
        );

    [ObservableProperty]
    public partial bool OpenFolderAfterDownload { get; set; } = true;

    [ObservableProperty]
    public partial bool DownloadWithDanmaku { get; set; } = false;

    [ObservableProperty]
    public partial bool OnlyCopyCommandWhenDownload { get; set; } = false;

    [ObservableProperty]
    public partial bool WithoutCredentialWhenGenDownloadCommand { get; set; } = false;

    // ================= 通知与交互 =================
    [ObservableProperty]
    public partial bool IsNotificationEnabled { get; set; } = true;

    [ObservableProperty]
    public partial bool IsVideoMomentNotificationEnabled { get; set; } = true;

    [ObservableProperty]
    public partial bool ShowSearchRecommend { get; set; } = false;

    [ObservableProperty]
    public partial bool HideWhenCloseWindow { get; set; } = false;

    [ObservableProperty]
    public partial bool ShouldAskBeforeWindowClosed { get; set; } = true;

    [ObservableProperty]
    public partial bool ShouldSkipStartup { get; set; } = false;

    [ObservableProperty]
    public partial string LastSelectedFeaturePage { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string LastTenMomentIds { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string BewlyExtensionVersion { get; set; } = string.Empty;

    [ObservableProperty]
    public partial int LastSelectedVideoQuality { get; set; } = 0;

    [ObservableProperty]
    public partial bool IsVideoPlayerPartsOnlyIndex { get; set; } = false;

    [ObservableProperty]
    public partial bool IsOpenGLSupport { get; set; } = false;

    [ObservableProperty]
    public partial bool IsGpuChecked { get; set; } = false;

    [ObservableProperty]
    public partial string AppVersion { get; set; } = string.Empty;

    // ================= 窗口位置与尺寸 =================
    [ObservableProperty]
    public partial int MainWindowPositionLeft { get; set; } = 0;

    [ObservableProperty]
    public partial int MainWindowPositionTop { get; set; } = 0;

    [ObservableProperty]
    public partial double MainWindowWidth { get; set; } = 1120;

    [ObservableProperty]
    public partial double MainWindowHeight { get; set; } = 740;

    [ObservableProperty]
    public partial bool IsMainWindowMaximized { get; set; } = false;

    [ObservableProperty]
    public partial int PlayerWindowPositionLeft { get; set; } = 0;

    [ObservableProperty]
    public partial int PlayerWindowPositionTop { get; set; } = 0;

    [ObservableProperty]
    public partial double PlayerWindowWidth { get; set; } = 1120;

    [ObservableProperty]
    public partial double PlayerWindowHeight { get; set; } = 740;

    [ObservableProperty]
    public partial bool IsPlayerWindowMaximized { get; set; } = false;

    [ObservableProperty]
    public partial int GalleryWindowPositionLeft { get; set; } = 0;

    [ObservableProperty]
    public partial int GalleryWindowPositionTop { get; set; } = 0;

    [ObservableProperty]
    public partial double GalleryWindowWidth { get; set; } = 800;

    [ObservableProperty]
    public partial double GalleryWindowHeight { get; set; } = 600;

    [ObservableProperty]
    public partial bool IsGalleryWindowMaximized { get; set; } = false;

    // ================= 页面导航与栏宽 =================
    [ObservableProperty]
    public partial double PopularPageNavColumnWidth { get; set; } = 0;

    [ObservableProperty]
    public partial bool IsPopularPageNavColumnManualHide { get; set; } = false;

    [ObservableProperty]
    public partial string PopularPageLastSelectedSectionId { get; set; } = string.Empty;

    [ObservableProperty]
    public partial double CalendarPageNavColumnWidth { get; set; } = 0;

    [ObservableProperty]
    public partial bool IsCalendarPageNavColumnManualHide { get; set; } = false;

    [ObservableProperty]
    public partial double FindDramaPageNavColumnWidth { get; set; } = 0;

    [ObservableProperty]
    public partial bool IsFindDramaPageNavColumnManualHide { get; set; } = false;

    [ObservableProperty]
    public partial string FollowsPageLastSelectedGroupId { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string LastSelectedFavoriteSection { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string LastSelectedMessageSection { get; set; } = string.Empty;

    [ObservableProperty]
    public partial bool LastMomentSectionIsVideo { get; set; } = true;

    [ObservableProperty]
    public partial bool LastUserSpaceSectionIsVideo { get; set; } = true;

    // ================= 导航项可见性 =================
    [ObservableProperty]
    public partial bool IsPopularPageVisible { get; set; } = true;

    [ObservableProperty]
    public partial bool IsMomentPageVisible { get; set; } = true;

    [ObservableProperty]
    public partial bool IsCalendarPageVisible { get; set; } = true;

    [ObservableProperty]
    public partial bool IsFindDramaPageVisible { get; set; } = true;

    // ================= 动态扩展配置项 =================
    [ObservableProperty]
    public partial Dictionary<string, string> CustomSettings { get; set; } = new();

    /// <summary>
    /// 获取导航栏宽度.
    /// </summary>
    public double GetNavColumnWidth(string pageKey, double defaultValue) =>
        pageKey switch
        {
            "PopularPage" => PopularPageNavColumnWidth <= 0
                ? defaultValue
                : PopularPageNavColumnWidth,
            "CalendarPage" => CalendarPageNavColumnWidth <= 0
                ? defaultValue
                : CalendarPageNavColumnWidth,
            "FindDramaPage" => FindDramaPageNavColumnWidth <= 0
                ? defaultValue
                : FindDramaPageNavColumnWidth,
            _ => defaultValue,
        };

    /// <summary>
    /// 设置导航栏宽度.
    /// </summary>
    public void SetNavColumnWidth(string pageKey, double value)
    {
        switch (pageKey)
        {
            case "PopularPage":
                PopularPageNavColumnWidth = value;
                break;
            case "CalendarPage":
                CalendarPageNavColumnWidth = value;
                break;
            case "FindDramaPage":
                FindDramaPageNavColumnWidth = value;
                break;
        }
    }

    /// <summary>
    /// 获取导航栏是否手动隐藏.
    /// </summary>
    public bool GetNavColumnManualHide(string pageKey, bool defaultValue) =>
        pageKey switch
        {
            "PopularPage" => IsPopularPageNavColumnManualHide,
            "CalendarPage" => IsCalendarPageNavColumnManualHide,
            "FindDramaPage" => IsFindDramaPageNavColumnManualHide,
            _ => defaultValue,
        };

    /// <summary>
    /// 设置导航栏是否手动隐藏.
    /// </summary>
    public void SetNavColumnManualHide(string pageKey, bool value)
    {
        switch (pageKey)
        {
            case "PopularPage":
                IsPopularPageNavColumnManualHide = value;
                break;
            case "CalendarPage":
                IsCalendarPageNavColumnManualHide = value;
                break;
            case "FindDramaPage":
                IsFindDramaPageNavColumnManualHide = value;
                break;
        }
    }

    /// <summary>
    /// 获取页面是否可见.
    /// </summary>
    public bool GetPageVisible(Type pageType)
    {
        string name = pageType.Name;
        return name switch
        {
            "PopularPage" => IsPopularPageVisible,
            "MomentPage" => IsMomentPageVisible,
            "CalendarPage" => IsCalendarPageVisible,
            "FindDramaPage" => IsFindDramaPageVisible,
            _ => true,
        };
    }

    /// <summary>
    /// 设置页面是否可见.
    /// </summary>
    public void SetPageVisible(Type pageType, bool isVisible)
    {
        string name = pageType.Name;
        switch (name)
        {
            case "PopularPage":
                IsPopularPageVisible = isVisible;
                break;
            case "MomentPage":
                IsMomentPageVisible = isVisible;
                break;
            case "CalendarPage":
                IsCalendarPageVisible = isVisible;
                break;
            case "FindDramaPage":
                IsFindDramaPageVisible = isVisible;
                break;
        }
    }
}

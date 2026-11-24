using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using PhoenixReadApi;
using PhoenixReadCopilot.Core.Contracts.Media;
using PhoenixReadCopilot.Core.Contracts.Services;
using PhoenixReadCopilot.Core.Enums;
using PhoenixReadCopilot.Core.Models.Pin;
using PhoenixReadCopilot.Forms;
using PhoenixReadCopilot.Helpers;
using PhoenixReadCopilot.Pages.Overlay;
using ZLogger;

namespace PhoenixReadCopilot.ViewModels.Items;

/// <summary>
/// 阅读/观看历史记录条目视图模型.
/// </summary>
internal sealed partial class ReadHistoryItemViewModel : ViewModelBase, IPlayableMedia
{
    private readonly Action<ReadHistoryItemViewModel>? _removeAction;

    private static readonly ILogger<ReadHistoryItemViewModel> _logger =
        CommunityToolkit.Mvvm.DependencyInjection.Ioc.Default.GetRequiredService<
            ILogger<ReadHistoryItemViewModel>
        >();

    /// <summary>
    /// 观看历史记录原始实体.
    /// </summary>
    public ReadHistoryItem History { get; }

    /// <summary>
    /// 短剧 ID.
    /// </summary>
    public string DramaId => History.DramaId;

    /// <summary>
    /// 分集 ID.
    /// </summary>
    public string VideoId => History.VideoId;

    #region IPlayableMedia 契约实现

    /// <inheritdoc/>
    public string MediaId => History.DramaId;

    /// <inheritdoc/>
    public string Title => History.Title;

    /// <inheritdoc/>
    public string? CoverUrl => History.CoverUrl;

    /// <inheritdoc/>
    public MediaType Type => MediaType.Drama;

    /// <inheritdoc/>
    public string? InitialEpisodeId => History.VideoId;

    /// <inheritdoc/>
    public long? InitialPositionMs => History.CurrentPositionMs;

    #endregion

    #region 视图展示属性

    /// <summary>
    /// 封面 Uri.
    /// </summary>
    public Uri? Cover { get; }

    /// <summary>
    /// 观看分集序号.
    /// </summary>
    public int EpisodeNumber => History.EpisodeNumber;

    /// <summary>
    /// 副标题 (如 "第 12 集").
    /// </summary>
    public string Subtitle => EpisodeNumber > 0 ? $"第 {EpisodeNumber} 集" : string.Empty;

    /// <summary>
    /// 进度文本 (如 "01:23").
    /// </summary>
    public string? ProgressText { get; }

    /// <summary>
    /// 卡片呈现样式 (固定为历史样式).
    /// </summary>
    public VideoCardStyle Style => VideoCardStyle.History;

    #endregion

    /// <summary>
    /// Initializes a new instance of the <see cref="ReadHistoryItemViewModel"/> class.
    /// </summary>
    public ReadHistoryItemViewModel(
        ReadHistoryItem history,
        Action<ReadHistoryItemViewModel>? removeAction = null
    )
    {
        History = history;
        _removeAction = removeAction;

        if (
            !string.IsNullOrEmpty(history.CoverUrl)
            && Uri.TryCreate(history.CoverUrl, UriKind.Absolute, out Uri? coverUri)
        )
        {
            Cover = coverUri;
        }

        if (history.CurrentPositionMs > 0)
        {
            ProgressText = FormatHelper.FormatDuration(
                TimeSpan.FromMilliseconds(history.CurrentPositionMs)
            );
        }
    }

    #region 命令

    [RelayCommand]
    public void Play()
    {
        PlayerDisplayMode preferDisplayMode = ItemServices.Settings.DefaultPlayerDisplayMode;
        if (preferDisplayMode == PlayerDisplayMode.NewWindow)
        {
            OpenInNewWindow();
            return;
        }

        // 以自身 IPlayableMedia 形式进入播放页，精确续播集数与进度
        ItemServices.Navigation.NavigateToOver(typeof(VideoPlayerPage), this);
    }

    [RelayCommand]
    private void OpenInNewWindow() => new PlayerWindow().OpenPlayable(this);

    [RelayCommand]
    private async Task RemoveHistoryAsync()
    {
        try
        {
            await ItemServices.PhoenixRead.DeleteHistoryAsync(History.DramaId, History.VideoId);
            _removeAction?.Invoke(this);
        }
        catch (Exception ex)
        {
            _logger.ZLogError(ex, $"移除历史记录短剧失败");
            ItemServices.Notification.ShowInAppNotification(
                Strings.Resources.FailedToRemoveVideoFromHistory,
                NotificationType.Error
            );
        }
    }

    [RelayCommand]
    private void Pin()
    {
        string coverUri = Cover?.ToString() ?? string.Empty;
        PinItem pinItem = new(History.DramaId, History.Title, coverUri, PinType.Drama);
        ItemServices.Pinner.AddItem(pinItem);
    }

    #endregion
}

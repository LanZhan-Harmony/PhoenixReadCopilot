using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.DependencyInjection;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using PhoenixReadApi;
using PhoenixReadCopilot.Core.Contracts.Media;
using PhoenixReadCopilot.Core.Contracts.Services;
using PhoenixReadCopilot.Core.Enums;
using PhoenixReadCopilot.Core.Models.Pin;
using PhoenixReadCopilot.Forms;
using PhoenixReadCopilot.Pages.Overlay;
using ZLogger;

namespace PhoenixReadCopilot.ViewModels.Items;

/// <summary>
/// 上新日历与待播预约短剧项视图模型.
/// </summary>
internal sealed partial class DailyReleaseItemViewModel : ViewModelBase, IPlayableMedia
{
    private readonly Action<DailyReleaseItemViewModel>? _removeAction;

    private static readonly ILogger<DailyReleaseItemViewModel> _logger =
        Ioc.Default.GetRequiredService<ILogger<DailyReleaseItemViewModel>>();

    /// <summary>
    /// 上新日历原始实体.
    /// </summary>
    public DailyReleaseItem DailyRelease { get; }

    /// <summary>
    /// 短剧原始实体.
    /// </summary>
    public Drama Drama => DailyRelease.Drama;

    #region IPlayableMedia 契约实现

    /// <inheritdoc/>
    public string MediaId => Drama.Id;

    /// <inheritdoc/>
    public string Title => Drama.Title;

    /// <inheritdoc/>
    public string? CoverUrl => Drama.CoverUrl;

    /// <inheritdoc/>
    public MediaType Type => MediaType.Drama;

    /// <inheritdoc/>
    public string? InitialEpisodeId => null;

    /// <inheritdoc/>
    public long? InitialPositionMs => null;

    #endregion

    #region 视图展示属性

    /// <summary>
    /// 封面 Uri.
    /// </summary>
    public Uri? Cover { get; }

    /// <summary>
    /// 分类拼接文案.
    /// </summary>
    public string Category { get; }

    /// <summary>
    /// 是否已正式上线.
    /// </summary>
    public bool IsOnline => DailyRelease.IsOnline;

    /// <summary>
    /// 上线说明或副标题备注.
    /// </summary>
    public string? ReleaseNote => DailyRelease.Note;

    /// <summary>
    /// 上线状态展示文案.
    /// </summary>
    public string OnlineStatusText =>
        IsOnline
            ? Strings.Resources.Online
            : (!string.IsNullOrWhiteSpace(ReleaseNote) ? ReleaseNote : Strings.Resources.Upcoming);

    /// <summary>
    /// 副标题 (上线说明与集数组合文案).
    /// </summary>
    public string Subtitle { get; }

    /// <summary>
    /// 评分.
    /// </summary>
    public string? Score => Drama.Score;

    /// <summary>
    /// 是否有评分.
    /// </summary>
    public bool HasScore => !string.IsNullOrWhiteSpace(Score) && Score != "0" && Score != "0.0";

    /// <summary>
    /// 总集数.
    /// </summary>
    public int EpisodeCount => Drama.EpisodeCount;

    /// <summary>
    /// 集数文本.
    /// </summary>
    public string? EpisodeCountText => $"{EpisodeCount}集";

    /// <summary>
    /// 是否有集数.
    /// </summary>
    public bool HasEpisodeCount => EpisodeCount > 0;

    /// <summary>
    /// 题材标签列表.
    /// </summary>
    public IReadOnlyList<string> Tags => Drama.Tags ?? [];

    /// <summary>
    /// 热度.
    /// </summary>
    public string? Heat => Drama.Heat;

    /// <summary>
    /// 播放量原始文本.
    /// </summary>
    public string? Views => Drama.Views;

    /// <summary>
    /// 热度或播放量展示文本.
    /// </summary>
    public string? HeatOrPlayCountText
    {
        get
        {
            if (!string.IsNullOrWhiteSpace(Heat))
            {
                return Heat;
            }

            if (!string.IsNullOrWhiteSpace(Views))
            {
                return Views;
            }

            return null;
        }
    }

    /// <summary>
    /// 卡片呈现样式 (固定为日历样式).
    /// </summary>
    public VideoCardStyle Style => VideoCardStyle.Calendar;

    /// <summary>
    /// 是否已预约.
    /// </summary>
    [ObservableProperty]
    public partial bool IsReserved { get; set; }

    /// <summary>
    /// 是否正在操作预约.
    /// </summary>
    [ObservableProperty]
    public partial bool IsReserving { get; set; }

    /// <summary>
    /// 是否已追剧/收藏.
    /// </summary>
    [ObservableProperty]
    public partial bool IsFavorited { get; set; }

    #endregion

    public DailyReleaseItemViewModel(
        DailyReleaseItem item,
        Action<DailyReleaseItemViewModel>? removeAction = null
    )
    {
        _removeAction = removeAction;
        DailyRelease = item;

        if (
            !string.IsNullOrEmpty(item.Drama.CoverUrl)
            && Uri.TryCreate(item.Drama.CoverUrl, UriKind.Absolute, out Uri? coverUri)
        )
        {
            Cover = coverUri;
        }

        Category = item.Categories is { Count: > 0 }
            ? string.Join(" · ", item.Categories)
            : (!string.IsNullOrEmpty(item.Drama.Category) ? item.Drama.Category : string.Empty);

        string statusText = item.IsOnline
            ? Strings.Resources.Online
            : (!string.IsNullOrWhiteSpace(item.Note) ? item.Note : Strings.Resources.Upcoming);
        Subtitle = !string.IsNullOrWhiteSpace(item.Note)
            ? statusText
            : (
                item.Drama.EpisodeCount > 0
                    ? $"{statusText} · 全 {item.Drama.EpisodeCount} 集"
                    : statusText
            );

        if (!string.IsNullOrEmpty(item.Drama.Id))
        {
            IsReserved = ItemServices.Reservation.IsReserved(item.Drama.Id);
            IsFavorited = ItemServices.Favorite.IsFavorited(item.Drama.Id);
        }
    }

    #region 命令

    [RelayCommand]
    public void Play()
    {
        // 若尚未正式上线，点击卡片直接触发预约
        if (!IsOnline)
        {
            _ = ToggleReservationAsync();
            return;
        }

        if (!string.IsNullOrEmpty(Drama.Id))
        {
            _ = ItemServices.Drama.GetDramaDetailsAsync(Drama.Id);
        }

        PlayerDisplayMode preferDisplayMode = ItemServices.Settings.DefaultPlayerDisplayMode;
        if (preferDisplayMode == PlayerDisplayMode.NewWindow)
        {
            OpenInNewWindow();
            return;
        }

        ItemServices.Navigation.NavigateToOver(typeof(VideoPlayerPage), this);
    }

    [RelayCommand]
    public async Task ToggleReservationAsync()
    {
        if (IsReserving || string.IsNullOrEmpty(Drama.Id))
        {
            return;
        }

        IsReserving = true;
        try
        {
            bool targetActive = !IsReserved;
            long releaseTimeSeconds = DailyRelease.PublishTime?.ToUnixTimeSeconds() ?? 0;
            bool success = await ItemServices.Reservation.SetReservationAsync(
                Drama.Id,
                targetActive,
                title: Drama.Title,
                coverUrl: Drama.CoverUrl,
                releaseAtUnixSeconds: releaseTimeSeconds
            );

            if (success)
            {
                IsReserved = targetActive;
                string msg = targetActive
                    ? Strings.Resources.ReserveSucceed
                    : Strings.Resources.CancelReserveSucceed;
                ItemServices.Notification.ShowInAppNotification(msg, NotificationType.Success);
            }
            else
            {
                ItemServices.Notification.ShowInAppNotification(
                    Strings.Resources.ReserveFailed,
                    NotificationType.Warning
                );
            }
        }
        catch (Exception ex)
        {
            _logger.ZLogError(ex, $"操作剧集预约状态失败: {Drama.Id}");
            ItemServices.Notification.ShowInAppNotification(
                Strings.Resources.ReserveFailed,
                NotificationType.Error
            );
        }
        finally
        {
            IsReserving = false;
        }
    }

    [RelayCommand]
    private void OpenInNewWindow() => new PlayerWindow().OpenPlayable(this);

    [RelayCommand]
    private async Task AddToViewLaterAsync()
    {
        try
        {
            await ItemServices.ViewLater.AddAsync(Drama);
            ItemServices.Notification.ShowInAppNotification(
                Strings.Resources.AddViewLaterSucceed,
                NotificationType.Success
            );
        }
        catch (Exception ex)
        {
            _logger.ZLogError(ex, $"添加稍后再看失败");
            ItemServices.Notification.ShowInAppNotification(
                Strings.Resources.AddViewLaterFailed,
                NotificationType.Error
            );
        }
    }

    [RelayCommand]
    private void Pin()
    {
        string coverUri = Cover?.ToString() ?? string.Empty;
        PinItem pinItem = new(Drama.Id, Drama.Title, coverUri, PinType.Drama);
        ItemServices.Pinner.AddItem(pinItem);
    }

    #endregion
}

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
using PhoenixReadCopilot.Helpers;
using PhoenixReadCopilot.Pages.Overlay;
using ZLogger;

namespace PhoenixReadCopilot.ViewModels.Items;

/// <summary>
/// 短剧卡片项视图模型 (专用于推荐、榜单、筛选、搜索、相关推荐与追剧列表).
/// </summary>
internal partial class DramaItemViewModel : ViewModelBase, IPlayableMedia
{
    private static readonly ILogger<DramaItemViewModel> _logger = Ioc.Default.GetRequiredService<
        ILogger<DramaItemViewModel>
    >();

    /// <summary>
    /// 短剧原始数据实体.
    /// </summary>
    public Drama Drama { get; }

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
    /// 副标题 / 备注.
    /// </summary>
    public string Subtitle => Drama.Remark;

    /// <summary>
    /// 分类名称 (用于替代旧版歧义属性 Author).
    /// </summary>
    public string Category { get; }

    /// <summary>
    /// 评分 (如 "9.5").
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
    /// 是否有集数或副标题.
    /// </summary>
    public bool HasEpisodeCount => EpisodeCount > 0 || !string.IsNullOrWhiteSpace(Subtitle);

    /// <summary>
    /// 集数格式化文本.
    /// </summary>
    public string? EpisodeCountText => $"{EpisodeCount}集";

    /// <summary>
    /// 题材标签列表.
    /// </summary>
    public IReadOnlyList<string> Tags => Drama.Tags ?? [];

    /// <summary>
    /// 优先展示的标签名.
    /// </summary>
    public string? TagName => Tags.Count > 0 ? Tags[0] : Category;

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

            if (PlayCount is > 0)
            {
                return FormatHelper.FormatCount(Convert.ToInt64(PlayCount.Value));
            }

            return null;
        }
    }

    /// <summary>
    /// 播放数值.
    /// </summary>
    public double? PlayCount { get; }

    /// <summary>
    /// 卡片呈现样式.
    /// </summary>
    public VideoCardStyle Style { get; }

    /// <summary>
    /// 上线日期文本.
    /// </summary>
    public string OnlineDate => Drama.OnlineDate;

    /// <summary>
    /// 是否显示上线日期徽章（仅上新分区且存在上线日期时显示）.
    /// </summary>
    public bool ShowOnlineDate =>
        Style == VideoCardStyle.NewArrival && !string.IsNullOrWhiteSpace(OnlineDate);

    /// <summary>
    /// 是否已追剧/收藏.
    /// </summary>
    [ObservableProperty]
    public partial bool IsFavorited { get; set; }

    #endregion

    public DramaItemViewModel(Drama drama, VideoCardStyle style = VideoCardStyle.Recommend)
    {
        Drama = drama;
        Style = style;

        if (
            !string.IsNullOrEmpty(drama.CoverUrl)
            && Uri.TryCreate(drama.CoverUrl, UriKind.Absolute, out Uri? coverUri)
        )
        {
            Cover = coverUri;
        }

        Category = !string.IsNullOrEmpty(drama.Category)
            ? drama.Category
            : (drama.Credits.FirstOrDefault()?.User?.Name ?? string.Empty);

        if (double.TryParse(drama.Views, out double vc))
        {
            PlayCount = vc;
        }

        if (!string.IsNullOrEmpty(drama.Id))
        {
            IsFavorited = ItemServices.Favorite.IsFavorited(drama.Id);
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

        ItemServices.Navigation.NavigateToOver(typeof(VideoPlayerPage), this);
    }

    [RelayCommand]
    private void OpenInNewWindow() => new PlayerWindow().OpenPlayable(this);

    [RelayCommand]
    private async Task ToggleFavoriteAsync()
    {
        if (string.IsNullOrEmpty(Drama.Id))
        {
            return;
        }

        bool targetState = !IsFavorited;
        bool success = await ItemServices.Favorite.SetFavoriteAsync(Drama.Id, targetState, Drama);
        if (success)
        {
            IsFavorited = targetState;
            string msg = targetState
                ? Strings.Resources.AddFavoriteSucceed
                : Strings.Resources.CancelFavoriteSucceed;
            ItemServices.Notification.ShowInAppNotification(msg, NotificationType.Success);
        }
        else
        {
            ItemServices.Notification.ShowInAppNotification(
                Strings.Resources.FavoriteFailed,
                NotificationType.Warning
            );
        }
    }

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

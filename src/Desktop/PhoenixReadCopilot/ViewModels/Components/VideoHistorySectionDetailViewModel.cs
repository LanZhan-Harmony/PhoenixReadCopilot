using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using PhoenixReadApi;
using PhoenixReadCopilot.Contracts.ViewModels;
using PhoenixReadCopilot.Core.Contracts.Services;
using PhoenixReadCopilot.Core.Enums;
using PhoenixReadCopilot.ViewModels.Items;
using ZLogger;

namespace PhoenixReadCopilot.ViewModels.Components;

/// <summary>
/// 视频/短剧历史记录视图模型.
/// </summary>
internal sealed partial class VideoHistorySectionDetailViewModel
    : ViewModelBase,
        IHistorySectionDetailViewModel
{
    private readonly IHistoryService _historyService;
    private readonly ILogger<VideoHistorySectionDetailViewModel> _logger;

    private bool _isPreventLoadMore;
    private int _offset;

    [ObservableProperty]
    public partial bool IsEmpty { get; set; }

    [ObservableProperty]
    public partial bool IsLoading { get; set; }

    /// <summary>
    /// 列表已完成更新.
    /// </summary>
    public event EventHandler? ListUpdated;

    /// <inheritdoc/>
    public ViewHistoryTabType Type => ViewHistoryTabType.Video;

    /// <summary>
    /// 条目列表.
    /// </summary>
    public ObservableCollection<ReadHistoryItemViewModel> Items { get; } = [];

    public VideoHistorySectionDetailViewModel(
        IHistoryService historyService,
        ILogger<VideoHistorySectionDetailViewModel> logger
    )
    {
        _historyService = historyService;
        _logger = logger;
        _historyService.HistoryChanged += OnHistoryChanged;
    }

    private void OnHistoryChanged(object? sender, EventArgs e)
    {
        if (Items.Count > 0 || IsEmpty)
        {
            _ = RefreshAsync();
        }
    }

    public async Task TryFirstLoadAsync()
    {
        if (Items.Count > 0 || _isPreventLoadMore)
        {
            return;
        }

        await LoadItemsAsync();
    }

    [RelayCommand]
    public async Task RefreshAsync()
    {
        IsEmpty = false;
        _isPreventLoadMore = false;
        Items.Clear();
        _offset = 0;
        await LoadItemsAsync();
    }

    public async Task LoadItemsAsync()
    {
        if (IsEmpty || _isPreventLoadMore || IsLoading)
        {
            return;
        }

        try
        {
            IsLoading = true;
            ReadHistoryPage page = await _historyService.GetHistoryAsync(
                count: 20,
                offset: _offset
            );
            _offset = page.NextOffset;
            _isPreventLoadMore = !page.HasMore || page.Items.Count == 0;
            if (page.Items.Count > 0)
            {
                foreach (ReadHistoryItem item in page.Items)
                {
                    if (
                        Items.Any(p =>
                            p.DramaId == item.DramaId
                            && p.VideoId == item.VideoId
                        )
                    )
                    {
                        continue;
                    }

                    Items.Add(new ReadHistoryItemViewModel(item, removeAction: RemoveHistory));
                }

                ListUpdated?.Invoke(this, EventArgs.Empty);
            }
        }
        catch (Exception ex)
        {
            _isPreventLoadMore = true;
            _logger.ZLogError(ex, $"加载短剧历史记录时出错.");
        }
        finally
        {
            IsEmpty = Items.Count == 0;
            IsLoading = false;
        }
    }

    private void RemoveHistory(ReadHistoryItemViewModel vm)
    {
        Items.Remove(vm);
        IsEmpty = Items.Count == 0;
        _ = _historyService.DeleteHistoryAsync(vm.DramaId, vm.VideoId);
    }
}

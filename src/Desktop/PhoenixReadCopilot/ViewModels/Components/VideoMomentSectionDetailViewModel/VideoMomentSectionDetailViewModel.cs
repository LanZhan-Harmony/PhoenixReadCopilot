using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Dispatching;
using PhoenixReadCopilot.Core.Enums;
using PhoenixReadCopilot.Services;
using PhoenixReadCopilot.ViewModels.Items;
using PhoenixReadCopilot.ViewModels.View;
using Richasy.BiliKernel.Bili.Moment;
using WinUIKernel.Share.ViewModels;

namespace PhoenixReadCopilot.ViewModels.Components;

/// <summary>
/// 视频动态分区详情视图模型.
/// </summary>
public sealed partial class VideoMomentSectionDetailViewModel
    : ViewModelBase,
        IMomentSectionDetailViewModel
{
    /// <summary>
    /// Initializes a new instance of the <see cref="VideoMomentSectionDetailViewModel"/> class.
    /// </summary>
    public VideoMomentSectionDetailViewModel(
        IMomentDiscoveryService service,
        DispatcherQueue dispatcherQueue,
        MomentPageViewModel momentPageViewModel,
        ISettingsService settingsService,
        ILogger<VideoMomentSectionDetailViewModel> logger
    )
    {
        _service = service;
        _dispatcherQueue = dispatcherQueue;
        _momentPageViewModel = momentPageViewModel;
        _settings = settingsService.Settings;
        _logger = logger;
    }

    [RelayCommand]
    private async Task InitializeAsync()
    {
        if (Items.Count > 0 || _preventLoadMore)
        {
            return;
        }

        await LoadItemsAsync();
        SaveLastTenMomentIds();
    }

    [RelayCommand]
    private void Refresh()
    {
        _offset = default;
        _baseline = default;
        IsEmpty = false;
        _preventLoadMore = false;
        Items.Clear();
        _dispatcherQueue.TryEnqueue(
            DispatcherQueuePriority.Low,
            async () =>
            {
                await LoadItemsAsync();
                SaveLastTenMomentIds();
            }
        );
    }

    [RelayCommand]
    private async Task LoadItemsAsync()
    {
        if (IsEmpty || IsLoading || _preventLoadMore)
        {
            return;
        }

        try
        {
            IsLoading = true;
            var view = await _service.GetVideoMomentsAsync(_offset, _baseline);
            if (view is not null)
            {
                _offset = view.Offset;
                _baseline = view.UpdateBaseline;
                _preventLoadMore = view.HasMoreMoments != true;
                foreach (
                    var item in view.Moments.Select(p => new MomentItemViewModel(
                        p,
                        MomentCardStyle.Video,
                        ShowComment
                    ))
                )
                {
                    Items.Add(item);
                }

                ListUpdated?.Invoke(this, EventArgs.Empty);
            }
        }
        catch (Exception ex)
        {
            _preventLoadMore = true;
            _logger.LogError(ex, "加载动态列表失败.");
        }
        finally
        {
            IsEmpty = Items.Count == 0;
            IsLoading = false;
        }
    }

    private void ShowComment(MomentItemViewModel item) =>
        _momentPageViewModel.ShowCommentCommand.Execute(item.Data);

    private void SaveLastTenMomentIds()
    {
        var momentIds = Items.Take(10).Select(p => p.Data.Id).ToList();
        var lastTenMomentIds = string.Join(',', momentIds);
        _settings.LastTenMomentIds = lastTenMomentIds;
    }
}

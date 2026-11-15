using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Dispatching;
using PhoenixReadCopilot.Core.Contracts.Services;
using PhoenixReadCopilot.Core.Enums;
using PhoenixReadCopilot.Core.Models.Settings;
using PhoenixReadCopilot.ViewModels.Items;
using PhoenixReadCopilot.ViewModels.View;
using WinUIKernel.Share.ViewModels;
using ZLogger;

namespace PhoenixReadCopilot.ViewModels.Components;

/// <summary>
/// 视频动态分区详情视图模型.
/// </summary>
public sealed partial class VideoMomentSectionDetailViewModel
    : ViewModelBase,
        IMomentSectionDetailViewModel
{
    private readonly ILogger<VideoMomentSectionDetailViewModel> _logger;
    private readonly DispatcherQueue _dispatcherQueue;
    private readonly MomentPageViewModel _momentPageViewModel;
    private readonly AppSettings _settings;

    private bool _preventLoadMore;
    private string? _offset;
    private string? _baseline;

    [ObservableProperty]
    public partial bool IsEmpty { get; set; }

    [ObservableProperty]
    public partial bool IsLoading { get; set; }

    /// <summary>
    /// 列表已完成更新.
    /// </summary>
    public event EventHandler? ListUpdated;

    /// <inheritdoc/>
    public MomentSectionType SectionType => MomentSectionType.Video;

    /// <summary>
    /// 条目列表.
    /// </summary>
    public ObservableCollection<MomentItemViewModel> Items { get; } = [];

    /// <summary>
    /// Initializes a new instance of the <see cref="VideoMomentSectionDetailViewModel"/> class.
    /// </summary>
    public VideoMomentSectionDetailViewModel(
        DispatcherQueue dispatcherQueue,
        MomentPageViewModel momentPageViewModel,
        ISettingsService settingsService,
        ILogger<VideoMomentSectionDetailViewModel> logger
    )
    {
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
    }

    [RelayCommand]
    private void Refresh()
    {
        _offset = null;
        _baseline = null;
        IsEmpty = false;
        _preventLoadMore = false;
        Items.Clear();
        _dispatcherQueue.TryEnqueue(DispatcherQueuePriority.Low,
            async () =>
            {
                await LoadItemsAsync();
            });
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
            // TODO: 红果动态广场 API 暂未开放
            await Task.CompletedTask;
            _preventLoadMore = true;
        }
        catch (Exception ex)
        {
            _preventLoadMore = true;
            _logger.ZLogError(ex, $"加载动态列表失败.");
        }
        finally
        {
            IsEmpty = Items.Count == 0;
            IsLoading = false;
        }
    }

    private void ShowComment(MomentItemViewModel item) =>
        _momentPageViewModel.ShowCommentCommand.Execute(item.Data);
}

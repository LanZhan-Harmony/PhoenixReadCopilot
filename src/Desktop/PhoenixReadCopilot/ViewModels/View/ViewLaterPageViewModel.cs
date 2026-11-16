using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Dispatching;
using PhoenixReadApi;
using PhoenixReadCopilot.Core.Contracts.Services;
using PhoenixReadCopilot.Core.Enums;
using PhoenixReadCopilot.Core.Models.Settings;
using PhoenixReadCopilot.Services;
using PhoenixReadCopilot.ViewModels.Core;
using PhoenixReadCopilot.ViewModels.Items;
using WinUIKernel.Share.ViewModels;
using ZLogger;

namespace PhoenixReadCopilot.ViewModels.View;

/// <summary>
/// 稍后再看视图模型.
/// </summary>
public sealed partial class ViewLaterPageViewModel : ViewModelBase
{
    private readonly ILocalViewLaterService _localService;
    private readonly ILogger<ViewLaterPageViewModel> _logger;
    private readonly NavigationViewModel _navigationViewModel;
    private readonly AppSettings _settings;
    private readonly DispatcherQueue _dispatcherQueue;

    [ObservableProperty]
    public partial bool IsLoading { get; set; }

    [ObservableProperty]
    public partial bool IsEmpty { get; set; }

    [ObservableProperty]
    public partial int TotalCount { get; set; }

    /// <summary>
    /// 视频列表.
    /// </summary>
    public ObservableCollection<VideoItemViewModel> Videos { get; } = [];

    /// <summary>
    /// Initializes a new instance of the <see cref="ViewLaterPageViewModel"/> class.
    /// </summary>
    public ViewLaterPageViewModel(
        ILocalViewLaterService localService,
        ILogger<ViewLaterPageViewModel> logger,
        NavigationViewModel navigationViewModel,
        ISettingsService settingsService,
        DispatcherQueue dispatcherQueue
    )
    {
        _localService = localService;
        _logger = logger;
        _navigationViewModel = navigationViewModel;
        _settings = settingsService.Settings;
        _dispatcherQueue = dispatcherQueue;

        _localService.ViewLaterChanged += OnViewLaterChanged;
    }

    private void OnViewLaterChanged(object? sender, EventArgs e)
    {
        _dispatcherQueue.TryEnqueue(() =>
        {
            if (Videos.Count > 0 || IsEmpty)
            {
                _ = RefreshAsync();
            }
        });
    }

    public async Task InitializeAsync()
    {
        if (Videos.Count > 0 || IsEmpty)
        {
            return;
        }

        await LoadVideosAsync();
    }

    [RelayCommand]
    private async Task RefreshAsync()
    {
        await LoadVideosAsync();
    }

    public async Task LoadVideosAsync()
    {
        if (IsLoading)
        {
            return;
        }

        IsLoading = true;
        try
        {
            IReadOnlyList<Drama> dramas = await _localService.GetAllAsync();

            // 如果已有项且ID序列完全一致，无需清空重建避免卡片闪烁
            if (Videos.Count == dramas.Count && Videos.Select(v => v.Drama?.Id).SequenceEqual(dramas.Select(d => d.Id)))
            {
                TotalCount = Videos.Count;
                IsEmpty = Videos.Count == 0;
                return;
            }

            Videos.Clear();
            foreach (Drama item in dramas)
            {
                Videos.Add(
                    new VideoItemViewModel(
                        item,
                        VideoCardStyle.ViewLater,
                        removeAction: RemoveVideo
                    )
                );
            }

            TotalCount = Videos.Count;
            IsEmpty = Videos.Count == 0;
        }
        catch (Exception ex)
        {
            _logger.ZLogError(ex, $"尝试加载稍后再看时出现异常");
        }
        finally
        {
            IsLoading = false;
            IsEmpty = Videos.Count == 0;
        }
    }

    [RelayCommand]
    private async Task ClearAllAsync()
    {
        try
        {
            await _localService.ClearAsync();
            Videos.Clear();
            TotalCount = 0;
            IsEmpty = true;
        }
        catch (Exception ex)
        {
            _logger.ZLogError(ex, $"清除稍后再看时出错");
        }
    }

    [RelayCommand]
    private void PlayAll()
    {
        if (Videos.Count == 0)
        {
            return;
        }

        VideoItemViewModel first = Videos.First();
        first.Play();
    }

    private void RemoveVideo(VideoItemViewModel item)
    {
        Videos.Remove(item);
        if (item.Drama is not null)
        {
            _ = _localService.RemoveAsync(item.Drama.Id);
        }
        TotalCount = Videos.Count;
        IsEmpty = Videos.Count == 0;
    }
}

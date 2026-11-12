using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Dispatching;
using PhoenixReadCopilot.ViewModels.Items;
using WinUIKernel.Share.ViewModels;

namespace PhoenixReadCopilot.ViewModels.View;

/// <summary>
/// 粉丝页面视图模型.
/// </summary>
public sealed partial class FansPageViewModel : ViewModelBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="FansPageViewModel"/> class.
    /// </summary>
    public FansPageViewModel(
        ILogger<FansPageViewModel> logger,
        DispatcherQueue dispatcherQueue
    )
    {
        _logger = logger;
        _dispatcherQueue = dispatcherQueue;
    }

    [RelayCommand]
    private async Task InitializeAsync()
    {
        if (Users.Count > 0 || IsEmpty)
        {
            return;
        }

        await LoadUsersAsync();
    }

    [RelayCommand]
    private void Refresh()
    {
        IsEmpty = false;
        TotalCount = 0;
        _dispatcherQueue.TryEnqueue(
            DispatcherQueuePriority.Low,
            async () =>
            {
                Users.Clear();
                await InitializeAsync();
            }
        );
    }

    [RelayCommand]
    private async Task LoadUsersAsync()
    {
        if (IsUserLoading || IsEmpty)
        {
            return;
        }

        IsUserLoading = true;
        try
        {
            // TODO: 红果粉丝列表 API 暂未开放
            await Task.CompletedTask;
            IsEmpty = true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "加载粉丝列表失败");
            IsEmpty = true;
        }
        finally
        {
            IsUserLoading = false;
        }
    }
}

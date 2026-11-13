using CommunityToolkit.Mvvm.Input;
using Microsoft.UI.Dispatching;
using PhoenixReadCopilot.Core.Models.Moment;
using PhoenixReadCopilot.Strings;
using WinUIKernel.Share.ViewModels;
using ZLogger;

namespace PhoenixReadCopilot.ViewModels.Items;

/// <summary>
/// 动态点赞者条目视图模型.
/// </summary>
public sealed partial class MomentUperSectionViewModel : ViewModelBase<MomentProfile>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MomentUperSectionViewModel"/> class.
    /// </summary>
    public MomentUperSectionViewModel(
        MomentProfile? data,
        Action<MomentInformation>? showCommentAction = null
    )
        : base(data)
    {
        _showCommentAction = showCommentAction;
        IsTotal = data is null;
        Avatar = data?.User?.Avatar?.Uri;
        Name = data?.User?.Name ?? Resources.AllDynamic;
        HasUnread = data?.IsUnread ?? false;
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
        if (IsTotal)
        {
            throw new Exception("请在外层视图模型中刷新综合动态，以便更新 UP 列表");
        }

        _offset = null;
        _baseline = null;
        IsEmpty = false;
        _preventLoadMore = false;
        Items.Clear();

        ItemServices.DispatcherQueue.TryEnqueue(DispatcherQueuePriority.Low, async () =>
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
            if (IsTotal)
            {
                await LoadComprehensiveItemsAsync();
            }
            else
            {
                await LoadUperItemsAsync();
                if (HasUnread)
                {
                    HasUnread = false;
                }
            }
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

    private async Task LoadComprehensiveItemsAsync()
    {
        // TODO: 红果动态广场 API 暂未开放
        await Task.CompletedTask;
    }

    private async Task LoadUperItemsAsync()
    {
        // TODO: 红果用户动态 API 暂未开放
        await Task.CompletedTask;
    }

    private void ShowComment(MomentItemViewModel item) => _showCommentAction?.Invoke(item.Data);
}

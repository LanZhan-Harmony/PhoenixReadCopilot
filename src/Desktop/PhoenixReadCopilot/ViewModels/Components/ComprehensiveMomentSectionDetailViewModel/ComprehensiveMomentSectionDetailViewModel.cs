using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Dispatching;
using PhoenixReadCopilot.Services;
using PhoenixReadCopilot.ViewModels.Items;
using PhoenixReadCopilot.ViewModels.View;
using Richasy.BiliKernel.Bili.Moment;
using Richasy.BiliKernel.Models.Moment;

namespace PhoenixReadCopilot.ViewModels.Components;

/// <summary>
/// 综合动态分区详情视图模型.
/// </summary>
public sealed partial class ComprehensiveMomentSectionDetailViewModel
    : LayoutPageViewModelBase,
        IMomentSectionDetailViewModel
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ComprehensiveMomentSectionDetailViewModel"/> class.
    /// </summary>
    public ComprehensiveMomentSectionDetailViewModel(
        IMomentDiscoveryService service,
        DispatcherQueue dispatcherQueue,
        MomentPageViewModel momentPageViewModel,
        ILogger<ComprehensiveMomentSectionDetailViewModel> logger,
        ILogger<MomentUperSectionViewModel> uperLogger,
        ISettingsService settingsService
    )
        : base(settingsService)
    {
        _service = service;
        _dispatcherQueue = dispatcherQueue;
        _momentPageViewModel = momentPageViewModel;
        _logger = logger;
        _uperLogger = uperLogger;
    }

    /// <inheritdoc/>
    protected override string GetPageKey() => "ComprehensiveMoment";

    [RelayCommand]
    private async Task InitializeAsync()
    {
        if (Upers.Count > 0)
        {
            return;
        }

        IsLoading = true;
        Upers.Add(
            new MomentUperSectionViewModel(
                null,
                data => _momentPageViewModel.ShowCommentCommand.Execute(data)
            )
        );
        await LoadUpersAsync();
        IsLoading = false;
        SelectUper(Upers.First());

        Initialized?.Invoke(this, EventArgs.Empty);
    }

    [RelayCommand]
    private void Refresh()
    {
        if (SelectedUper.IsTotal)
        {
            Upers.Clear();
            _dispatcherQueue.TryEnqueue(
                DispatcherQueuePriority.Low,
                async () =>
                {
                    SelectedUper?.Items?.Clear();
                    SelectedUper = null;
                    await InitializeAsync();
                }
            );
        }
        else
        {
            SelectedUper.RefreshCommand.Execute(null);
        }
    }

    [RelayCommand]
    private void SelectUper(MomentUperSectionViewModel item)
    {
        if (item == null)
        {
            return;
        }

        SelectedUper = item;
        SelectedUper.InitializeCommand.Execute(null);
    }

    private async Task LoadUpersAsync()
    {
        try
        {
            MomentView response = await _service.GetComprehensiveMomentsAsync();
            IReadOnlyList<MomentProfile>? upers = response.Users;
            if (upers?.Count > 0)
            {
                foreach (MomentProfile item in upers)
                {
                    Upers.Add(
                        new MomentUperSectionViewModel(
                            item,
                            data => _momentPageViewModel.ShowCommentCommand.Execute(data)
                        )
                    );
                }
            }

            MomentUperSectionViewModel total = Upers.First();
            total.InjectFirstPageData(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "尝试获取综合动态时发生错误.");
        }
    }
}

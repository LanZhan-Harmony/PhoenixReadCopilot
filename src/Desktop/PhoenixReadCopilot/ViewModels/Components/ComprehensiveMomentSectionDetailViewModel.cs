using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Dispatching;
using PhoenixReadCopilot.Core.Contracts.Services;
using PhoenixReadCopilot.Core.Enums;
using PhoenixReadCopilot.ViewModels.Items;
using PhoenixReadCopilot.ViewModels.View;

namespace PhoenixReadCopilot.ViewModels.Components;

/// <summary>
/// 综合动态分区详情视图模型.
/// </summary>
public sealed partial class ComprehensiveMomentSectionDetailViewModel
    : LayoutPageViewModelBase,
        IMomentSectionDetailViewModel
{
    private readonly DispatcherQueue _dispatcherQueue;
    private readonly MomentPageViewModel _momentPageViewModel;
    private readonly ILogger<ComprehensiveMomentSectionDetailViewModel> _logger;
    private readonly ILogger<MomentUperSectionViewModel> _uperLogger;

    [ObservableProperty]
    public partial bool IsEmpty { get; set; }

    [ObservableProperty]
    public partial bool IsLoading { get; set; }

    [ObservableProperty]
    public partial MomentUperSectionViewModel? SelectedUper { get; set; }

    /// <summary>
    /// 已经初始化完成.
    /// </summary>
    public event EventHandler? Initialized;

    /// <inheritdoc/>
    public MomentSectionType SectionType => MomentSectionType.Comprehensive;

    /// <summary>
    /// 推荐的用户列表.
    /// </summary>
    public ObservableCollection<MomentUperSectionViewModel> Upers { get; } = [];

    /// <summary>
    /// Initializes a new instance of the <see cref="ComprehensiveMomentSectionDetailViewModel"/> class.
    /// </summary>
    public ComprehensiveMomentSectionDetailViewModel(
        DispatcherQueue dispatcherQueue,
        MomentPageViewModel momentPageViewModel,
        ILogger<ComprehensiveMomentSectionDetailViewModel> logger,
        ILogger<MomentUperSectionViewModel> uperLogger,
        ISettingsService settingsService
    )
        : base(settingsService)
    {
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
            new MomentUperSectionViewModel(null,
                data =>
                    _momentPageViewModel.ShowCommentCommand.Execute(data)
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
        if (SelectedUper is not null && SelectedUper.IsTotal)
        {
            Upers.Clear();
            _dispatcherQueue.TryEnqueue(DispatcherQueuePriority.Low,
                async () =>
                {
                    SelectedUper?.Items?.Clear();
                    SelectedUper = null;
                    await InitializeAsync();
                });
        }
        else
        {
            SelectedUper?.RefreshCommand.Execute(null);
        }
    }

    [RelayCommand]
    private void SelectUper(MomentUperSectionViewModel item)
    {
        if (item is null)
        {
            return;
        }

        SelectedUper = item;
        SelectedUper.InitializeCommand.Execute(null);
    }

    private async Task LoadUpersAsync()
    {
        // TODO: 红果动态广场 API 暂未开放
        await Task.CompletedTask;
    }
}

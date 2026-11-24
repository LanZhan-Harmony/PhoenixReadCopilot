using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Dispatching;
using PhoenixReadCopilot.Core.Contracts.Services;
using PhoenixReadCopilot.Core.Models.Pin;
using PhoenixReadCopilot.Strings;

namespace PhoenixReadCopilot.ViewModels.Core;

/// <summary>
/// 固定组件视图模型.
/// </summary>
internal sealed partial class PinnerViewModel : ViewModelBase
{
    private readonly IPinService _pinService;
    private readonly INotificationService _notificationService;
    private readonly DispatcherQueue _dispatcherQueue;
    private readonly ILogger<PinnerViewModel> _logger;

    [ObservableProperty]
    public partial bool IsEmpty { get; set; }

    [ObservableProperty]
    public partial bool IsLoading { get; set; }

    /// <summary>
    /// 条目列表.
    /// </summary>
    public ObservableCollection<PinItem> Items { get; } = [];

    public PinnerViewModel(
        IPinService pinService,
        INotificationService notificationService,
        DispatcherQueue dispatcherQueue,
        ILogger<PinnerViewModel> logger
    )
    {
        _pinService = pinService;
        _notificationService = notificationService;
        _dispatcherQueue = dispatcherQueue;
        _logger = logger;

        _pinService.PinChanged += OnPinChanged;
    }

    private void OnPinChanged(object? sender, EventArgs e)
    {
        _dispatcherQueue.TryEnqueue(async () =>
        {
            await LoadItemsAsync();
        });
    }

    public async Task InitializeAsync()
    {
        IsLoading = true;
        try
        {
            await LoadItemsAsync();
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task LoadItemsAsync()
    {
        IReadOnlyList<PinItem> data = await _pinService.GetPinnedItemsAsync();
        Items.Clear();
        foreach (PinItem item in data)
        {
            Items.Add(item);
        }
        IsEmpty = Items.Count == 0;
    }

    public void AddItem(PinItem item) => _ = AddItemAsync(item);

    public async Task AddItemAsync(PinItem item)
    {
        bool success = await _pinService.PinAsync(item);
        if (success)
        {
            _notificationService.ShowInAppNotification(Resources.Fixed, NotificationType.Success);
        }
        else
        {
            _notificationService.ShowInAppNotification(
                Resources.AlreadyPinned,
                NotificationType.Warning
            );
        }
    }

    public void RemoveItem(PinItem item) => _ = RemoveItemAsync(item);

    public async Task RemoveItemAsync(PinItem item)
    {
        await _pinService.UnpinAsync(item);
    }
}

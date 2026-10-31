using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using PhoenixReadCopilot.UI.Models;
using PhoenixReadCopilot.UI.Strings;
using Richasy.BiliKernel.Bili.Authorization;
using WinUIKernel.Share.Base;
using WinUIKernel.Share.Toolkits;
using WinUIKernel.Share.ViewModels;

namespace PhoenixReadCopilot.UI.ViewModels.Core;

/// <summary>
/// 固定组件视图模型.
/// </summary>
public sealed partial class PinnerViewModel : ViewModelBase
{
    private const string PinFileName = "User-{0}.json";
    private const string PinFolderName = "Fixed";
    private readonly ILogger<PinnerViewModel> _logger;

    [ObservableProperty]
    public partial bool IsEmpty { get; set; }

    [ObservableProperty]
    public partial bool IsLoading { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="PinnerViewModel"/> class.
    /// </summary>
    public PinnerViewModel(ILogger<PinnerViewModel> logger) => _logger = logger;

    /// <summary>
    /// 条目列表.
    /// </summary>
    public ObservableCollection<PinItem> Items { get; } = new();

    [RelayCommand]
    private async Task InitializeAsync()
    {
        var currentUserId = this.Get<IBiliTokenResolver>().GetToken().UserId;
        var data = await this.Get<IFileToolkit>()
            .ReadLocalDataAsync(
                string.Format(PinFileName, currentUserId),
                GlobalSerializeContext.Default.ListPinItem,
                "[]",
                PinFolderName
            );
        Items.Clear();
        if (data.Count > 0)
        {
            data.ForEach(Items.Add);
        }

        IsEmpty = Items.Count == 0;
    }

    [RelayCommand]
    private async Task AddItemAsync(PinItem item)
    {
        if (Items.Contains(item))
        {
            this.Get<AppViewModel>()
                .ShowTipCommand.Execute((Resources.AlreadyPinned, InfoType.Warning));
            return;
        }

        Items.Add(item);
        await SaveDataAsync();
        this.Get<AppViewModel>().ShowTipCommand.Execute((Resources.Fixed, InfoType.Success));
    }

    [RelayCommand]
    private async Task RemoveItemAsync(PinItem item)
    {
        Items.Remove(item);
        await SaveDataAsync();
    }

    private async Task SaveDataAsync()
    {
        await this.Get<IFileToolkit>()
            .WriteLocalDataAsync(
                string.Format(PinFileName, this.Get<IBiliTokenResolver>().GetToken().UserId),
                Items.ToList(),
                GlobalSerializeContext.Default.ListPinItem,
                PinFolderName
            );
        IsEmpty = Items.Count == 0;
    }
}

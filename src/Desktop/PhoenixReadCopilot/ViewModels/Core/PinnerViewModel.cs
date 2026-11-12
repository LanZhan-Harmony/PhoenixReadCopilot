using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using PhoenixReadCopilot.Core.Models.Pin;
using PhoenixReadCopilot.Strings;
using PhoenixReadCopilot.ViewModels.Components;
using WinUIKernel.Share.Base;
using WinUIKernel.Share.Toolkits;
using WinUIKernel.Share.ViewModels;

namespace PhoenixReadCopilot.ViewModels.Core;

/// <summary>
/// 固定组件视图模型.
/// </summary>
public sealed partial class PinnerViewModel : ViewModelBase
{
    private readonly IFileToolkit _fileToolkit;
    private readonly AppViewModel _appViewModel;
    private readonly AccountViewModel _accountViewModel;
    private readonly ILogger<PinnerViewModel> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="PinnerViewModel"/> class.
    /// </summary>
    public PinnerViewModel(
        IFileToolkit fileToolkit,
        AppViewModel appViewModel,
        AccountViewModel accountViewModel,
        ILogger<PinnerViewModel> logger
    )
    {
        _fileToolkit = fileToolkit;
        _appViewModel = appViewModel;
        _accountViewModel = accountViewModel;
        _logger = logger;
    }

    private const string PinFileName = "User-{0}.json";
    private const string PinFolderName = "Fixed";

    [ObservableProperty]
    public partial bool IsEmpty { get; set; }

    [ObservableProperty]
    public partial bool IsLoading { get; set; }

    /// <summary>
    /// 条目列表.
    /// </summary>
    public ObservableCollection<PinItem> Items { get; } = [];

    [RelayCommand]
    private async Task InitializeAsync()
    {
        string currentUserId = _accountViewModel.MyProfile?.User?.Id ?? "default";
        List<PinItem> data = await _fileToolkit.ReadLocalDataAsync<List<PinItem>>(
            string.Format(PinFileName, currentUserId),
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
            _appViewModel.ShowTipCommand.Execute((Resources.AlreadyPinned, InfoType.Warning));
            return;
        }

        Items.Add(item);
        await SaveDataAsync();
        _appViewModel.ShowTipCommand.Execute((Resources.Fixed, InfoType.Success));
    }

    [RelayCommand]
    private async Task RemoveItemAsync(PinItem item)
    {
        Items.Remove(item);
        await SaveDataAsync();
    }

    private async Task SaveDataAsync()
    {
        string currentUserId = _accountViewModel.MyProfile?.User?.Id ?? "default";
        await _fileToolkit.WriteLocalDataAsync<List<PinItem>>(
            string.Format(PinFileName, currentUserId),
            [.. Items],
            PinFolderName
        );
        IsEmpty = Items.Count == 0;
    }
}

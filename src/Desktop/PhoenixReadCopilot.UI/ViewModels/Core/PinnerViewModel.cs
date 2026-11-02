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
    private readonly IBiliTokenResolver _tokenResolver;
    private readonly IFileToolkit _fileToolkit;
    private readonly AppViewModel _appViewModel;
    private readonly ILogger<PinnerViewModel> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="PinnerViewModel"/> class.
    /// </summary>
    public PinnerViewModel(
        IBiliTokenResolver tokenResolver,
        IFileToolkit fileToolkit,
        AppViewModel appViewModel,
        ILogger<PinnerViewModel> logger
    )
    {
        _tokenResolver = tokenResolver;
        _fileToolkit = fileToolkit;
        _appViewModel = appViewModel;
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
    public ObservableCollection<PinItem> Items { get; } = new();

    [RelayCommand]
    private async Task InitializeAsync()
    {
        var currentUserId = _tokenResolver.GetToken().UserId;
        var data = await _fileToolkit.ReadLocalDataAsync(
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
        await _fileToolkit.WriteLocalDataAsync(
            string.Format(PinFileName, _tokenResolver.GetToken().UserId),
            Items.ToList(),
            GlobalSerializeContext.Default.ListPinItem,
            PinFolderName
        );
        IsEmpty = Items.Count == 0;
    }
}

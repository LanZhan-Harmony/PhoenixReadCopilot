using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using PhoenixReadCopilot.Core.Contracts.Services;
using PhoenixReadCopilot.Core.Models.Pin;
using PhoenixReadCopilot.Helpers;
using PhoenixReadCopilot.Strings;
using PhoenixReadCopilot.ViewModels.Components;
using WinUIKernel.Share.Base;
using WinUIKernel.Share.ViewModels;
using ZLogger;
using PhoenixReadCopilot.Core.Helpers;

namespace PhoenixReadCopilot.ViewModels.Core;

/// <summary>
/// 固定组件视图模型.
/// </summary>
public sealed partial class PinnerViewModel : ViewModelBase
{
    private readonly IPathService _pathService;
    private readonly AppViewModel _appViewModel;
    private readonly AccountViewModel _accountViewModel;
    private readonly ILogger<PinnerViewModel> _logger;

    private string _filePath;

    [ObservableProperty]
    public partial bool IsEmpty { get; set; }

    [ObservableProperty]
    public partial bool IsLoading { get; set; }

    /// <summary>
    /// 条目列表.
    /// </summary>
    public ObservableCollection<PinItem> Items { get; } = [];

    /// <summary>
    /// Initializes a new instance of the <see cref="PinnerViewModel"/> class.
    /// </summary>
    public PinnerViewModel(
        IPathService pathService,
        AppViewModel appViewModel,
        AccountViewModel accountViewModel,
        ILogger<PinnerViewModel> logger
    )
    {
        _appViewModel = appViewModel;
        _accountViewModel = accountViewModel;
        _logger = logger;
        _pathService = pathService;
    }

    [RelayCommand]
    private async Task InitializeAsync()
    {
        string currentUserId = _accountViewModel.MyProfile?.User?.Id ?? "default";
        _filePath = Path.Combine(_pathService.DataFolderPath, "Pin", $"pin_{currentUserId}.json");
        List<PinItem> data = await ReadDataAsync();
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

    private async Task<List<PinItem>> ReadDataAsync()
    {
        if (File.Exists(_filePath))
        {
            try
            {
                string json = await File.ReadAllTextAsync(_filePath);
                return JsonAotSerializer.Deserialize<List<PinItem>>(json) ?? [];
            }
            catch (Exception ex)
            {
                _logger.ZLogWarning(ex, $"读取固定组件缓存文件失败，将重新初始化.");
                return [];
            }
        }
        else
        {
            return [];
        }
    }

    private async Task SaveDataAsync()
    {
        try
        {
            string json = JsonAotSerializer.Serialize(Items);
            string targetFolder = Path.Combine(_pathService.DataFolderPath, "Pin");
            Directory.CreateDirectory(targetFolder);
            await File.WriteAllTextAsync(_filePath, json).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.ZLogError(ex, $"保存固定组件缓存文件失败.");
        }
        finally
        {
            IsEmpty = Items.Count == 0;
        }
    }
}

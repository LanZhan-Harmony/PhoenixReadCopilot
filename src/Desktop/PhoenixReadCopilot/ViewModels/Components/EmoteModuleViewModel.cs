using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using Richasy.BiliKernel.Models.Appearance;
using WinUIKernel.Share.ViewModels;

namespace PhoenixReadCopilot.ViewModels.Components;

/// <summary>
/// 表情模块视图模型.
/// </summary>
public sealed partial class EmoteModuleViewModel : ViewModelBase
{
    private readonly ILogger<EmoteModuleViewModel> _logger;

    [ObservableProperty]
    public partial bool IsLoading { get; set; }

    [ObservableProperty]
    public partial List<EmotePackage>? Packages { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="EmoteModuleViewModel"/> class.
    /// </summary>
    public EmoteModuleViewModel(ILogger<EmoteModuleViewModel> logger)
    {
        _logger = logger;
    }

    [RelayCommand]
    private async Task InitializeAsync()
    {
        if (IsLoading || Packages is not null)
        {
            return;
        }

        await Task.CompletedTask;
        Packages = [];
    }
}

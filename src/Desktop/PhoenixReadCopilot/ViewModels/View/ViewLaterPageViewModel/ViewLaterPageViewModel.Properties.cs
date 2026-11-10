using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.Logging;
using PhoenixReadCopilot.Models.Settings;
using PhoenixReadCopilot.Services;
using PhoenixReadCopilot.ViewModels.Core;
using PhoenixReadCopilot.ViewModels.Items;

namespace PhoenixReadCopilot.ViewModels.View;

/// <summary>
/// 稍后再看视图模型.
/// </summary>
public sealed partial class ViewLaterPageViewModel
{
    private readonly ILocalViewLaterService _localService;
    private readonly ILogger<ViewLaterPageViewModel> _logger;
    private readonly NavigationViewModel _navigationViewModel;
    private readonly AppSettings _settings;

    [ObservableProperty]
    public partial bool IsLoading { get; set; }

    [ObservableProperty]
    public partial bool IsEmpty { get; set; }

    [ObservableProperty]
    public partial int TotalCount { get; set; }

    /// <summary>
    /// 视频列表已完成更新.
    /// </summary>
    public event EventHandler? ListUpdated;

    /// <summary>
    /// 视频列表.
    /// </summary>
    public ObservableCollection<VideoItemViewModel> Videos { get; } = [];
}

using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.Logging;
using PhoenixReadCopilot.UI.ViewModels.Items;
using Richasy.BiliKernel.Bili.Search;

namespace PhoenixReadCopilot.UI.ViewModels.View;

/// <summary>
/// 搜索页面视图模型.
/// </summary>
public sealed partial class SearchPageViewModel
{
    private readonly ISearchService _service;
    private readonly ILogger<SearchPageViewModel> _logger;

    [ObservableProperty]
    public partial string Keyword { get; set; }

    [ObservableProperty]
    public partial bool IsSearching { get; set; }

    [ObservableProperty]
    public partial ISearchSectionDetailViewModel SelectedSection { get; set; }

    /// <summary>
    /// 详情分区列表.
    /// </summary>
    [ObservableProperty]
    public partial List<ISearchSectionDetailViewModel>? Sections { get; set; }

    /// <summary>
    /// 分区初始化完成.
    /// </summary>
    public event EventHandler SectionInitialized;
}

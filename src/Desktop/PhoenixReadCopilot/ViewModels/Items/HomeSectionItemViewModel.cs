using System.Collections.ObjectModel;
using PhoenixReadCopilot.Core.Enums;
using PhoenixReadCopilot.ViewModels.View;

namespace PhoenixReadCopilot.ViewModels.Items;

/// <summary>
/// 首页分区内容项视图模型.
/// </summary>
internal sealed class HomeSectionItemViewModel(
    HomeSectionType sectionType,
    string title,
    HomePageViewModel pageViewModel
) : ViewModelBase
{
    /// <summary>
    /// 分区类型.
    /// </summary>
    public HomeSectionType SectionType { get; } = sectionType;

    /// <summary>
    /// 分区标题.
    /// </summary>
    public string Title { get; } = title;

    /// <summary>
    /// 所属首页视图模型.
    /// </summary>
    public HomePageViewModel PageViewModel { get; } = pageViewModel;

    /// <summary>
    /// 推荐短剧列表.
    /// </summary>
    public ObservableCollection<DramaItemViewModel> RecommendVideos => PageViewModel.RecommendVideos;

    /// <summary>
    /// 上新短剧列表.
    /// </summary>
    public ObservableCollection<DramaItemViewModel> NewArrivalVideos => PageViewModel.NewArrivalVideos;
}

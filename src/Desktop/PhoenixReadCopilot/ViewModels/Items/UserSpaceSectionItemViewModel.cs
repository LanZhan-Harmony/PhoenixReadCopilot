using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using FluentIcons.Common;
using WinUIKernel.Share.ViewModels;

namespace PhoenixReadCopilot.ViewModels.Items;

/// <summary>
/// 用户空间主体内容展示条目接口（统一 Section 与 SubTab，支持 DataTemplateSelector 纯 MVVM 自动识别）.
/// </summary>
internal interface IUserSpaceContentItem
{
    /// <summary>
    /// 所属页面视图模型.
    /// </summary>
    View.UserSpacePageViewModel? PageViewModel { get; }
}

/// <summary>
/// 用户空间功能区 Section 条目模型.
/// </summary>
internal sealed partial class UserSpaceSectionItemViewModel : ViewModelBase, IUserSpaceContentItem
{
    /// <summary>
    /// Section 唯一标识（例如 Works, Collections, Reviews, History, Favorite, ViewLater, Likes, Reservation, Circle, Follows, Fans）.
    /// </summary>
    public string Id { get; init; }

    /// <summary>
    /// Section 标题.
    /// </summary>
    [ObservableProperty]
    public partial string Title { get; set; }

    /// <summary>
    /// Fluent 图标.
    /// </summary>
    [ObservableProperty]
    public partial Symbol Symbol { get; set; }

    /// <summary>
    /// 条目总数（若有）.
    /// </summary>
    [ObservableProperty]
    public partial long? Count { get; set; }

    /// <summary>
    /// 是否显示数量徽标.
    /// </summary>
    public bool HasCount => Count.HasValue && Count.Value > 0;

    /// <summary>
    /// 子分类列表（如全部、剧作、合集、剧评等）.
    /// </summary>
    [ObservableProperty]
    public partial ObservableCollection<UserSpaceSubTabItemViewModel> SubTabs { get; set; }

    /// <summary>
    /// 当前选中的子分类.
    /// </summary>
    [ObservableProperty]
    public partial UserSpaceSubTabItemViewModel? SelectedSubTab { get; set; }

    /// <summary>
    /// 是否拥有子分类.
    /// </summary>
    public bool HasSubTabs => SubTabs.Count > 1;

    /// <summary>
    /// 所属用户空间页面视图模型.
    /// </summary>
    [ObservableProperty]
    public partial View.UserSpacePageViewModel? PageViewModel { get; set; }

    /// <summary>
    /// 构造函数.
    /// </summary>
    public UserSpaceSectionItemViewModel(
        string id,
        string title,
        Symbol symbol,
        long? count = null,
        View.UserSpacePageViewModel? pageViewModel = null
    )
    {
        Id = id;
        Title = title;
        Symbol = symbol;
        Count = count;
        PageViewModel = pageViewModel;
        SubTabs = [];
        SubTabs.CollectionChanged += (_, _) => OnPropertyChanged(nameof(HasSubTabs));
    }
}

/// <summary>
/// 子分类 Tab 模型.
/// </summary>
internal sealed partial class UserSpaceSubTabItemViewModel : ViewModelBase, IUserSpaceContentItem
{
    /// <summary>
    /// 唯一标识.
    /// </summary>
    public string Id { get; init; }

    /// <summary>
    /// 标题.
    /// </summary>
    [ObservableProperty]
    public partial string Title { get; set; }

    /// <summary>
    /// 附带的值或枚举对象.
    /// </summary>
    public object? Value { get; init; }

    /// <summary>
    /// 所属用户空间页面视图模型.
    /// </summary>
    [ObservableProperty]
    public partial View.UserSpacePageViewModel? PageViewModel { get; set; }

    public UserSpaceSubTabItemViewModel(
        string id,
        string title,
        object? value = null,
        View.UserSpacePageViewModel? pageViewModel = null
    )
    {
        Id = id;
        Title = title;
        Value = value;
        PageViewModel = pageViewModel;
    }

    public override string ToString() => Title;
}

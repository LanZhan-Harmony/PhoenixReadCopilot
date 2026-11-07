using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using Windows.Globalization;
using WinUIKernel.Share.ViewModels;

namespace PhoenixReadCopilot.ViewModels.Items;

/// <summary>
/// 时间轴条目视图模型.
/// </summary>
public sealed partial class TimelineItemViewModel : ViewModelBase
{
    [ObservableProperty]
    public partial List<SeasonItemViewModel>? Items { get; set; }

    [ObservableProperty]
    public partial bool IsEmpty { get; set; }

    [ObservableProperty]
    public partial int Count { get; set; }

    /// <summary>
    /// 目标日期.
    /// </summary>
    public DateOnly TargetDate { get; }

    /// <summary>
    /// 是否为今天.
    /// </summary>
    public bool IsToday { get; }

    /// <summary>
    /// 星期几.
    /// </summary>
    public string DayOfWeek { get; }

    /// <summary>
    /// 日期字符串 (MM-dd).
    /// </summary>
    public string Date { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="TimelineItemViewModel"/> class from a <see cref="DateOnly"/>.
    /// </summary>
    public TimelineItemViewModel(DateOnly targetDate, bool isToday = false, List<SeasonItemViewModel>? items = null)
    {
        TargetDate = targetDate;
        IsToday = isToday;
        var firstLan = ApplicationLanguages.Languages.Count > 0 ? ApplicationLanguages.Languages[0] : "zh-CN";
        var culture = new CultureInfo(firstLan);
        DayOfWeek = isToday ? "今天" : targetDate.ToDateTime(TimeOnly.MinValue).ToString("ddd", culture);
        Date = targetDate.ToString("MM-dd");
        Items = items;
        Count = items?.Count ?? 0;
        IsEmpty = items != null && items.Count == 0;
    }
}

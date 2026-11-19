using System.Collections.ObjectModel;
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
    public partial ObservableCollection<VideoItemViewModel> Items { get; set; } = [];

    [ObservableProperty]
    public partial bool IsEmpty { get; set; }

    [ObservableProperty]
    public partial int Count { get; set; }

    /// <summary>
    /// 分页下一页偏移量.
    /// </summary>
    public int NextOffset { get; set; }

    /// <summary>
    /// 是否还有更多数据.
    /// </summary>
    public bool HasMore { get; set; } = true;

    /// <summary>
    /// 跨页会话标识.
    /// </summary>
    public string? SessionId { get; set; }

    /// <summary>
    /// 该日期的数据是否已完成首屏加载.
    /// </summary>
    public bool IsLoaded { get; set; }

    /// <summary>
    /// 是否正在加载更多.
    /// </summary>
    public bool IsLoadingMore { get; set; }

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
    public TimelineItemViewModel(
        DateOnly targetDate,
        bool isToday = false,
        IEnumerable<VideoItemViewModel>? items = null
    )
    {
        TargetDate = targetDate;
        IsToday = isToday;
        string firstLan =
            ApplicationLanguages.Languages.Count > 0 ? ApplicationLanguages.Languages[0] : "zh-CN";
        CultureInfo culture = new(firstLan);
        DayOfWeek = isToday
            ? "今天"
            : targetDate.ToDateTime(TimeOnly.MinValue).ToString("ddd", culture);
        Date = targetDate.ToString("MM-dd");

        if (items is not null)
        {
            Items = [.. items];
            Count = Items.Count;
            IsLoaded = true;
            IsEmpty = Items.Count == 0;
        }
        else
        {
            Items = [];
            Count = 0;
            IsLoaded = false;
            IsEmpty = false;
        }
    }
}

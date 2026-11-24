using CommunityToolkit.Mvvm.ComponentModel;
using PhoenixReadCopilot.Core.Models.Filter;

namespace PhoenixReadCopilot.ViewModels.Items;

/// <summary>
/// 索引过滤器视图模型.
/// </summary>
internal sealed partial class IndexFilterViewModel : ViewModelBase
{
    private readonly Action? _reloadAction;

    /// <summary>
    /// 筛选数据实体.
    /// </summary>
    public Filter Data { get; }

    [ObservableProperty]
    public partial List<Condition>? Conditions { get; set; }

    [ObservableProperty]
    public partial Condition? CurrentCondition { get; set; }

    /// <summary>
    /// 维度标识键.
    /// </summary>
    public string Key => Data.Key;

    /// <summary>
    /// 维度显示名称.
    /// </summary>
    public string Name => Data.Name;

    public IndexFilterViewModel(Filter filter, Action? reloadAction)
    {
        Data = filter;
        Conditions = [.. filter.Conditions];
        CurrentCondition = Conditions.FirstOrDefault();
        _reloadAction = reloadAction;
    }

    partial void OnCurrentConditionChanged(Condition? value)
    {
        _reloadAction?.Invoke();
    }

    public void ChangeCondition(Condition condition)
    {
        if (condition is null || condition == CurrentCondition)
        {
            return;
        }

        CurrentCondition = condition;
    }
}

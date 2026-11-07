using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PhoenixReadCopilot.Models;
using WinUIKernel.Share.ViewModels;

namespace PhoenixReadCopilot.ViewModels.Items;

/// <summary>
/// 索引过滤器视图模型.
/// </summary>
public sealed partial class IndexFilterViewModel : ViewModelBase<Filter>
{
    private readonly Action? _reloadAction;

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

    /// <summary>
    /// Initializes a new instance of the <see cref="IndexFilterViewModel"/> class.
    /// </summary>
    public IndexFilterViewModel(Filter filter, Action? reloadAction)
        : base(filter)
    {
        Conditions = [.. filter.Conditions];
        CurrentCondition = Conditions.FirstOrDefault();
        _reloadAction = reloadAction;
    }

    partial void OnCurrentConditionChanged(Condition? value)
    {
        _reloadAction?.Invoke();
    }

    [RelayCommand]
    private void ChangeCondition(Condition condition)
    {
        if (condition is null || condition == CurrentCondition)
        {
            return;
        }

        CurrentCondition = condition;
    }
}

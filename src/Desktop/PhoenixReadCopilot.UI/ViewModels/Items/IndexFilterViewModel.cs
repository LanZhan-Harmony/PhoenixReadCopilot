using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Richasy.BiliKernel.Models.Appearance;
using WinUIKernel.Share.ViewModels;

namespace PhoenixReadCopilot.UI.ViewModels.Items;

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
    /// Initializes a new instance of the <see cref="IndexFilterViewModel"/> class.
    /// </summary>
    public IndexFilterViewModel(Filter filter, Action? reloadAction)
        : base(filter)
    {
        Conditions = [.. filter.Conditions];
        CurrentCondition = Conditions.FirstOrDefault();
        _reloadAction = reloadAction;
    }

    [RelayCommand]
    private void ChangeCondition(Condition condition)
    {
        if (condition is null || condition == CurrentCondition)
        {
            return;
        }

        CurrentCondition = condition;
        _reloadAction?.Invoke();
    }
}

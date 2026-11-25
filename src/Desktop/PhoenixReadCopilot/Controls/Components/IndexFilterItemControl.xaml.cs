using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using PhoenixReadCopilot.Core.Models.Filter;
using PhoenixReadCopilot.ViewModels.Items;

namespace PhoenixReadCopilot.Controls.Components;

/// <summary>
/// 索引过滤器项控件.
/// </summary>
internal sealed partial class IndexFilterItemControl : UserControl
{
    public IndexFilterViewModel? ViewModel
    {
        get => (IndexFilterViewModel?)GetValue(ViewModelProperty);
        set => SetValue(ViewModelProperty, value);
    }

    public static readonly DependencyProperty ViewModelProperty = DependencyProperty.Register(
        nameof(ViewModel),
        typeof(IndexFilterViewModel),
        typeof(IndexFilterItemControl),
        new PropertyMetadata(null)
    );

    public IndexFilterItemControl()
    {
        InitializeComponent();
    }

    private void OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        Condition? condition = ConditionComboBox.SelectedItem as Condition;
        if (condition is not null)
        {
            ViewModel?.ChangeCondition(condition);
        }
    }
}

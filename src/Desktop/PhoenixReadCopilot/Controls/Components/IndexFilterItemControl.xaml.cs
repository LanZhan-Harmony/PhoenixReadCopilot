using Microsoft.UI.Xaml.Controls;
using PhoenixReadCopilot.Core.Models.Filter;
using PhoenixReadCopilot.ViewModels.Items;
using WinUIKernel.Share.Base;

namespace PhoenixReadCopilot.Controls.Components;

/// <summary>
/// 索引过滤器项控件.
/// </summary>
public sealed partial class IndexFilterItemControl : IndexFilterItemControlBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="IndexFilterItemControl"/> class.
    /// </summary>
    public IndexFilterItemControl()
    {
        InitializeComponent();
    }

    /// <inheritdoc/>
    protected override void OnControlLoaded() =>
        ConditionComboBox.SelectionChanged += OnSelectionChanged;

    /// <inheritdoc/>
    protected override void OnControlUnloaded() =>
        ConditionComboBox.SelectionChanged -= OnSelectionChanged;

    private void OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        Condition? condition = ConditionComboBox.SelectedItem as Condition;
        if (condition is not null)
        {
            ViewModel.ChangeConditionCommand.Execute(condition);
        }
    }
}

/// <summary>
/// 索引过滤器项控件基类.
/// </summary>
public abstract class IndexFilterItemControlBase : LayoutUserControlBase<IndexFilterViewModel> { }

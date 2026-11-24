using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using PhoenixReadCopilot.ViewModels.Items;

namespace PhoenixReadCopilot.Controls.Components;

/// <summary>
/// 时间轴控件.
/// </summary>
internal sealed partial class TimelineControl : UserControl
{
    public TimelineItemViewModel? ViewModel
    {
        get => (TimelineItemViewModel?)GetValue(ViewModelProperty);
        set => SetValue(ViewModelProperty, value);
    }

    public static readonly DependencyProperty ViewModelProperty = DependencyProperty.Register(
        nameof(ViewModel),
        typeof(TimelineItemViewModel),
        typeof(TimelineControl),
        new PropertyMetadata(null)
    );

    /// <summary>
    /// Initializes a new instance of the <see cref="TimelineControl"/> class.
    /// </summary>
    public TimelineControl()
    {
        InitializeComponent();
    }
}

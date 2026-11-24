using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using PhoenixReadCopilot.ViewModels.View;

namespace PhoenixReadCopilot.Controls.Calendar;

/// <summary>
/// 上新日历左侧顶部.
/// </summary>
internal sealed partial class CalendarSideHeader : UserControl
{
    public CalendarPageViewModel? ViewModel
    {
        get => (CalendarPageViewModel?)GetValue(ViewModelProperty);
        set => SetValue(ViewModelProperty, value);
    }

    public static readonly DependencyProperty ViewModelProperty = DependencyProperty.Register(
        nameof(ViewModel),
        typeof(CalendarPageViewModel),
        typeof(CalendarSideHeader),
        new PropertyMetadata(null)
    );

    /// <summary>
    /// Initializes a new instance of the <see cref="CalendarSideHeader"/> class.
    /// </summary>
    public CalendarSideHeader()
    {
        InitializeComponent();
    }
}

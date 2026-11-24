using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using PhoenixReadCopilot.ViewModels.View;

namespace PhoenixReadCopilot.Controls.Calendar;

/// <summary>
/// 上新日历右侧顶部.
/// </summary>
internal sealed partial class CalendarMainHeader : UserControl
{
    public CalendarPageViewModel? ViewModel
    {
        get => (CalendarPageViewModel?)GetValue(ViewModelProperty);
        set => SetValue(ViewModelProperty, value);
    }

    public static readonly DependencyProperty ViewModelProperty = DependencyProperty.Register(
        nameof(ViewModel),
        typeof(CalendarPageViewModel),
        typeof(CalendarMainHeader),
        new PropertyMetadata(null)
    );

    /// <summary>
    /// Initializes a new instance of the <see cref="CalendarMainHeader"/> class.
    /// </summary>
    public CalendarMainHeader()
    {
        InitializeComponent();
    }
}

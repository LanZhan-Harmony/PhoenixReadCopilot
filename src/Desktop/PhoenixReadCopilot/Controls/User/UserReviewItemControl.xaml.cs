using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using PhoenixReadCopilot.ViewModels.Items;

namespace PhoenixReadCopilot.Controls.User;

public sealed partial class UserReviewItemControl : UserControl
{
    public static readonly DependencyProperty ViewModelProperty =
        DependencyProperty.Register(
            nameof(ViewModel),
            typeof(UserReviewItemViewModel),
            typeof(UserReviewItemControl),
            new PropertyMetadata(null)
        );

    public UserReviewItemViewModel ViewModel
    {
        get => (UserReviewItemViewModel)GetValue(ViewModelProperty);
        set => SetValue(ViewModelProperty, value);
    }

    public UserReviewItemControl()
    {
        InitializeComponent();
    }
}

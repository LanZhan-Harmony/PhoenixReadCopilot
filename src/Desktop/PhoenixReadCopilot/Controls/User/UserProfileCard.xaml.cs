using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using PhoenixReadCopilot.ViewModels.View;

namespace PhoenixReadCopilot.Controls.User;

/// <summary>
/// 用户空间顶部资料卡片控件.
/// </summary>
internal sealed partial class UserProfileCard : UserControl
{
    /// <summary>
    /// 视图模型.
    /// </summary>
    public UserSpacePageViewModel? ViewModel
    {
        get => (UserSpacePageViewModel?)GetValue(ViewModelProperty);
        set => SetValue(ViewModelProperty, value);
    }

    public static readonly DependencyProperty ViewModelProperty = DependencyProperty.Register(
        nameof(ViewModel),
        typeof(UserSpacePageViewModel),
        typeof(UserProfileCard),
        new PropertyMetadata(null)
    );

    public UserProfileCard()
    {
        InitializeComponent();
    }
}

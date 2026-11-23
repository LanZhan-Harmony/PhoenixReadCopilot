using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using PhoenixReadCopilot.ViewModels.View;

namespace PhoenixReadCopilot.Controls.User;

/// <summary>
/// 用户空间控件基类.
/// </summary>
internal abstract class UserControlBase : UserControl
{
    public static readonly DependencyProperty ViewModelProperty =
        DependencyProperty.Register(
            nameof(ViewModel),
            typeof(UserSpacePageViewModel),
            typeof(UserControlBase),
            new PropertyMetadata(null, OnViewModelPropertyChanged)
        );

    protected UserControlBase()
    {
        Loaded += (_, _) => OnControlLoaded();
        Unloaded += (_, _) => OnControlUnloaded();
    }

    private static void OnViewModelPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is UserControlBase control)
        {
            control.OnViewModelChanged(e.OldValue as UserSpacePageViewModel, e.NewValue as UserSpacePageViewModel);
        }
    }

    /// <summary>
    /// 当控件加载完成时调用.
    /// </summary>
    protected virtual void OnControlLoaded()
    {
    }

    /// <summary>
    /// 当控件卸载时调用.
    /// </summary>
    protected virtual void OnControlUnloaded()
    {
    }

    /// <summary>
    /// 当 ViewModel 实例发生变化时调用.
    /// </summary>
    protected virtual void OnViewModelChanged(UserSpacePageViewModel? oldViewModel, UserSpacePageViewModel? newViewModel)
    {
    }

    /// <summary>
    /// 视图模型.
    /// </summary>
    public UserSpacePageViewModel ViewModel
    {
        get => (UserSpacePageViewModel)GetValue(ViewModelProperty);
        set => SetValue(ViewModelProperty, value);
    }
}

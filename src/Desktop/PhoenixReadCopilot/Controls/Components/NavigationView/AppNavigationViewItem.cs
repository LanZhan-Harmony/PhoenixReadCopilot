using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Symbol = FluentIcons.Common.Symbol;

namespace PhoenixReadCopilot.Controls.Components;

/// <summary>
/// 应用导航视图项.
/// </summary>
public sealed partial class AppNavigationViewItem : NavigationViewItem
{
    /// <summary>
    /// 图标.
    /// </summary>
    public Symbol Symbol
    {
        get => (Symbol)GetValue(SymbolProperty);
        set => SetValue(SymbolProperty, value);
    }

    public static readonly DependencyProperty SymbolProperty = DependencyProperty.Register(
        nameof(Symbol),
        typeof(Symbol),
        typeof(AppNavigationViewItem),
        new PropertyMetadata(null)
    );

    /// <summary>
    /// 是否显示未读消息.
    /// </summary>
    public bool ShowUnread
    {
        get => (bool)GetValue(ShowUnreadProperty);
        set => SetValue(ShowUnreadProperty, value);
    }

    public static readonly DependencyProperty ShowUnreadProperty = DependencyProperty.Register(
        nameof(ShowUnread),
        typeof(bool),
        typeof(AppNavigationViewItem),
        new PropertyMetadata(false, OnShowUnreadChanged)
    );

    /// <summary>
    /// 未读标识可见性.
    /// </summary>
    public Visibility UnreadVisibility
    {
        get => (Visibility)GetValue(UnreadVisibilityProperty);
        private set => SetValue(UnreadVisibilityProperty, value);
    }

    public static readonly DependencyProperty UnreadVisibilityProperty = DependencyProperty.Register(
        nameof(UnreadVisibility),
        typeof(Visibility),
        typeof(AppNavigationViewItem),
        new PropertyMetadata(Visibility.Collapsed)
    );

    public AppNavigationViewItem()
    {
        DefaultStyleKey = typeof(AppNavigationViewItem);
    }

    private static void OnShowUnreadChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is AppNavigationViewItem item)
        {
            item.UnreadVisibility = e.NewValue is true ? Visibility.Visible : Visibility.Collapsed;
        }
    }
}

/// <summary>
/// 应用导航视图项显示器.
/// </summary>
public sealed partial class AppNavigationViewItemPresenter : NavigationViewItemPresenter
{
    /// <summary>
    /// 图标.
    /// </summary>
    public Symbol Symbol
    {
        get => (Symbol)GetValue(SymbolProperty);
        set => SetValue(SymbolProperty, value);
    }

    public static readonly DependencyProperty SymbolProperty = DependencyProperty.Register(
        nameof(Symbol),
        typeof(Symbol),
        typeof(AppNavigationViewItemPresenter),
        new PropertyMetadata(null)
    );

    public AppNavigationViewItemPresenter()
    {
        DefaultStyleKey = typeof(AppNavigationViewItemPresenter);
    }
}

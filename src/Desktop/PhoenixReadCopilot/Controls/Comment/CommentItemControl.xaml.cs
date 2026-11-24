using System.Windows.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using PhoenixReadCopilot.ViewModels.Items;

namespace PhoenixReadCopilot.Controls.Comment;

/// <summary>
/// 评论控件.
/// </summary>
internal sealed partial class CommentItemControl : UserControl
{
    public CommentItemViewModel? ViewModel
    {
        get => (CommentItemViewModel?)GetValue(ViewModelProperty);
        set => SetValue(ViewModelProperty, value);
    }

    public static readonly DependencyProperty ViewModelProperty = DependencyProperty.Register(
        nameof(ViewModel),
        typeof(CommentItemViewModel),
        typeof(CommentItemControl),
        new PropertyMetadata(null)
    );

    /// <summary>
    /// 显示更多命令.
    /// </summary>
    public ICommand ShowMoreCommand
    {
        get => (ICommand)GetValue(ShowMoreCommandProperty);
        set => SetValue(ShowMoreCommandProperty, value);
    }

    public static readonly DependencyProperty ShowMoreCommandProperty = DependencyProperty.Register(
        nameof(ShowMoreCommand),
        typeof(ICommand),
        typeof(CommentItemControl),
        new PropertyMetadata(null)
    );

    /// <summary>
    /// 是否启用更多.
    /// </summary>
    public bool IsMoreEnabled
    {
        get => (bool)GetValue(IsMoreEnabledProperty);
        set => SetValue(IsMoreEnabledProperty, value);
    }

    public static readonly DependencyProperty IsMoreEnabledProperty = DependencyProperty.Register(
        nameof(IsMoreEnabled),
        typeof(bool),
        typeof(CommentItemControl),
        new PropertyMetadata(true)
    );

    public CommentItemControl()
    {
        InitializeComponent();
    }
}

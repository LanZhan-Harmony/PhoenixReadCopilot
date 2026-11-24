using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using PhoenixReadCopilot.ViewModels.Items;

namespace PhoenixReadCopilot.Controls.Components;

/// <summary>
/// 评论项视图模型.
/// </summary>
internal sealed partial class CommentLikeButton : UserControl
{
    public CommentItemViewModel? ViewModel
    {
        get => (CommentItemViewModel?)GetValue(ViewModelProperty);
        set => SetValue(ViewModelProperty, value);
    }

    public static readonly DependencyProperty ViewModelProperty = DependencyProperty.Register(
        nameof(ViewModel),
        typeof(CommentItemViewModel),
        typeof(CommentLikeButton),
        new PropertyMetadata(null)
    );

    /// <summary>
    /// Initializes a new instance of the <see cref="CommentLikeButton"/> class.
    /// </summary>
    public CommentLikeButton()
    {
        InitializeComponent();
    }
}

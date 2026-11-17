using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Windows.UI.Text;
using FluentSymbolIcon = FluentIcons.WinUI.SymbolIcon;
using Symbol = FluentIcons.Common.Symbol;

namespace PhoenixReadCopilot.Controls.Components;

[TemplatePart(Name = IconPartName, Type = typeof(FluentSymbolIcon))]
[TemplatePart(Name = TextPartName, Type = typeof(TextBlock))]
internal sealed partial class IconTextBlock : Control
{
    private const string IconPartName = "PART_Icon";
    private const string TextPartName = "PART_Text";

    private FluentSymbolIcon? _iconPart;
    private TextBlock? _textPart;

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
        typeof(IconTextBlock),
        new PropertyMetadata(null)
    );

    /// <summary>
    /// 文本.
    /// </summary>
    public string Text
    {
        get => (string)GetValue(TextProperty);
        set => SetValue(TextProperty, value);
    }

    public static readonly DependencyProperty TextProperty = DependencyProperty.Register(
        nameof(Text),
        typeof(string),
        typeof(IconTextBlock),
        new PropertyMetadata(null)
    );

    /// <summary>
    /// 图标画刷. 若未设置则回退使用 <see cref="Control.Foreground"/>.
    /// </summary>
    public Brush? IconBrush
    {
        get => (Brush?)GetValue(IconBrushProperty);
        set => SetValue(IconBrushProperty, value);
    }

    public static readonly DependencyProperty IconBrushProperty = DependencyProperty.Register(
        nameof(IconBrush),
        typeof(Brush),
        typeof(IconTextBlock),
        new PropertyMetadata(null, OnBrushPropertyChanged)
    );

    /// <summary>
    /// 文字画刷. 若未设置则回退使用 <see cref="Control.Foreground"/>.
    /// </summary>
    public Brush? TextBrush
    {
        get => (Brush?)GetValue(TextBrushProperty);
        set => SetValue(TextBrushProperty, value);
    }

    public static readonly DependencyProperty TextBrushProperty = DependencyProperty.Register(
        nameof(TextBrush),
        typeof(Brush),
        typeof(IconTextBlock),
        new PropertyMetadata(null, OnBrushPropertyChanged)
    );

    /// <summary>
    /// 文字裁剪方式.
    /// </summary>
    public TextTrimming TextTrimming
    {
        get => (TextTrimming)GetValue(TextTrimmingProperty);
        set => SetValue(TextTrimmingProperty, value);
    }

    public static readonly DependencyProperty TextTrimmingProperty = DependencyProperty.Register(
        nameof(TextTrimming),
        typeof(TextTrimming),
        typeof(IconTextBlock),
        new PropertyMetadata(TextTrimming.CharacterEllipsis)
    );

    /// <summary>
    /// 图标大小.
    /// </summary>
    public double IconSize
    {
        get => (double)GetValue(IconSizeProperty);
        set => SetValue(IconSizeProperty, value);
    }

    public static readonly DependencyProperty IconSizeProperty = DependencyProperty.Register(
        nameof(IconSize),
        typeof(double),
        typeof(IconTextBlock),
        new PropertyMetadata(10d)
    );

    /// <summary>
    /// 文字大小.
    /// </summary>
    public double TextSize
    {
        get => (double)GetValue(TextSizeProperty);
        set => SetValue(TextSizeProperty, value);
    }

    public static readonly DependencyProperty TextSizeProperty = DependencyProperty.Register(
        nameof(TextSize),
        typeof(double),
        typeof(IconTextBlock),
        new PropertyMetadata(12d)
    );

    /// <summary>
    /// 文字字重
    /// </summary>
    public FontWeight TextWeight
    {
        get => (FontWeight)GetValue(TextWeightProperty);
        set => SetValue(TextWeightProperty, value);
    }

    public static readonly DependencyProperty TextWeightProperty = DependencyProperty.Register(
        nameof(TextWeight),
        typeof(FontWeight),
        typeof(IconTextBlock),
        new PropertyMetadata(FontWeights.Normal)
    );

    /// <summary>
    /// 图标与文字间距.
    /// </summary>
    public double IconTextSpacing
    {
        get => (double)GetValue(IconTextSpacingProperty);
        set => SetValue(IconTextSpacingProperty, value);
    }

    public static readonly DependencyProperty IconTextSpacingProperty = DependencyProperty.Register(
        nameof(IconTextSpacing),
        typeof(double),
        typeof(IconTextBlock),
        new PropertyMetadata(4d)
    );

    /// <summary>
    /// 是否反转图标与文字位置（为 true 时文字在前，图标在后）.
    /// </summary>
    public bool IsReversed
    {
        get => (bool)GetValue(IsReversedProperty);
        set => SetValue(IsReversedProperty, value);
    }

    public static readonly DependencyProperty IsReversedProperty = DependencyProperty.Register(
        nameof(IsReversed),
        typeof(bool),
        typeof(IconTextBlock),
        new PropertyMetadata(false, OnIsReversedChanged)
    );

    public IconTextBlock()
    {
        DefaultStyleKey = typeof(IconTextBlock);
        RegisterPropertyChangedCallback(ForegroundProperty, (s, e) =>
            ((IconTextBlock)s).UpdateBrushes()
        );
    }

    protected override void OnApplyTemplate()
    {
        base.OnApplyTemplate();
        _iconPart = GetTemplateChild(IconPartName) as FluentSymbolIcon;
        _textPart = GetTemplateChild(TextPartName) as TextBlock;
        UpdateLayoutOrder();
        UpdateBrushes();
    }

    private static void OnIsReversedChanged(
        DependencyObject d,
        DependencyPropertyChangedEventArgs e
    )
    {
        if (d is IconTextBlock control)
        {
            control.UpdateLayoutOrder();
        }
    }

    private static void OnBrushPropertyChanged(
        DependencyObject d,
        DependencyPropertyChangedEventArgs e
    )
    {
        if (d is IconTextBlock control)
        {
            control.UpdateBrushes();
        }
    }

    private void UpdateLayoutOrder()
    {
        if (_iconPart is not null && _textPart is not null)
        {
            Grid.SetColumn(_iconPart, IsReversed ? 1 : 0);
            Grid.SetColumn(_textPart, IsReversed ? 0 : 1);
        }
    }

    private void UpdateBrushes()
    {
        _iconPart?.Foreground = IconBrush ?? Foreground;
        _textPart?.Foreground = TextBrush ?? Foreground;
    }
}

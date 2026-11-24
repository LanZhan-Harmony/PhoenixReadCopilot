using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace PhoenixReadCopilot.Controls.Components;

/// <summary>
/// 状态提示.
/// </summary>
internal sealed partial class StatusTip : UserControl
{
    /// <summary>
    /// 显示文本.
    /// </summary>
    public string Text
    {
        get => (string)GetValue(TextProperty);
        set => SetValue(TextProperty, value);
    }

    public static readonly DependencyProperty TextProperty = DependencyProperty.Register(
        nameof(Text),
        typeof(string),
        typeof(StatusTip),
        new PropertyMetadata(string.Empty)
    );

    /// <summary>
    /// 当前状态.
    /// </summary>
    public InfoType Status
    {
        get => (InfoType)GetValue(StatusProperty);
        set => SetValue(StatusProperty, value);
    }

    public static readonly DependencyProperty StatusProperty = DependencyProperty.Register(
        nameof(Status),
        typeof(InfoType),
        typeof(StatusTip),
        new PropertyMetadata(null, new PropertyChangedCallback(OnStatusChanged))
    );

    /// <summary>
    /// Initializes a new instance of the <see cref="StatusTip"/> class.
    /// </summary>
    public StatusTip()
    {
        InitializeComponent();
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        ChangeStatus();
    }

    private static void OnStatusChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        StatusTip? instance = d as StatusTip;
        instance.ChangeStatus();
    }

    private void ChangeStatus()
    {
        InformationIcon.Visibility = Visibility.Collapsed;
        SuccessIcon.Visibility = Visibility.Collapsed;
        WarningIcon.Visibility = Visibility.Collapsed;
        ErrorIcon.Visibility = Visibility.Collapsed;
        LoadingRing.Visibility = Visibility.Collapsed;
        LoadingRing.IsActive = false;

        switch (Status)
        {
            case InfoType.Information:
                InformationIcon.Visibility = Visibility.Visible;
                break;
            case InfoType.Success:
                SuccessIcon.Visibility = Visibility.Visible;
                break;
            case InfoType.Warning:
                WarningIcon.Visibility = Visibility.Visible;
                break;
            case InfoType.Error:
                ErrorIcon.Visibility = Visibility.Visible;
                break;
            case InfoType.Loading:
                LoadingRing.Visibility = Visibility.Visible;
                LoadingRing.IsActive = true;
                break;
            default:
                break;
        }
    }
}

/// <summary>
/// 信息类型.
/// </summary>
public enum InfoType
{
    /// <summary>
    /// 普通消息.
    /// </summary>
    Information,

    /// <summary>
    /// 错误消息.
    /// </summary>
    Error,

    /// <summary>
    /// 警告消息.
    /// </summary>
    Warning,

    /// <summary>
    /// 成功消息.
    /// </summary>
    Success,

    /// <summary>
    /// 加载中.
    /// </summary>
    Loading,
}

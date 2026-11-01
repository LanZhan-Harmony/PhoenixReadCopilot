using CommunityToolkit.Mvvm.ComponentModel;
using WinUIKernel.Share.ViewModels;

namespace PhoenixReadCopilot.UI.ViewModels;

/// <summary>
/// 布局页面视图模型基类.
/// </summary>
public abstract partial class LayoutPageViewModelBase : ViewModelBase
{
    [ObservableProperty]
    public partial double NavColumnWidth { get; set; }

    [ObservableProperty]
    public partial bool IsNavColumnManualHide { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="LayoutPageViewModelBase"/> class.
    /// </summary>
    protected LayoutPageViewModelBase()
    {
#pragma warning disable CA2214
        NavColumnWidth = Settings.GetNavColumnWidth(GetPageKey(), GetDefaultNavColumnWidth());
        IsNavColumnManualHide = Settings.GetNavColumnManualHide(GetPageKey(), false);
#pragma warning restore CA2214
    }

    /// <summary>
    /// 获取页面名称.
    /// </summary>
    /// <returns>页面名称.</returns>
    protected abstract string GetPageKey();

    /// <summary>
    /// 获取默认导航栏宽度.
    /// </summary>
    /// <returns>宽度.</returns>
    protected virtual double GetDefaultNavColumnWidth() => 240d;

    /// <summary>
    /// 导航栏手动关闭时的行为.
    /// </summary>
    protected virtual void IsNavManualHideChanged(bool value) =>
        NavColumnWidth = value
            ? 0
            : Settings.GetNavColumnWidth(GetPageKey(), 240d);

    partial void OnNavColumnWidthChanged(double value)
    {
        if (value > 0)
        {
            Settings.SetNavColumnWidth(GetPageKey(), value);
        }
    }

    partial void OnIsNavColumnManualHideChanged(bool value)
    {
        Settings.SetNavColumnManualHide(GetPageKey(), value);
        IsNavManualHideChanged(value);
    }
}

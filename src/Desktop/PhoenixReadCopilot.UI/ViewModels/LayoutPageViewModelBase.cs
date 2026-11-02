using CommunityToolkit.Mvvm.ComponentModel;
using PhoenixReadCopilot.UI.Models.Settings;
using PhoenixReadCopilot.UI.Services;
using WinUIKernel.Share.ViewModels;

namespace PhoenixReadCopilot.UI.ViewModels;

/// <summary>
/// 布局页面视图模型基类.
/// </summary>
public abstract partial class LayoutPageViewModelBase : ViewModelBase
{
    /// <summary>
    /// 应用设置.
    /// </summary>
    protected readonly AppSettings _settings;

    [ObservableProperty]
    public partial double NavColumnWidth { get; set; }

    [ObservableProperty]
    public partial bool IsNavColumnManualHide { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="LayoutPageViewModelBase"/> class.
    /// </summary>
    protected LayoutPageViewModelBase(ISettingsService settingsService)
    {
        _settings = settingsService.Settings;
#pragma warning disable CA2214
        NavColumnWidth = _settings.GetNavColumnWidth(GetPageKey(), GetDefaultNavColumnWidth());
        IsNavColumnManualHide = _settings.GetNavColumnManualHide(GetPageKey(), false);
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
        NavColumnWidth = value ? 0 : _settings.GetNavColumnWidth(GetPageKey(), 240d);

    partial void OnNavColumnWidthChanged(double value)
    {
        if (value > 0)
        {
            _settings.SetNavColumnWidth(GetPageKey(), value);
        }
    }

    partial void OnIsNavColumnManualHideChanged(bool value)
    {
        _settings.SetNavColumnManualHide(GetPageKey(), value);
        IsNavManualHideChanged(value);
    }
}

using CommunityToolkit.Mvvm.DependencyInjection;
using PhoenixReadCopilot.ViewModels.View;
using WinUIKernel.Share.Base;

namespace PhoenixReadCopilot.Controls.Settings;

/// <summary>
/// 设置页面控件基类.
/// </summary>
public abstract class SettingsPageControlBase : LayoutUserControlBase<SettingsPageViewModel>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SettingsPageControlBase"/> class.
    /// </summary>
    protected SettingsPageControlBase() =>
        ViewModel = Ioc.Default.GetRequiredService<SettingsPageViewModel>();
}

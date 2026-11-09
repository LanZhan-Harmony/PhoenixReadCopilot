using CommunityToolkit.Mvvm.DependencyInjection;
using PhoenixReadCopilot.ViewModels.View;
using WinUIKernel.Share.Base;

namespace PhoenixReadCopilot.Pages;

/// <summary>
/// 动漫页面.
/// </summary>
public partial class AnimePage : AnimePageBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AnimePage"/> class.
    /// </summary>
    public AnimePage() => InitializeComponent();

    /// <inheritdoc/>
    protected override void OnPageLoaded() => ViewModel.InitializeCommand.Execute(null);
}

/// <summary>
/// 动漫页面基类.
/// </summary>
public abstract class AnimePageBase : LayoutPageBase<AnimePageViewModel>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AnimePageBase"/> class.
    /// </summary>
    protected AnimePageBase() => ViewModel = Ioc.Default.GetRequiredService<AnimePageViewModel>();
}

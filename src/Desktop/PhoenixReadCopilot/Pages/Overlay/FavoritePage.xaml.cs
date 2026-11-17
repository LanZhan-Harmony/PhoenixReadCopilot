using CommunityToolkit.Mvvm.DependencyInjection;
using PhoenixReadCopilot.ViewModels.View;
using WinUIKernel.Share.Base;

namespace PhoenixReadCopilot.Pages.Overlay;

/// <summary>
/// 收藏页面.
/// </summary>
public sealed partial class FavoritePage : FavoritePageBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="FavoritePage"/> class.
    /// </summary>
    public FavoritePage()
    {
        InitializeComponent();
    }

    /// <inheritdoc/>
    protected override void OnPageLoaded() => _ = ViewModel.InitializeAsync();
}

/// <summary>
/// 收藏页面基类.
/// </summary>
public abstract class FavoritePageBase : LayoutPageBase<FavoritePageViewModel>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="FavoritePageBase"/> class.
    /// </summary>
    protected FavoritePageBase()
    {
        ViewModel = Ioc.Default.GetRequiredService<FavoritePageViewModel>();
    }
}

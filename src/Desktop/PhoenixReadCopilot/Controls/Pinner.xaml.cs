using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.UI.Xaml;
using PhoenixReadCopilot.Core.Contracts.Services;
using PhoenixReadCopilot.Core.Enums;
using PhoenixReadCopilot.Core.Models.Media;
using PhoenixReadCopilot.Core.Models.Pin;
using PhoenixReadCopilot.Core.Models.User;
using PhoenixReadCopilot.Pages;
using PhoenixReadCopilot.Pages.Overlay;
using PhoenixReadCopilot.ViewModels.Core;
using WinUIKernel.Share.Base;

namespace PhoenixReadCopilot.Controls;

/// <summary>
/// 固定器.
/// </summary>
public sealed partial class Pinner : PinnerBase
{
    private readonly INavigationService _navigationService =
        Ioc.Default.GetRequiredService<INavigationService>();

    /// <summary>
    /// Initializes a new instance of the <see cref="Pinner"/> class.
    /// </summary>
    public Pinner()
    {
        InitializeComponent();
    }

    /// <inheritdoc/>
    protected override void OnControlLoaded() => _ = ViewModel.InitializeAsync();

    protected override void OnControlUnloaded() => Repeater.ItemsSource = null;

    private void OnItemClick(object sender, RoutedEventArgs e)
    {
        if ((sender as FrameworkElement)?.DataContext is not PinItem item)
        {
            return;
        }

        INavigationService navVM = _navigationService;
        if (item.Type == PinType.User)
        {
            PhoenixReadCopilot.Core.Models.User.User user = new()
            {
                Id = item.Id,
                Name = item.Title,
                Avatar = Uri.TryCreate(item.Cover, UriKind.Absolute, out Uri? coverUri)
                    ? new UserAvatar { Uri = coverUri }
                    : null,
            };
            navVM.NavigateToOver(typeof(UserSpacePage), user);
        }
        else if (item.Type == PinType.Drama)
        {
            VideoSnapshot snapshot = new(
                new VideoInformation(new MediaIdentifier(item.Id, item.Title, null), null)
            );
            navVM.NavigateToOver(typeof(VideoPlayerPage), snapshot);
        }

        ItemsFlyout.Hide();
    }

    private void OnRemoveItemClick(object sender, RoutedEventArgs e)
    {
        bool isLastItem = ViewModel.Items.Count == 1;
        if (sender is FrameworkElement { DataContext: PinItem item })
            ViewModel.RemoveItem(item);
        if (isLastItem)
        {
            ItemsFlyout.Hide();
        }
    }
}

/// <summary>
/// 固定器基类.
/// </summary>
public abstract class PinnerBase : LayoutUserControlBase<PinnerViewModel>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PinnerBase"/> class.
    /// </summary>
    protected PinnerBase()
    {
        ViewModel = Ioc.Default.GetRequiredService<PinnerViewModel>();
    }
}

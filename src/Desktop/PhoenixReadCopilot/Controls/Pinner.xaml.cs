using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using PhoenixReadCopilot.Core.Contracts.Services;
using PhoenixReadCopilot.Core.Enums;
using PhoenixReadCopilot.Core.Models.Media;
using PhoenixReadCopilot.Core.Models.Pin;
using PhoenixReadCopilot.Core.Models.User;
using PhoenixReadCopilot.Pages;
using PhoenixReadCopilot.Pages.Overlay;
using PhoenixReadCopilot.ViewModels.Core;

namespace PhoenixReadCopilot.Controls;

/// <summary>
/// 固定器.
/// </summary>
internal sealed partial class Pinner : UserControl
{
    private readonly INavigationService _navigationService =
        Ioc.Default.GetRequiredService<INavigationService>();

    public PinnerViewModel ViewModel { get; } = Ioc.Default.GetRequiredService<PinnerViewModel>();

    /// <summary>
    /// Initializes a new instance of the <see cref="Pinner"/> class.
    /// </summary>
    public Pinner()
    {
        InitializeComponent();
        Loaded += (_, _) => _ = ViewModel.InitializeAsync();
        Unloaded += (_, _) => Repeater.ItemsSource = null;
    }

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
        {
            ViewModel.RemoveItem(item);
        }

        if (isLastItem)
        {
            ItemsFlyout.Hide();
        }
    }
}

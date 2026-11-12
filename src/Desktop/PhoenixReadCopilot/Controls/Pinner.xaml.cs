using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.UI.Xaml;
using PhoenixReadCopilot.Core.Enums;
using PhoenixReadCopilot.Core.Models.Media;
using PhoenixReadCopilot.Core.Models.Pin;
using PhoenixReadCopilot.Core.Models.User;
using PhoenixReadCopilot.Pages.Overlay;
using PhoenixReadCopilot.ViewModels.Core;
using WinUIKernel.Share.Base;

namespace PhoenixReadCopilot.Controls;

/// <summary>
/// 固定器.
/// </summary>
public sealed partial class Pinner : PinnerBase
{
    private readonly NavigationViewModel _navigationViewModel =
        Ioc.Default.GetRequiredService<NavigationViewModel>();

    /// <summary>
    /// Initializes a new instance of the <see cref="Pinner"/> class.
    /// </summary>
    public Pinner()
    {
        InitializeComponent();
    }

    /// <inheritdoc/>
    protected override void OnControlLoaded() => ViewModel.InitializeCommand.Execute(null);

    protected override void OnControlUnloaded() => Repeater.ItemsSource = null;

    private void OnItemClick(object sender, RoutedEventArgs e)
    {
        PinItem? item = (sender as FrameworkElement)?.DataContext as PinItem;
        if (item is null)
        {
            return;
        }

        NavigationViewModel navVM = _navigationViewModel;
        if (item.Type == PinContentType.User)
        {
            User user = new()
            {
                Id = item.Id,
                Name = item.Title,
                Avatar = Uri.TryCreate(item.Cover, UriKind.Absolute, out Uri? coverUri)
                    ? new UserAvatar { Uri = coverUri }
                    : null,
            };
            navVM.NavigateToOver(typeof(UserSpacePage), user);
        }
        else if (item.Type == PinContentType.Video)
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
        ViewModel.RemoveItemCommand.Execute((sender as FrameworkElement)?.DataContext as PinItem);
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

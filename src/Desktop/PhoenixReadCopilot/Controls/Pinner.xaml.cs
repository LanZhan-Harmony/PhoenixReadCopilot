using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.UI.Xaml;
using PhoenixReadCopilot.Core.Enums;
using PhoenixReadCopilot.Core.Models;
using PhoenixReadCopilot.Pages.Overlay;
using PhoenixReadCopilot.ViewModels.Core;
using Richasy.BiliKernel.Models.Appearance;
using Richasy.BiliKernel.Models.Article;
using Richasy.BiliKernel.Models.Media;
using Richasy.BiliKernel.Models.User;
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
    public Pinner() => InitializeComponent();

    /// <inheritdoc/>
    protected override void OnControlLoaded() => ViewModel.InitializeCommand.Execute(null);

    protected override void OnControlUnloaded() => Repeater.ItemsSource = null;

    private void OnItemClick(object sender, RoutedEventArgs e)
    {
        PinItem? item = (sender as FrameworkElement)?.DataContext as PinItem;
        NavigationViewModel navVM = _navigationViewModel;
        if (item.Type == PinContentType.User)
        {
            UserProfile user = new(item.Id, item.Title, new BiliImage(new Uri(item.Cover)));
            navVM.NavigateToOver(typeof(UserSpacePage), user);
        }
        else if (item.Type == PinContentType.Pgc)
        {
            MediaIdentifier identifier = new(item.Id, item.Title, null);
            navVM.NavigateToOver(typeof(PgcPlayerPage), identifier);
        }
        else if (item.Type == PinContentType.Video)
        {
            VideoSnapshot snapshot = new(
                new VideoInformation(new MediaIdentifier(item.Id, item.Title, null), null)
            );
            navVM.NavigateToOver(typeof(VideoPlayerPage), snapshot);
        }
        else if (item.Type == PinContentType.Article)
        {
            ArticleIdentifier identifier = new(item.Id, item.Title, null, null);
            navVM.NavigateToOver(typeof(ArticleReaderPage), identifier);
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
    protected PinnerBase() => ViewModel = Ioc.Default.GetRequiredService<PinnerViewModel>();
}

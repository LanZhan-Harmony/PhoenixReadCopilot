using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;
using PhoenixReadCopilot.Core.Enums;
using PhoenixReadCopilot.ViewModels.Items;
using Windows.Foundation;
using WinUIKernel.Share.Base;
using WinUIKernel.Share.Toolkits;

namespace PhoenixReadCopilot.Controls.Components;

/// <summary>
/// 剧集卡片展示器.
/// </summary>
public abstract class SeasonCardPresenter : LayoutUserControlBase<SeasonItemViewModel>;

/// <summary>
/// 剧集卡片控件.
/// </summary>
public sealed partial class SeasonCardControl : LayoutControlBase<SeasonItemViewModel>
{
    private CardControl _rootCard;

    /// <summary>
    /// Initializes a new instance of the <see cref="SeasonCardControl"/> class.
    /// </summary>
    public SeasonCardControl()
    {
        DefaultStyleKey = typeof(SeasonCardControl);
    }

    /// <inheritdoc/>
    protected override void OnApplyTemplate()
    {
        _rootCard = GetTemplateChild("RootCard") as CardControl;
        if (ViewModel is not null)
        {
            _rootCard?.Command = ViewModel.PlayCommand;
        }
    }

    /// <inheritdoc/>
    protected override void OnControlLoaded() => ContextRequested += OnContextRequested;

    /// <inheritdoc/>
    protected override void OnControlUnloaded() => ContextRequested -= OnContextRequested;

    /// <inheritdoc/>
    protected override void OnViewModelChanged(
        SeasonItemViewModel? oldValue,
        SeasonItemViewModel? newValue
    )
    {
        _rootCard?.Command = newValue?.PlayCommand;
    }

    private static MenuFlyoutItem CreateOpenInNewWindowItem()
    {
        return new MenuFlyoutItem()
        {
            Text = Strings.Resources.OpenInNewWindow,
            Icon = new FluentIcons.WinUI.SymbolIcon
            {
                Symbol = FluentIcons.Common.Symbol.WindowPlay,
            },
            Tag = nameof(ViewModel.OpenInNewWindowCommand),
        };
    }

    private static MenuFlyoutItem CreateFollowItem()
    {
        return new MenuFlyoutItem()
        {
            Text = Strings.Resources.Follow,
            Icon = new FluentIcons.WinUI.SymbolIcon { Symbol = FluentIcons.Common.Symbol.Heart },
            Tag = nameof(ViewModel.FollowCommand),
        };
    }

    private static MenuFlyoutItem CreatePinItem()
    {
        return new MenuFlyoutItem()
        {
            Text = Strings.Resources.FixContent,
            Icon = new FluentIcons.WinUI.SymbolIcon { Symbol = FluentIcons.Common.Symbol.Pin },
            Tag = nameof(ViewModel.PinCommand),
        };
    }

    private MenuFlyoutItem CreateUnfollowItem()
    {
        return new MenuFlyoutItem()
        {
            Text = Strings.Resources.Unfollow,
            Icon = new FluentIcons.WinUI.SymbolIcon
            {
                Symbol = FluentIcons.Common.Symbol.HeartBroken,
                Foreground = Ioc
                    .Default.GetRequiredService<IResourceToolkit>()
                    .GetThemeBrush("SystemFillColorCriticalBrush"),
            },
            Tag = nameof(ViewModel.UnfollowCommand),
        };
    }

    private void OnContextRequested(UIElement sender, ContextRequestedEventArgs args)
    {
        if (ContextFlyout is null)
        {
            CreateContextFlyout();
            args.TryGetPosition(this, out Point point);
            ContextFlyout.ShowAt(this, new FlyoutShowOptions { Position = point });
        }

        RelocateCommands();
        args.Handled = true;
    }

    private void CreateContextFlyout()
    {
        MenuFlyout menuFlyout = new() { ShouldConstrainToRootBounds = false };
        menuFlyout.Items.Add(CreateOpenInNewWindowItem());
        menuFlyout.Items.Add(CreatePinItem());
        menuFlyout.Items.Add(CreateUnfollowItem());

        ContextFlyout = menuFlyout;
    }

    private void RelocateCommands()
    {
        if (ContextFlyout is not MenuFlyout flyout)
        {
            return;
        }

        foreach (MenuFlyoutItem item in flyout.Items.OfType<MenuFlyoutItem>())
        {
            switch (item.Tag.ToString())
            {
                case nameof(ViewModel.OpenInNewWindowCommand):
                    item.Command = ViewModel.OpenInNewWindowCommand;
                    break;
                case nameof(ViewModel.FollowCommand):
                    item.Command = ViewModel.FollowCommand;
                    break;
                case nameof(ViewModel.PinCommand):
                    item.Command = ViewModel.PinCommand;
                    break;
                case nameof(ViewModel.UnfollowCommand):
                    item.Command = ViewModel.UnfollowCommand;
                    break;
            }
        }
    }
}

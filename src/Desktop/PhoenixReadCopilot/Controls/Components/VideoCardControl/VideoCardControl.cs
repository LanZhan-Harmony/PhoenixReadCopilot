using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using PhoenixReadCopilot.Core.Enums;
using PhoenixReadCopilot.ViewModels.Items;
using Windows.Foundation;
using WinUIKernel.Share.Base;
using WinUIKernel.Share.Toolkits;

namespace PhoenixReadCopilot.Controls.Components;

/// <summary>
/// 视频卡片展示器.
/// </summary>
public abstract class VideoCardPresenter : LayoutUserControlBase<VideoItemViewModel>;

/// <summary>
/// 视频卡片控件.
/// </summary>
public sealed partial class VideoCardControl : LayoutControlBase<VideoItemViewModel>
{
    private CardControl? _rootCard;
    private VideoCardPresenter? _presenter;

    /// <summary>
    /// Initializes a new instance of the <see cref="VideoCardControl"/> class.
    /// </summary>
    public VideoCardControl() => DefaultStyleKey = typeof(VideoCardControl);

    /// <inheritdoc/>
    protected override void OnApplyTemplate()
    {
        base.OnApplyTemplate();
        _rootCard = GetTemplateChild("RootCard") as CardControl;
        _presenter =
            (_rootCard?.Content as VideoCardPresenter)
            ?? (GetTemplateChild("Presenter") as VideoCardPresenter);

        if (_rootCard is not null && ViewModel is not null)
        {
            _rootCard.Command = ViewModel.PlayCommand;
        }

        if (_presenter is not null && ViewModel is not null)
        {
            _presenter.ViewModel = ViewModel;
        }
    }

    /// <inheritdoc/>
    protected override void OnControlLoaded() => ContextRequested += OnContextRequested;

    /// <inheritdoc/>
    protected override void OnControlUnloaded() => ContextRequested -= OnContextRequested;

    /// <inheritdoc/>
    protected override void OnViewModelChanged(
        VideoItemViewModel? oldValue,
        VideoItemViewModel? newValue
    )
    {
        base.OnViewModelChanged(oldValue, newValue);
        _rootCard?.Command = newValue?.PlayCommand;

        if (_presenter is null && _rootCard is not null)
        {
            _presenter =
                (_rootCard.Content as VideoCardPresenter)
                ?? (GetTemplateChild("Presenter") as VideoCardPresenter);
        }

        _presenter?.ViewModel = newValue;
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

    private static MenuFlyoutItem CreateAddViewLaterItem()
    {
        return new MenuFlyoutItem()
        {
            Text = Strings.Resources.AddToViewLater,
            Icon = new FluentIcons.WinUI.SymbolIcon
            {
                Symbol = FluentIcons.Common.Symbol.CalendarAdd,
            },
            Tag = nameof(ViewModel.AddToViewLaterCommand),
        };
    }

    private static Brush? GetCriticalBrush()
    {
        if (Ioc.Default.GetService<IResourceToolkit>() is { } toolkit)
        {
            return toolkit.GetThemeBrush("SystemFillColorCriticalBrush");
        }

        return Application.Current.Resources.TryGetValue(
            "SystemFillColorCriticalBrush",
            out object? brush
        )
            ? brush as Brush
            : null;
    }

    private MenuFlyoutItem CreateRemoveViewLaterItem()
    {
        return new MenuFlyoutItem()
        {
            Text = Strings.Resources.Remove,
            Icon = new FluentIcons.WinUI.SymbolIcon
            {
                Symbol = FluentIcons.Common.Symbol.Delete,
                Foreground = GetCriticalBrush(),
            },
            Tag = nameof(ViewModel.RemoveViewLaterCommand),
        };
    }

    private MenuFlyoutItem CreateRemoveHistoryItem()
    {
        return new MenuFlyoutItem()
        {
            Text = Strings.Resources.Remove,
            Icon = new FluentIcons.WinUI.SymbolIcon
            {
                Symbol = FluentIcons.Common.Symbol.Delete,
                Foreground = GetCriticalBrush(),
            },
            Tag = nameof(ViewModel.RemoveHistoryCommand),
        };
    }

    private MenuFlyoutItem CreateRemoveFavoriteItem()
    {
        return new MenuFlyoutItem()
        {
            Text = Strings.Resources.Remove,
            Icon = new FluentIcons.WinUI.SymbolIcon
            {
                Symbol = FluentIcons.Common.Symbol.Delete,
                Foreground = GetCriticalBrush(),
            },
            Tag = nameof(ViewModel.RemoveFavoriteCommand),
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
        if (ViewModel.Style != VideoCardStyle.ViewLater)
        {
            menuFlyout.Items.Add(CreateAddViewLaterItem());
        }

        menuFlyout.Items.Add(CreatePinItem());
        if (ViewModel.Style == VideoCardStyle.ViewLater)
        {
            menuFlyout.Items.Add(CreateRemoveViewLaterItem());
        }
        else if (ViewModel.Style == VideoCardStyle.History)
        {
            menuFlyout.Items.Add(CreateRemoveHistoryItem());
        }
        else if (ViewModel.Style == VideoCardStyle.Favorite)
        {
            menuFlyout.Items.Add(CreateRemoveFavoriteItem());
        }

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
                case nameof(ViewModel.AddToViewLaterCommand):
                    item.Command = ViewModel.AddToViewLaterCommand;
                    break;
                case nameof(ViewModel.RemoveViewLaterCommand):
                    item.Command = ViewModel.RemoveViewLaterCommand;
                    break;
                case nameof(ViewModel.RemoveHistoryCommand):
                    item.Command = ViewModel.RemoveHistoryCommand;
                    break;
                case nameof(ViewModel.RemoveFavoriteCommand):
                    item.Command = ViewModel.RemoveFavoriteCommand;
                    break;
                case nameof(ViewModel.OpenInNewWindowCommand):
                    item.Command = ViewModel.OpenInNewWindowCommand;
                    break;
                case nameof(ViewModel.PinCommand):
                    item.Command = ViewModel.PinCommand;
                    break;
                default:
                    break;
            }
        }
    }
}

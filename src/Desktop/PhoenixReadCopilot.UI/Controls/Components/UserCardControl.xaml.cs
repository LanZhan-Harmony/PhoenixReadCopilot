using System.ComponentModel;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;
using PhoenixReadCopilot.UI.ViewModels.Items;
using WinUIKernel.Share.Base;

namespace PhoenixReadCopilot.UI.Controls.Components;

/// <summary>
/// 用户卡片.
/// </summary>
public sealed partial class UserCardControl : UserCardControlBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="UserCardControl"/> class.
    /// </summary>
    public UserCardControl()
    {
        this.InitializeComponent();
    }

    /// <inheritdoc/>
    protected override void OnApplyTemplate()
    {
        CheckFollowState();
    }

    /// <inheritdoc/>
    protected override void OnControlLoaded()
    {
        ContextRequested += OnContextRequested;
        CheckFollowState();
    }

    /// <inheritdoc/>
    protected override void OnControlUnloaded()
    {
        ContextRequested -= OnContextRequested;
        ViewModel?.PropertyChanged -= OnViewModelPropertyChanged;
    }

    /// <inheritdoc/>
    protected override void OnViewModelChanged(
        UserItemViewModel? oldValue,
        UserItemViewModel? newValue
    )
    {
        oldValue?.PropertyChanged -= OnViewModelPropertyChanged;

        newValue?.PropertyChanged += OnViewModelPropertyChanged;
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

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(ViewModel.IsFollowed))
        {
            CheckFollowState();
        }
    }

    private void CheckFollowState()
    {
        FollowButton?.IsChecked = ViewModel?.IsFollowed;

        FollowIcon?.IconVariant =
            ViewModel?.IsFollowed == true
                ? FluentIcons.Common.IconVariant.Filled
                : FluentIcons.Common.IconVariant.Regular;
    }

    private void OnContextRequested(UIElement sender, ContextRequestedEventArgs args)
    {
        if (ContextFlyout is null)
        {
            CreateContextFlyout();
            args.TryGetPosition(this, out var point);
            ContextFlyout.ShowAt(this, new FlyoutShowOptions { Position = point });
        }

        RelocateCommands();
        args.Handled = true;
    }

    private void CreateContextFlyout()
    {
        var menuFlyout = new MenuFlyout() { ShouldConstrainToRootBounds = false };
        menuFlyout.Items.Add(CreatePinItem());
        ContextFlyout = menuFlyout;
    }

    private void RelocateCommands()
    {
        if (ContextFlyout is not MenuFlyout flyout)
        {
            return;
        }

        foreach (var item in flyout.Items.OfType<MenuFlyoutItem>())
        {
            switch (item.Tag.ToString())
            {
                case nameof(ViewModel.PinCommand):
                    item.Command = ViewModel.PinCommand;
                    break;
            }
        }
    }
}

/// <summary>
/// 用户卡片.
/// </summary>
public abstract class UserCardControlBase : LayoutUserControlBase<UserItemViewModel>;

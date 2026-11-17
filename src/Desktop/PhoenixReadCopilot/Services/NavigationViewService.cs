using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using Microsoft.UI.Xaml.Controls;
using PhoenixReadCopilot.Contracts.Services;
using PhoenixReadCopilot.Core.Contracts.Services;
using PhoenixReadCopilot.Core.Models.Settings;
using PhoenixReadCopilot.Pages;
using PhoenixReadCopilot.Strings;
using WinUIKernel.Share.Base;
using WinUIKernel.Share.ViewModels;

namespace PhoenixReadCopilot.Services;

/// <summary>
/// 导航视图条目管理服务实现.
/// </summary>
public sealed class NavigationViewService : INavigationViewService
{
    private readonly INavigationService _navigationService;
    private readonly AppSettings _settings;
    private NavigationView? _navigationView;

    /// <inheritdoc/>
    public IReadOnlyList<AppNavigationItemViewModel> MenuItems { get; }

    /// <inheritdoc/>
    public ObservableCollection<AppNavigationItemViewModel> FooterItems { get; } = [];

    public NavigationViewService(
        INavigationService navigationService,
        ISettingsService settingsService
    )
    {
        _navigationService = navigationService;
        _settings = settingsService.Settings;

        MenuItems = CreateMenuItems();
        foreach (AppNavigationItemViewModel item in CreateFooterItems())
        {
            FooterItems.Add(item);
        }
    }

    /// <inheritdoc/>
    [MemberNotNull(nameof(_navigationView))]
    public void Initialize(NavigationView navigationView)
    {
        _navigationView = navigationView;
        _navigationView.BackRequested += OnBackRequested;
        _navigationView.ItemInvoked += OnItemInvoked;
        _navigationService.Navigated += OnNavigated;
    }

    /// <inheritdoc/>
    public void UnregisterEvents()
    {
        if (_navigationView != null)
        {
            _navigationView.BackRequested -= OnBackRequested;
            _navigationView.ItemInvoked -= OnItemInvoked;
        }

        _navigationService.Navigated -= OnNavigated;
    }

    /// <inheritdoc/>
    public AppNavigationItemViewModel? GetSelectedItem(Type pageType)
    {
        return MenuItems.FirstOrDefault(p => p.PageKey == pageType.FullName)
            ?? FooterItems.FirstOrDefault(p => p.PageKey == pageType.FullName);
    }

    /// <inheritdoc/>
    public void SetNavItemVisibility(Type pageType, bool isVisible)
    {
        SetNavItemVisibility(pageType.FullName ?? string.Empty, isVisible);
    }

    /// <inheritdoc/>
    public void SetNavItemVisibility(string pageKey, bool isVisible)
    {
        AppNavigationItemViewModel? item = MenuItems.FirstOrDefault(p => p.PageKey == pageKey);
        if (item != null)
        {
            item.IsVisible = isVisible;
        }
    }

    private void OnBackRequested(NavigationView sender, NavigationViewBackRequestedEventArgs args)
    {
        _navigationService.GoBack();
    }

    private void OnItemInvoked(NavigationView sender, NavigationViewItemInvokedEventArgs args)
    {
        AppNavigationViewItem? item = args.InvokedItemContainer as AppNavigationViewItem;
        AppNavigationItemViewModel? context = item?.Tag as AppNavigationItemViewModel
            ?? args.InvokedItem as AppNavigationItemViewModel;
        context?.NavigateCommand.Execute(null);
    }

    private void OnNavigated(object? sender, Type pageType)
    {
        AppNavigationItemViewModel? item = GetSelectedItem(pageType);
        if (item != null)
        {
            foreach (AppNavigationItemViewModel m in MenuItems)
            {
                m.IsSelected = (m == item);
            }

            foreach (AppNavigationItemViewModel f in FooterItems)
            {
                f.IsSelected = (f == item);
            }

            if (_navigationView != null)
            {
                _navigationView.SelectedItem = item;
            }
        }
    }

    private IReadOnlyList<AppNavigationItemViewModel> CreateMenuItems()
    {
        string? lastSelectedPage = string.IsNullOrEmpty(_settings.LastSelectedFeaturePage)
            ? typeof(HomePage).FullName
            : _settings.LastSelectedFeaturePage;

        List<AppNavigationItemViewModel> list =
        [
            GetItem<HomePage>(Resources.Home, FluentIcons.Common.Symbol.Home),
            GetItem<RankPage>(Resources.Rank, FluentIcons.Common.Symbol.Rocket),
            GetItem<MomentPage>(Resources.DynamicFeed, FluentIcons.Common.Symbol.DesignIdeas),
            GetItem<CalendarPage>(Resources.Calendar, FluentIcons.Common.Symbol.Calendar),
            GetItem<FilterPage>(Resources.Filter, FluentIcons.Common.Symbol.SlideSearch),
        ];

        foreach (AppNavigationItemViewModel item in list)
        {
            item.IsSelected = item.PageKey == lastSelectedPage && item.IsVisible;
        }

        if (!list.Any(p => p.IsSelected) && list.Any(p => p.IsVisible))
        {
            list.First(p => p.IsVisible).IsSelected = true;
        }

        return list;
    }

    private IReadOnlyList<AppNavigationItemViewModel> CreateFooterItems()
    {
        List<AppNavigationItemViewModel> list =
        [
            GetItem<SettingsPage>(Resources.Settings, FluentIcons.Common.Symbol.Settings),
        ];
        return list;
    }

    private AppNavigationItemViewModel GetItem<TPage>(
        string title,
        FluentIcons.Common.Symbol symbol,
        bool isSelected = false
    )
        where TPage : Page
    {
        bool isVisible = _settings.GetPageVisible(typeof(TPage));
        return new AppNavigationItemViewModel(
            (INavServiceViewModel)_navigationService,
            typeof(TPage),
            title,
            symbol,
            isSelected,
            isVisible: isVisible
        );
    }
}

using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using Microsoft.UI.Xaml.Controls;
using PhoenixReadCopilot.Contracts.Services;
using PhoenixReadCopilot.Controls.Components;
using PhoenixReadCopilot.Core.Contracts.Services;
using PhoenixReadCopilot.Core.Models.Settings;
using PhoenixReadCopilot.Pages;
using PhoenixReadCopilot.Strings;
using PhoenixReadCopilot.ViewModels.Items;

namespace PhoenixReadCopilot.Services;

/// <summary>
/// 导航视图条目管理服务实现.
/// </summary>
internal sealed class NavigationViewService : INavigationViewService
{
    private readonly INavigationService _navigationService;
    private readonly AppSettings _settings;
    private NavigationView? _navigationView;

    /// <inheritdoc/>
    public ObservableCollection<AppNavigationItemViewModel> MenuItems { get; } = [];

    /// <inheritdoc/>
    public ObservableCollection<AppNavigationItemViewModel> FooterItems { get; } = [];

    public NavigationViewService(
        INavigationService navigationService,
        ISettingsService settingsService
    )
    {
        _navigationService = navigationService;
        _settings = settingsService.Settings;

        foreach (AppNavigationItemViewModel item in CreateMenuItems())
        {
            MenuItems.Add(item);
        }

        foreach (AppNavigationItemViewModel item in CreateFooterItems())
        {
            FooterItems.Add(item);
        }

        _settings.PropertyChanged += OnSettingsPropertyChanged;
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
        _settings.PropertyChanged -= OnSettingsPropertyChanged;
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

            if (!isVisible && item.IsSelected)
            {
                AppNavigationItemViewModel? firstVisible = MenuItems.FirstOrDefault(p =>
                    p.IsVisible
                );
                firstVisible?.NavigateCommand.Execute(null);
            }
        }
    }

    private void OnSettingsPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is null)
        {
            return;
        }

        Action? action = e.PropertyName switch
        {
            nameof(AppSettings.IsHomePageVisible) => () =>
                SetNavItemVisibility(typeof(HomePage), _settings.IsHomePageVisible),
            nameof(AppSettings.IsRankPageVisible) => () =>
                SetNavItemVisibility(typeof(RankPage), _settings.IsRankPageVisible),
            nameof(AppSettings.IsCalendarPageVisible) => () =>
                SetNavItemVisibility(typeof(CalendarPage), _settings.IsCalendarPageVisible),
            nameof(AppSettings.IsFilterPageVisible) => () =>
                SetNavItemVisibility(typeof(FilterPage), _settings.IsFilterPageVisible),
            _ => null,
        };

        if (action != null)
        {
            if (_navigationView?.DispatcherQueue is { } dq && !dq.HasThreadAccess)
            {
                dq.TryEnqueue(() => action());
            }
            else
            {
                action();
            }
        }
    }

    private void OnBackRequested(NavigationView sender, NavigationViewBackRequestedEventArgs args)
    {
        _navigationService.GoBack();
    }

    private void OnItemInvoked(NavigationView sender, NavigationViewItemInvokedEventArgs args)
    {
        AppNavigationViewItem? item = args.InvokedItemContainer as AppNavigationViewItem;
        AppNavigationItemViewModel? context =
            item?.Tag as AppNavigationItemViewModel
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

            _navigationView?.SelectedItem = item;
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
            GetItem<UserSpacePage>(Resources.PersonalSpace, FluentIcons.Common.Symbol.Person),
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

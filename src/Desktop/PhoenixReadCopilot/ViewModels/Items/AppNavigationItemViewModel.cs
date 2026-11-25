using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PhoenixReadCopilot.Core.Contracts.Services;
using Symbol = FluentIcons.Common.Symbol;

namespace PhoenixReadCopilot.ViewModels.Items;

/// <summary>
/// 应用导航视图项视图模型.
/// </summary>
internal sealed partial class AppNavigationItemViewModel(
    INavigationService navService,
    Type pageType,
    string title,
    Symbol? symbol = null,
    bool isSelected = false,
    string accessKey = "",
    bool isVisible = true
) : ViewModelBase
{
    private readonly Action<object?> _navigateAction = parameter =>
        navService.NavigateTo(pageType, parameter);

    [ObservableProperty]
    public partial bool ShowUnread { get; set; }

    [ObservableProperty]
    public partial Symbol Symbol { get; set; } = symbol ?? Symbol.Circle;

    [ObservableProperty]
    public partial bool IsSelected { get; set; } = isSelected;

    [ObservableProperty]
    public partial string Title { get; set; } = title;

    [ObservableProperty]
    public partial string AccessKey { get; set; } = accessKey;

    [ObservableProperty]
    public partial bool IsVisible { get; set; } = isVisible;

    /// <summary>
    /// 页面标识.
    /// </summary>
    public string PageKey { get; } = pageType.FullName ?? pageType.Name;

    /// <inheritdoc/>
    public override bool Equals(object? obj) =>
        obj is AppNavigationItemViewModel model && PageKey == model.PageKey;

    /// <inheritdoc/>
    public override int GetHashCode() => HashCode.Combine(PageKey);

    [RelayCommand]
    private void Navigate(object? parameter = null) => _navigateAction?.Invoke(parameter);
}

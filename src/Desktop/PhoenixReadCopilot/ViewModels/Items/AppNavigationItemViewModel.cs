using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PhoenixReadCopilot.Core.Contracts.Services;
using Symbol = FluentIcons.Common.Symbol;

namespace PhoenixReadCopilot.ViewModels.Items;

/// <summary>
/// 应用导航视图项视图模型.
/// </summary>
internal sealed partial class AppNavigationItemViewModel : ViewModelBase
{
    private readonly Action<object?> _navigateAction;

    [ObservableProperty]
    public partial bool ShowUnread { get; set; }

    [ObservableProperty]
    public partial Symbol Symbol { get; set; }

    [ObservableProperty]
    public partial bool IsSelected { get; set; }

    [ObservableProperty]
    public partial string Title { get; set; }

    [ObservableProperty]
    public partial string AccessKey { get; set; }

    [ObservableProperty]
    public partial bool IsVisible { get; set; }

    public AppNavigationItemViewModel(
        INavigationService navService,
        Type pageType,
        string title,
        Symbol? symbol = null,
        bool isSelected = false,
        string accessKey = "",
        bool isVisible = true
    )
    {
        PageKey = pageType.FullName;
        Title = title;
        Symbol = symbol ?? Symbol.Circle;
        IsSelected = isSelected;
        AccessKey = accessKey;
        _navigateAction = parameter => navService.NavigateTo(pageType, parameter);
        IsVisible = isVisible;
    }

    /// <summary>
    /// 页面标识.
    /// </summary>
    public string PageKey { get; }

    /// <inheritdoc/>
    public override bool Equals(object? obj) =>
        obj is AppNavigationItemViewModel model && PageKey == model.PageKey;

    /// <inheritdoc/>
    public override int GetHashCode() => HashCode.Combine(PageKey);

    [RelayCommand]
    private void Navigate(object? parameter = null) => _navigateAction?.Invoke(parameter);
}

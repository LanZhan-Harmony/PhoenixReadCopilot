using CommunityToolkit.Mvvm.Input;
using Microsoft.UI.Dispatching;
using PhoenixReadCopilot.Pages.Overlay;

namespace PhoenixReadCopilot.ViewModels.View;

public sealed partial class FindDramaPageViewModel
{
    [RelayCommand]
    private async Task InitializeAsync() => await RequestFilterPanelAsync();

    [RelayCommand]
    private Task Refresh()
    {
        _offset = 0;
        _hasMore = true;
        _dispatcherQueue.TryEnqueue(
            DispatcherQueuePriority.Low,
            async () =>
            {
                Items.Clear();
                Filters = null;
                await InitializeAsync();
            }
        );

        return Task.CompletedTask;
    }

    [RelayCommand]
    private void ShowMyFavoriteFindDrama()
    {
        _settings.LastSelectedFavoriteSection = "drama";
        _navigationViewModel.NavigateToOver(typeof(FavoritesPage));
    }
}

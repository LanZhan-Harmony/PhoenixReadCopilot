using CommunityToolkit.Mvvm.Input;
using PhoenixReadCopilot.Pages.Overlay;
using PhoenixReadCopilot.ViewModels.Items;

namespace PhoenixReadCopilot.ViewModels.View;

public sealed partial class AnimePageViewModel
{
    [RelayCommand]
    private async Task InitializeAsync() => await RequestDailyReleasesAsync();

    [RelayCommand]
    private async Task RefreshAsync()
    {
        _dateCache.Clear();
        Timelines.Clear();
        SelectedTimeline = null;
        _isInitialized = false;
        IsError = false;
        UpdateVisibilityStates();
        await RequestDailyReleasesAsync();
    }

    [RelayCommand]
    private void SelectTimeline(TimelineItemViewModel vm)
    {
        if (vm is null)
        {
            return;
        }

        SelectedTimeline = vm;

        if (vm.Items == null)
        {
            if (_dateCache.TryGetValue(vm.TargetDate, out List<SeasonItemViewModel>? cached))
            {
                vm.Items = cached;
                vm.Count = cached.Count;
                vm.IsEmpty = cached.Count == 0;
                UpdateVisibilityStates();
            }
            else
            {
                _ = LoadDateReleasesAsync(vm);
            }
        }
        else
        {
            UpdateVisibilityStates();
        }
    }

    [RelayCommand]
    private void ShowMyFavoriteAnime()
    {
        _settings.LastSelectedFavoriteSection = "drama";
        _navigationViewModel.NavigateToOver(typeof(FavoritesPage));
    }
}

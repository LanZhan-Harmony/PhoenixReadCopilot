using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using PhoenixReadCopilot.Core.Enums;
using PhoenixReadCopilot.ViewModels.Items;
using Richasy.BiliKernel.Bili.User;
using Richasy.BiliKernel.Models;
using Richasy.BiliKernel.Models.Media;
using WinUIKernel.Share.ViewModels;

namespace PhoenixReadCopilot.ViewModels.Components;

/// <summary>
/// 追番分区详情视图模型.
/// </summary>
public sealed partial class PgcFavoriteSectionDetailViewModel
    : ViewModelBase,
        IFavoriteSectionDetailViewModel
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PgcFavoriteSectionDetailViewModel"/> class.
    /// </summary>
    public PgcFavoriteSectionDetailViewModel(
        PgcFavoriteType type,
        IFavoriteService service,
        ILogger<PgcFavoriteSectionDetailViewModel> logger
    )
    {
        _service = service;
        _logger = logger;
        StatusList =
        [
            .. Enum.GetValues<PgcFavoriteStatus>(),
        ];
        Type = type;
    }

    [RelayCommand]
    private async Task TryFirstLoadAsync()
    {
        if (Items.Count > 0 || _cache.Count > 0)
        {
            return;
        }

        CurrentStatus = PgcFavoriteStatus.Watching;
        await LoadItemsAsync();
    }

    [RelayCommand]
    private async Task RefreshAsync()
    {
        IsEmpty = false;
        Items.Clear();
        _offsetCache.Clear();
        _cache.Clear();
        await LoadItemsAsync();
    }

    [RelayCommand]
    private async Task ChangeStatusAsync(PgcFavoriteStatus status)
    {
        if (CurrentStatus == status)
        {
            return;
        }

        _cache[CurrentStatus] =
        [
            .. Items,
        ];
        CurrentStatus = status;
        if (_cache.TryGetValue(status, out List<SeasonItemViewModel>? list))
        {
            Items.Clear();
            foreach (SeasonItemViewModel item in list)
            {
                Items.Add(item);
            }

            IsEmpty = Items.Count == 0;
            ListUpdated?.Invoke(this, EventArgs.Empty);
        }
        else
        {
            Items.Clear();
            await LoadItemsAsync();
        }
    }

    [RelayCommand]
    private async Task LoadItemsAsync()
    {
        (int pn, bool isPrevent) = _offsetCache.GetValueOrDefault(CurrentStatus, (0, false));
        if (IsEmpty || IsLoading || isPrevent)
        {
            return;
        }

        try
        {
            IsLoading = true;
            (IReadOnlyList<SeasonInformation>? seasons, int totalCount, int? nextPage) =
                await _service.GetPgcFavoritesAsync(Type, CurrentStatus, pn);
            bool isPreventLoadMore =
                seasons is null
                || nextPage == 0
                || (seasons.Count + Items.Count >= totalCount && totalCount > 0);
            _offsetCache[CurrentStatus] = (nextPage ?? 0, isPreventLoadMore);
            if (seasons is not null)
            {
                foreach (SeasonInformation item in seasons)
                {
                    Items.Add(
                        new SeasonItemViewModel(
                            item,
                            SeasonCardStyle.Favorite,
                            CurrentStatus,
                            RemoveSeason
                        )
                    );
                }
            }

            ListUpdated?.Invoke(this, EventArgs.Empty);
        }
        catch (Exception ex)
        {
            _offsetCache[CurrentStatus] = (pn, true);
            _logger.LogError(ex, $"获取{Type}列表时出错.");
        }
        finally
        {
            IsEmpty = Items.Count == 0;
            IsLoading = false;
        }
    }

    private void RemoveSeason(SeasonItemViewModel item)
    {
        Items.Remove(item);
        IsEmpty = Items.Count == 0;
        ListUpdated?.Invoke(this, EventArgs.Empty);
    }
}

using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.UI.Dispatching;
using PhoenixReadApi;
using PhoenixReadCopilot.Core.Contracts.Services;
using PhoenixReadCopilot.Core.Models.Settings;
using PhoenixReadCopilot.ViewModels.Components;
using PhoenixReadCopilot.ViewModels.Core;

namespace PhoenixReadCopilot.ViewModels.Items;

/// <summary>
/// 为大量复用的 ItemViewModel 提供静态统一服务访问.
/// </summary>
internal static class ItemServices
{
    public static INavigationService Navigation =
        Ioc.Default.GetRequiredService<INavigationService>();

    public static AppViewModel App = Ioc.Default.GetRequiredService<AppViewModel>();

    public static PinnerViewModel Pinner = Ioc.Default.GetRequiredService<PinnerViewModel>();

    public static PhoenixReadApiClient PhoenixRead =>
        Ioc.Default.GetRequiredService<PhoenixReadApiClient>();

    public static AppSettings Settings = Ioc
        .Default.GetRequiredService<ISettingsService>()
        .Settings;

    public static IViewLaterService ViewLater = Ioc.Default.GetRequiredService<IViewLaterService>();

    public static IPinService Pin => Ioc.Default.GetRequiredService<IPinService>();

    public static IReservationService Reservation =>
        Ioc.Default.GetRequiredService<IReservationService>();

    public static IFavoriteService Favorite =>
        Ioc.Default.GetRequiredService<IFavoriteService>();

    public static ISearchService Search => Ioc.Default.GetRequiredService<ISearchService>();

    public static IDanmakuService DanmakuService =>
        Ioc.Default.GetRequiredService<IDanmakuService>();

    public static IFollowService Follow => Ioc.Default.GetRequiredService<IFollowService>();

    public static ICommentService Comment => Ioc.Default.GetRequiredService<ICommentService>();

    public static IRankService Rank => Ioc.Default.GetRequiredService<IRankService>();

    public static ICalendarService Calendar => Ioc.Default.GetRequiredService<ICalendarService>();

    public static IFilterService Filter => Ioc.Default.GetRequiredService<IFilterService>();

    public static AccountViewModel Account = Ioc.Default.GetRequiredService<AccountViewModel>();

    public static DispatcherQueue DispatcherQueue =
        Ioc.Default.GetRequiredService<DispatcherQueue>();
}

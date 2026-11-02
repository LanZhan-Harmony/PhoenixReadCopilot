using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.UI.Dispatching;
using PhoenixReadCopilot.UI.Models.Settings;
using PhoenixReadCopilot.UI.Services;
using PhoenixReadCopilot.UI.ViewModels.Components;
using PhoenixReadCopilot.UI.ViewModels.Core;
using Richasy.BiliKernel.Bili.Authorization;
using Richasy.BiliKernel.Bili.Media;
using Richasy.BiliKernel.Bili.Moment;
using Richasy.BiliKernel.Bili.User;

namespace PhoenixReadCopilot.UI.ViewModels.Items;

/// <summary>
/// 为大量复用的 ItemViewModel 提供静态统一服务访问.
/// </summary>
internal static class ItemServices
{
    public static NavigationViewModel Navigation =
        Ioc.Default.GetRequiredService<NavigationViewModel>();

    public static AppViewModel App = Ioc.Default.GetRequiredService<AppViewModel>();

    public static PinnerViewModel Pinner = Ioc.Default.GetRequiredService<PinnerViewModel>();

    public static AppSettings Settings = Ioc
        .Default.GetRequiredService<ISettingsService>()
        .Settings;

    public static IViewLaterService ViewLater = Ioc.Default.GetRequiredService<IViewLaterService>();

    public static IViewHistoryService ViewHistory =
        Ioc.Default.GetRequiredService<IViewHistoryService>();

    public static IFavoriteService Favorite = Ioc.Default.GetRequiredService<IFavoriteService>();

    public static IRelationshipService Relationship =
        Ioc.Default.GetRequiredService<IRelationshipService>();

    public static IEntertainmentDiscoveryService Entertainment =
        Ioc.Default.GetRequiredService<IEntertainmentDiscoveryService>();

    public static IMomentOperationService MomentOperation =
        Ioc.Default.GetRequiredService<IMomentOperationService>();

    public static IBiliTokenResolver TokenResolver =
        Ioc.Default.GetRequiredService<IBiliTokenResolver>();

    public static AccountViewModel Account = Ioc.Default.GetRequiredService<AccountViewModel>();

    public static IMomentDiscoveryService MomentDiscovery =
        Ioc.Default.GetRequiredService<IMomentDiscoveryService>();

    public static DispatcherQueue DispatcherQueue =
        Ioc.Default.GetRequiredService<DispatcherQueue>();
}

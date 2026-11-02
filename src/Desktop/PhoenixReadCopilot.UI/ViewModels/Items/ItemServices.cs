using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.UI.Dispatching;
using PhoenixReadCopilot.UI.Models.Settings;
using PhoenixReadCopilot.UI.Services;
using PhoenixReadCopilot.UI.ViewModels.Components;
using PhoenixReadCopilot.UI.ViewModels.Core;
using PhoenixReadCopilot.UI.ViewModels.View;
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
    public static NavigationViewModel Navigation =>
        field ??= Ioc.Default.GetRequiredService<NavigationViewModel>();

    public static AppViewModel App => field ??= Ioc.Default.GetRequiredService<AppViewModel>();

    public static PinnerViewModel Pinner =>
        field ??= Ioc.Default.GetRequiredService<PinnerViewModel>();

    public static AppSettings Settings =>
        Ioc.Default.GetRequiredService<ISettingsService>().Settings;

    public static IViewLaterService ViewLater =>
        field ??= Ioc.Default.GetRequiredService<IViewLaterService>();

    public static IViewHistoryService ViewHistory =>
        field ??= Ioc.Default.GetRequiredService<IViewHistoryService>();

    public static IFavoriteService Favorite =>
        field ??= Ioc.Default.GetRequiredService<IFavoriteService>();

    public static IRelationshipService Relationship =>
        field ??= Ioc.Default.GetRequiredService<IRelationshipService>();

    public static IEntertainmentDiscoveryService Entertainment =>
        field ??= Ioc.Default.GetRequiredService<IEntertainmentDiscoveryService>();

    public static IMomentOperationService MomentOperation =>
        field ??= Ioc.Default.GetRequiredService<IMomentOperationService>();

    public static IBiliTokenResolver TokenResolver =>
        field ??= Ioc.Default.GetRequiredService<IBiliTokenResolver>();

    public static AccountViewModel Account =>
        field ??= Ioc.Default.GetRequiredService<AccountViewModel>();

    public static IMomentDiscoveryService MomentDiscovery =>
        field ??= Ioc.Default.GetRequiredService<IMomentDiscoveryService>();

    public static DispatcherQueue DispatcherQueue =>
        field ??= Ioc.Default.GetRequiredService<DispatcherQueue>();

    public static MomentPageViewModel MomentPage =>
        field ??= Ioc.Default.GetRequiredService<MomentPageViewModel>();
}

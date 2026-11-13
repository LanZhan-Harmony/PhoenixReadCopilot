using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.UI.Dispatching;
using PhoenixReadApi;
using PhoenixReadCopilot.Core.Models.Settings;
using PhoenixReadCopilot.Services;
using PhoenixReadCopilot.ViewModels.Components;
using PhoenixReadCopilot.ViewModels.Core;
using PhoenixReadCopilot.Core.Contracts.Services;

namespace PhoenixReadCopilot.ViewModels.Items;

/// <summary>
/// 为大量复用的 ItemViewModel 提供静态统一服务访问.
/// </summary>
internal static class ItemServices
{
    public static NavigationViewModel Navigation =
        Ioc.Default.GetRequiredService<NavigationViewModel>();

    public static AppViewModel App = Ioc.Default.GetRequiredService<AppViewModel>();

    public static PinnerViewModel Pinner = Ioc.Default.GetRequiredService<PinnerViewModel>();

    public static PhoenixReadApiClient PhoenixRead => Ioc.Default.GetRequiredService<PhoenixReadApiClient>();

    public static AppSettings Settings = Ioc
        .Default.GetRequiredService<ISettingsService>()
        .Settings;

    public static ILocalViewLaterService LocalViewLater = Ioc.Default.GetRequiredService<ILocalViewLaterService>();

    public static AccountViewModel Account = Ioc.Default.GetRequiredService<AccountViewModel>();

    public static DispatcherQueue DispatcherQueue =
        Ioc.Default.GetRequiredService<DispatcherQueue>();
}

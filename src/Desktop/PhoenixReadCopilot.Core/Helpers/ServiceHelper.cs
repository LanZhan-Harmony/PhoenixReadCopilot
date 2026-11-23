using System.Net;
using Microsoft.Extensions.DependencyInjection;
using PhoenixReadApi;
using PhoenixReadCopilot.Core.Contracts.Services;
using PhoenixReadCopilot.Core.Services;
using ZLogger;
using ZLogger.Providers;

namespace PhoenixReadCopilot.Core.Helpers;

public static class ServiceHelper
{
    public static void ConfigureCoreServices(IServiceCollection services)
    {
        services.AddLogging(logging =>
        {
            logging.AddZLoggerRollingFile(
                (options, sp) =>
                {
                    IPathService pathService = sp.GetRequiredService<IPathService>();
                    options.FilePathSelector = (dt, index) =>
                        Path.Combine(
                            pathService.LogFolderPath,
                            $"{dt.ToLocalTime():yyyy-MM-dd}_{index}.log"
                        );
                    options.RollingInterval = RollingInterval.Month;
                    options.RollingSizeKB = 1024;
                }
            );
        });

        services
            .AddHttpClient("PhoenixReadClient")
            .UseSocketsHttpHandler(
                (handler, _) =>
                {
                    handler.AutomaticDecompression = DecompressionMethods.All;
                    handler.PooledConnectionLifetime = TimeSpan.FromMinutes(15);
                    handler.PooledConnectionIdleTimeout = TimeSpan.FromMinutes(2);
                    handler.EnableMultipleHttp2Connections = true;
                }
            )
            .SetHandlerLifetime(Timeout.InfiniteTimeSpan);

        services.AddSingleton<IAuthenticationService, AuthenticationService>();
        services.AddSingleton<ICalendarService, CalendarService>();
        services.AddSingleton<ICommentService, CommentService>();
        services.AddSingleton<IDanmakuService, DanmakuService>();
        services.AddSingleton<IDownloadService, DownloadService>();
        services.AddSingleton<IDramaService, DramaService>();
        services.AddSingleton<IFavoriteService, FavoriteService>();
        services.AddSingleton<IFilterService, FilterService>();
        services.AddSingleton<IFollowService, FollowService>();
        services.AddSingleton<IHistoryService, HistoryService>();
        services.AddSingleton<ILikeService, LikeService>();
        services.AddSingleton<ISettingsService, SettingsService>();
        services.AddSingleton<IPinService, PinService>();
        services.AddSingleton<IPlaybackMediaService, PlaybackMediaService>();
        services.AddSingleton<IRankService, RankService>();
        services.AddSingleton<IRecommendService, RecommendService>();
        services.AddSingleton<IReservationService, ReservationService>();
        services.AddSingleton<ISearchService, SearchService>();
        services.AddSingleton<ISubtitleService, SubtitleService>();
        services.AddSingleton<IUserService, UserService>();
        services.AddSingleton<IViewLaterService, ViewLaterService>();

        services.AddSingleton<PhoenixReadApiClient>(sp =>
        {
            IPathService pathService = sp.GetRequiredService<IPathService>();
            string sessionPath = Path.Combine(pathService.AuthFolderPath, "phoenix_session.json");
            return new PhoenixReadApiClient(
                new PhoenixReadApiOptions
                {
                    AuthenticationStateFilePath = sessionPath,
                    RequestTimeout = TimeSpan.FromSeconds(25),
                    RetryCount = 2,
                }
            );
        });
    }
}

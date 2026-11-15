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

        services.AddSingleton<ISettingsService, SettingsService>();
        services.AddSingleton<IDownloadService, DownloadService>();

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

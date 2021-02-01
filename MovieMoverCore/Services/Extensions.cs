using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Policy;
using System.Threading.Tasks;

namespace MovieMoverCore.Services
{
    public static class Extensions
    {
        public static IServiceCollection UseSettings(this IServiceCollection services)
        {
            if (Environment.OSVersion.Platform == PlatformID.Win32NT)
            {
#if DEBUG
                return services.AddSingleton<ISettings, SettingsWinTesting>();
#endif
            }
            return services.AddSingleton<ISettings, Settings>();
        }

        public static IServiceCollection UsePlex(this IServiceCollection services, bool useMock = false)
        {
            if (useMock)
            {
                return services.AddSingleton<IPlex, PlexMock>();
            }
            return services.AddSingleton<IPlex, Plex>();
        }

        public static IServiceCollection UseFileMover(this IServiceCollection services, bool useMock = false)
        {
            if (useMock)
            {
                services.AddSingleton<IFileMoveWorker, FileWorkerMock>();
                return services.AddTransient<IFileMover, FilesMoverMock>();
            }
            services.AddSingleton<IFileMoveWorker, FileMoveWorker>();
            return services.AddTransient<IFileMover, FileMover>();
        }

        public static IServiceCollection UseDatabase(this IServiceCollection services, bool useMock = false)
        {
            if (useMock)
            {
                return services.AddSingleton<IDatabase, DBMock>();
            }
            return services.AddSingleton<IDatabase, DB>();
        }

        public static IServiceCollection UseEpGuide(this IServiceCollection services, bool useMock = false)
        {
            if (useMock)
            {
                return services.AddScoped<IEpGuide, EpGuideMock>();
            }
            return services.AddScoped<IEpGuide, EpGuidesCom>();
        }

        public static IServiceCollection UseSubtitles(this IServiceCollection services, bool useMock = false)
        {
            if (useMock)
            {
                return services.AddScoped<ISubtitles, SubtitlesMock>();
            }
            return services.AddScoped<ISubtitles, Addic7ed>();
        }

        public static IServiceCollection UseSeriesVideoSearcher(this IServiceCollection services, bool useMock = false)
        {
            if (useMock)
            {
                return services.AddScoped<ISeriesVideoSearcher, SeriesVideoSearcherMock>();
            }
            return services.AddScoped<ISeriesVideoSearcher, SeriesVideoSearcher>();
        }

        public static IServiceCollection UseCache(this IServiceCollection services, bool useMock = false)
        {
            if (useMock)
            {
                return services.AddSingleton(typeof(ICache<,,>), typeof(CacheMock<,,>));
            }
            return services.AddSingleton(typeof(ICache<,,>), typeof(Cache<,,>));
        }

        public static IServiceCollection UseJDownloader(this IServiceCollection services, bool useMock = false)
        {
            if (useMock)
            {
                return services.AddSingleton<IJDownloader, JDownloaderMock>();
            }
            return services.AddSingleton<IJDownloader, JDownloader>();
        }

        public static void FireForget<T>(this Task task, ILogger<T> logger)
        {
            task.ContinueWith(t => logger.LogWarning(t.Exception, "Fire-And-Forget action failed"), TaskContinuationOptions.OnlyOnFaulted);
        }
    }
}

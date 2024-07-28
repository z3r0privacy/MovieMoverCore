﻿using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using MovieMoverCore.Helpers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Policy;
using System.Threading.Tasks;

namespace MovieMoverCore.Services
{
    public static class Extensions
    {
        public static string GetFileNamePlatformIndependent(string path)
        {
            if (path.StartsWith("/"))
            {
                // JD runs on a *nix platform
                return path.Split('/', StringSplitOptions.RemoveEmptyEntries)[^1];
            } else if (path.Length > 2 && path[1]==':' && path[2] == '\\')
            {
                // JD runs on a Windows platform
                return path.Split('\\', StringSplitOptions.RemoveEmptyEntries)[^1];
            }
            // JD did not provide a full path, falling back to system's defaults
            return Path.GetFileName(path);
        }

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

        public static IServiceCollection UseMultimediaServerManagerCollection(this IServiceCollection services)
        {
            return services.AddSingleton<IMultimediaServerManagerCollection, MultimediaServerManagerCollection>();
        }

        public static IServiceCollection UseMultimediaMetadataProvider(this IServiceCollection services, bool useMock = false)
        {
            if (useMock)
            {
                return services.AddSingleton<IMultimediaMetadataProvider, PlexMock>();
            }
            return services.AddSingleton<IMultimediaMetadataProvider, Plex>();
        }

        public static IServiceCollection UseFileMover(this IServiceCollection services, bool useMock = false)
        {
            if (useMock)
            {
                services.AddSingleton<IFileOperationsWorker, FileWorkerMock>();
                return services.AddSingleton<IFileMover, FilesMoverMock>();
            }
            services.AddSingleton<IFileOperationsWorker, FileOperationsWorker>();
            return services.AddSingleton<IFileMover, FileMover>();
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
                return services.AddSingleton<IEpGuide, EpGuideMock>();
            }
            return services.AddSingleton<IEpGuide, EpGuidesCom>();
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
                return services.AddSingleton<ISeriesVideoSearcher, SeriesVideoSearcherMock>();
            }
            return services.AddSingleton<ISeriesVideoSearcher, SeriesVideoSearcher>();
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

        public static IServiceCollection UseHistory(this IServiceCollection services)
        {
            return services.AddSingleton(typeof(IHistoryCollection<>), typeof(HistoryCollection<>));
        }
        public static void FireForget<T>(this Task task, ILogger<T> logger)
        {
            task.ContinueWith(t => logger.LogWarning(t.Exception, "Fire-And-Forget action failed"), TaskContinuationOptions.OnlyOnFaulted);
        }

        public static long ToUnixTime(this DateTime dateTime)
        {
            var timeSpan = (dateTime - new DateTime(1970, 1, 1, 0, 0, 0));
            return (long)timeSpan.TotalSeconds;
        }

        public static IApplicationBuilder UseTimingMiddleware(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<TimingMiddleware>();
        }

        public static IServiceCollection UseSharedData(this IServiceCollection services)
        {
            return services.AddSingleton(typeof(ISharedData<>), typeof(SharedData<>));
        }

        public static void DoForEach<Element>(this IList<Element> list, Action<Element> action)
        {
            foreach (var el in list)
            {
                action(el);
            }
        }
    }
}

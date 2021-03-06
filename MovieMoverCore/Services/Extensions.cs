﻿using Microsoft.Extensions.DependencyInjection;
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
            return services.AddSingleton<ISettings, Settings>();
        }

        public static IServiceCollection UsePlex(this IServiceCollection services)
        {
            return services.AddSingleton<IPlex, Plex>();
        }

        public static IServiceCollection UseFileMover(this IServiceCollection services)
        {
            services.AddSingleton<IFileMoveWorker, FileMoveWorker>();
            return services.AddTransient<IFileMover, FileMover>();
        }

        public static IServiceCollection UseDatabase(this IServiceCollection services)
        {
            return services.AddSingleton<IDatabase, DB>();
        }

        public static IServiceCollection UseEpGuide(this IServiceCollection services)
        {
            return services.AddTransient<IEpGuide, EpGuidesCom>();
        }

        public static IServiceCollection UseSubtitles(this IServiceCollection services)
        {
            return services.AddTransient<ISubtitles, Addic7ed>();
        }

        public static IServiceCollection UseSeriesVideoSearcher(this IServiceCollection services)
        {
            return services.AddTransient<ISeriesVideoSearcher, SeriesVideoSearcher>();
        }

        public static IServiceCollection UseCache(this IServiceCollection services)
        {
            return services.AddSingleton(typeof(ICache<,,>), typeof(Cache<,,>));
        }

        public static IServiceCollection UseJDownloader(this IServiceCollection services)
        {
            return services.AddSingleton<IJDownloader, JDownloader>();
        }

        public static void FireForget<T>(this Task task, ILogger<T> logger)
        {
            task.ContinueWith(t => logger.LogWarning(t.Exception, "Fire-And-Forget action failed"), TaskContinuationOptions.OnlyOnFaulted);
        }
    }
}

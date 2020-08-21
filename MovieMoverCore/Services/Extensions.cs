using Microsoft.Extensions.DependencyInjection;
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
    }
}

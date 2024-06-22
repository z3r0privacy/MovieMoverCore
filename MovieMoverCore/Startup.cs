using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using MovieMoverCore.Services;
using Microsoft.EntityFrameworkCore;

namespace MovieMoverCore
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.UseSettings();
            services.UseDatabase();
            services.UsePlex();
            services.UseFileMover();
            services.UseEpGuide();
            services.UseSeriesVideoSearcher();
            services.UseSubtitles();
            services.UseCache();
            services.UseJDownloader();
            services.UseHistory();
            services.UseSharedData();

            //services.UseSettings();
            //services.UseDatabase(true);
            //services.UsePlex(true);
            //services.UseFileMover(true);
            //services.UseEpGuide(true);
            //services.UseSeriesVideoSearcher(true);
            //services.UseSubtitles(true);
            //services.UseCache(true);
            //services.UseJDownloader(true);


            services.AddMvc().AddRazorPagesOptions(opts =>
            {
                opts.Conventions.AddPageRoute("/Downloads", "");
            });
            services.AddRazorPages();
            services.AddControllers();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            string http_redirect = Environment.GetEnvironmentVariable("MOVER_https_redirect");
            if (http_redirect != null && http_redirect.Trim().Equals("true", StringComparison.CurrentCultureIgnoreCase))
            {
                app.UseHttpsRedirection();
            }
            
            app.UseStaticFiles();

            app.UseRouting();

            app.UseAuthorization();

            app.UseTimingMiddleware();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapRazorPages();
                endpoints.MapControllers();
            });
        }
    }
}

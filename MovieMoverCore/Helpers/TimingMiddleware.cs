using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;
using System;
using MovieMoverCore.Services;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;

namespace MovieMoverCore.Helpers
{
    public class TimingMiddleware
    {
        private readonly RequestDelegate _next;
        private bool _timespanSet = false;

        public TimingMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext httpContext, ILogger<TimingMiddleware> logger, ISharedData<(DateTime requestTime, TimeSpan duration, string request)> data)
        {
            var start = DateTime.Now;
            await _next(httpContext);
            var duration = DateTime.Now - start;

            data.Add("REQUEST_TIMINGS", (start, duration, $"{httpContext.Request.Method} {httpContext.Request.Path}"));
            var msg = $"The request {httpContext.Request.Method} {httpContext.Request.Path} took {duration}";
            if (duration.TotalSeconds > 1)
            {
                logger.LogWarning(msg);
            } else
            {
                logger.LogDebug(msg);
            }
            
            if (!_timespanSet)
            {
                data.SetValidity("REQUEST_TIMINGS", new TimeSpan(0, 10, 0));
                _timespanSet = true;
            }
        }
    }
}

// Attributes/RateLimitAttribute.cs
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Caching.Memory;
using System;
using System.Net;

namespace HealthcareSystem.API.Attributes
{
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
    public class RateLimitAttribute : ActionFilterAttribute
    {
        public int PermitLimit { get; set; } = 100;
        public int Window { get; set; } = 60; // seconds

        public override void OnActionExecuting(ActionExecutingContext context)
        {
            var cache = context.HttpContext.RequestServices.GetService<IMemoryCache>();
            var ipAddress = context.HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
            var endpoint = context.HttpContext.Request.Path;
            var cacheKey = $"ratelimit_{ipAddress}_{endpoint}";

            if (cache.TryGetValue(cacheKey, out int requestCount))
            {
                if (requestCount >= PermitLimit)
                {
                    context.HttpContext.Response.StatusCode = 429;
                    context.Result = new Microsoft.AspNetCore.Mvc.JsonResult(new
                    {
                        message = "Too many requests. Please try again later.",
                        retryAfter = Window
                    });
                    return;
                }

                cache.Set(cacheKey, requestCount + 1, TimeSpan.FromSeconds(Window));
            }
            else
            {
                cache.Set(cacheKey, 1, TimeSpan.FromSeconds(Window));
            }

            base.OnActionExecuting(context);
        }
    }
}
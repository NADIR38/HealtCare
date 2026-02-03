// Attributes/CacheAttribute.cs
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Caching.Memory;
using System;
using System.Text;

namespace HealthcareSystem.API.Attributes
{
    [AttributeUsage(AttributeTargets.Method)]
    public class CacheAttribute : ActionFilterAttribute
    {
        public int Duration { get; set; } = 60; // seconds
        public string VaryByQueryKeys { get; set; } = ""; // comma-separated

        public override void OnActionExecuting(ActionExecutingContext context)
        {
            var cache = context.HttpContext.RequestServices.GetService<IMemoryCache>();
            var cacheKey = GenerateCacheKey(context);

            if (cache.TryGetValue(cacheKey, out object cachedResult))
            {
                context.Result = new OkObjectResult(cachedResult);
                return;
            }

            base.OnActionExecuting(context);
        }

        public override void OnActionExecuted(ActionExecutedContext context)
        {
            if (context.Result is OkObjectResult okResult)
            {
                var cache = context.HttpContext.RequestServices.GetService<IMemoryCache>();
                var cacheKey = GenerateCacheKey(context);
                cache.Set(cacheKey, okResult.Value, TimeSpan.FromSeconds(Duration));
            }

            base.OnActionExecuted(context);
        }

        private string GenerateCacheKey(FilterContext context)
        {
            var keyBuilder = new StringBuilder();
            keyBuilder.Append(context.HttpContext.Request.Path);

            if (!string.IsNullOrEmpty(VaryByQueryKeys))
            {
                var keys = VaryByQueryKeys.Split(',');
                foreach (var key in keys)
                {
                    var value = context.HttpContext.Request.Query[key.Trim()];
                    keyBuilder.Append($"_{key}={value}");
                }
            }

            return keyBuilder.ToString();
        }
    }
}
using Hangfire.Annotations;
using Hangfire.Dashboard;

namespace HealthCare.Authorization
{
    public class HangfirAuthrizationFilter : IDashboardAsyncAuthorizationFilter
    {
        //public bool AuthorizeAsync( DashboardContext context)
        //{
        //    var HttpContext = context.GetHttpContext();
        //    return HttpContext.Request.Host.Host == "localhost" || HttpContext.User.IsInRole("Admin");
        //}

        public async Task<bool> AuthorizeAsync(DashboardContext context)
        {
            var httpContext = context.GetHttpContext();
            return httpContext.Request.Host.Host == "localhost" || httpContext.User.IsInRole("Admin");
        }
    }
}

using Microsoft.AspNetCore.Mvc;

namespace HealthCare.Api.Controllers
{
    public class UserController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}

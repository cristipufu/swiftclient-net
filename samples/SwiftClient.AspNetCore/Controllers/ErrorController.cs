using Microsoft.AspNetCore.Mvc;

namespace SwiftClient.AspNetCore.Controllers
{
    public class ErrorController : Controller
    {
        public ErrorController()
        {

        }

        public IActionResult Http403()
        {
            return View("Forbidden");
        }

        public IActionResult Http404()
        {
            return View("NotFound");
        }

        public IActionResult Http500()
        {
            return View("ServerError");
        }
    }
}

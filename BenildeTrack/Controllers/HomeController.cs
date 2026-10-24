using Microsoft.AspNetCore.Mvc;

namespace BenildeTrack.Controllers;

public class HomeController : Controller
{
    public IActionResult Index()
    {
        if (User.Identity?.IsAuthenticated == true)
            return RedirectToAction("Index", "Dashboard");
        return View();
    }

    public IActionResult Error() => Content("Something went wrong.");
}
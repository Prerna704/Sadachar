using Microsoft.AspNetCore.Mvc;

namespace Sadachar.Web.Controllers;

public class HomeController : Controller
{
    [HttpGet]
    [Route("/")]
    [Route("/Home")]
    [Route("/Home/Index")]
    public IActionResult Index()
    {
        return View();
    }

    [HttpGet]
    [Route("/Home/Error")]
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View();
    }
}

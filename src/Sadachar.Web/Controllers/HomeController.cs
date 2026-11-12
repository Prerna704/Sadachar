using Microsoft.AspNetCore.Mvc;

namespace Sadachar.Web.Controllers;

public class HomeController : Controller
{
    public IActionResult Index() => Redirect("~/");
}

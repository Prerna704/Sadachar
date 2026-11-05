using Microsoft.AspNetCore.Mvc;

namespace SakshamGovernance.Web.Controllers;

public class HomeController : Controller
{
    public IActionResult Index() => View();
}

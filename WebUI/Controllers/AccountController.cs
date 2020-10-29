using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace WebUI.Controllers
{
    public class AccountController : Controller
    {
        public IActionResult NewUserGreeting()
        {
            var id = (ClaimsIdentity)HttpContext.User.Identity;
            var newUserClaim = id.FindFirst(c => c.Type == "isNewUser");
            id.RemoveClaim(newUserClaim);            

            return View(id.Claims);
        }

        [AllowAnonymous]
        public IActionResult UserClaims()
        {
            ViewBag.ClaimsIdentity = User.Identity;
            ViewBag.Claims = User.Claims;
            return View();
        }
    }
}

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Identity.Web;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using WebUI.Service;
using WebUI.ViewModels;

namespace WebUI.Controllers
{
    public class AccountController : Controller
    {
        private readonly IUserService _userService;

        public AccountController(IUserService userService)
        {
            _userService = userService;
        }

        [Authorize(Policy = "HasNoAccount")]
        public IActionResult NewUserGreeting()
        {
            var vm = new NewUserGreetingVm
            {
                Name = User.GetDisplayName(),
                Email = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value
            };

            return View(vm);
        }

        [HttpPost]
        [Authorize(Policy = "HasNoAccount")]
        public async Task<IActionResult> NewUserGreeting(string returnUrl)
        {
            await _userService.AddNewUser(User);

            var result = await HttpContext.AuthenticateAsync();
            _userService.SetHasAccountClaim(User);
            await _userService.SetLocalFolderClaim(User);
            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, User, result.Properties);
                        
            return RedirectToAction(nameof(Info));
        }

        [AllowAnonymous]
        public async Task<IActionResult> RemoveClaim()
        {
            var result = await HttpContext.AuthenticateAsync();

            var identity = (ClaimsIdentity)User.Identity;
            var claimToRemove = identity.FindFirst("isNewUser");

            if (claimToRemove != null)
                identity.RemoveClaim(claimToRemove);

            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, User, result.Properties);

            return RedirectToAction(nameof(Info));
        }
        
        [Authorize(Policy = "HasAccount")]
        public IActionResult SelfRemove()
        {
            ViewBag.ClaimsIdentity = User.Identity;
            ViewBag.Claims = User.Claims;
            return View();
        }

        [HttpPost]
        [Authorize(Policy = "HasAccount")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SelfRemove(string _)
        {
            await _userService.RemoveUser(User);
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

            return RedirectToAction("Index", "Image");
        }

        [AllowAnonymous]
        public IActionResult Info()
        {
            ViewBag.ClaimsIdentity = User.Identity;
            ViewBag.Claims = User.Claims;
            return View();
        }
    }
}

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using WebUI.Controllers;

namespace WebUI.Filters
{
    public class NewUserActionFilter : ActionFilterAttribute
    {
        public override void OnActionExecuting(ActionExecutingContext context)
        {
            if (!context.HttpContext.User.Identity.IsAuthenticated)
                return;

            if (context.Controller.GetType().FullName.Contains("AccountController"))
                return;

            if (context.HttpContext.User.HasClaim("hasAccount", "false"))
                context.Result = new RedirectToActionResult("NewUserGreeting", "Account", null);
        }
    }
}

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace WebUI.Filters
{
    /// <summary>
    /// Filter intercepting users logged in to the IDP who not yet have an account in the app. Intercept happens based on 
    /// the availability of custom claim set to: "hasAccount" == "false" 
    /// </summary>
    public class NewUserActionFilter : ActionFilterAttribute
    {
        public override void OnActionExecuting(ActionExecutingContext context)
        {
            if (!context.HttpContext.User.Identity.IsAuthenticated)
                return;

            // if request is already on the account controller we do not intercept (possible endless loop)
            if (context.Controller.GetType().FullName.Contains("AccountController"))
                return;

            // if error happend with or without redirection by error middleware the we do not intercept
            if (context.HttpContext.Response.StatusCode == 500 || context.HttpContext.Request.Path == "/image/error")
                return;

            // if claim is set then intercept and redirect to handler 
            if (context.HttpContext.User.HasClaim("hasAccount", "false"))
                context.Result = new RedirectToActionResult("NewUserGreeting", "Account", null);
        }
    }
}

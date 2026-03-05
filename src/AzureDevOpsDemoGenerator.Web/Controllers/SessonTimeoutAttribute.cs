using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace AzureDevOpsDemoGenerator.Web.Controllers
{
    public class SessonTimeoutAttribute : ActionFilterAttribute
    {
        public override void OnActionExecuting(ActionExecutingContext context)
        {
            if (context.HttpContext.Session.GetString("visited") == null)
            {
                context.Result = new RedirectToActionResult("SessionOutReturn", "Account", null);
                return;
            }

            base.OnActionExecuting(context);
        }
    }
}

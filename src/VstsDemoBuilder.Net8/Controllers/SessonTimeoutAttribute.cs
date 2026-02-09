using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;

namespace VstsDemoBuilder.Controllers
{
    public class SessonTimeoutAttribute : ActionFilterAttribute
    {
        public override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            if (filterContext.HttpContext.Session.GetString("visited") == null)
            {
                //filterContext.Result = new RedirectResult("../account/SessionOutReturn", true);
                filterContext.Result = new RedirectToRouteResult(new RouteValueDictionary(new { controller = "account", action = "SessionOutReturn" }));
                return;
            }
            base.OnActionExecuting(filterContext);
        }
    }
}

using System;
using Microsoft.ApplicationInsights;
using Microsoft.AspNetCore.Mvc.Filters;

namespace VstsDemoBuilder.ErrorHandler
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, Inherited = true, AllowMultiple = true)]
    public class AiHandleErrorAttribute : ExceptionFilterAttribute
    {
        public override void OnException(ExceptionContext context)
        {
            if (context?.Exception != null)
            {
                var ai = context.HttpContext.RequestServices.GetService(typeof(TelemetryClient)) as TelemetryClient;
                ai?.TrackException(context.Exception);
            }

            base.OnException(context);
        }
    }
}

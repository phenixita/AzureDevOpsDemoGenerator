using Microsoft.AspNetCore.Mvc;

namespace VstsDemoBuilder.Infrastructure
{
    public abstract class LegacyController : Controller
    {
        private LegacySession _session;
        private LegacyServerUtility _server;

        protected LegacySession Session => _session ??= new LegacySession(HttpContext.Session);

        protected LegacyServerUtility Server => _server ??= new LegacyServerUtility();
    }
}

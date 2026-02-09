using Microsoft.AspNetCore.Mvc.Razor;

namespace VstsDemoBuilder.Infrastructure
{
    public abstract class LegacyRazorPage<TModel> : RazorPage<TModel>
    {
        private LegacySession _session;

        public LegacySession Session => _session ??= new LegacySession(Context.Session);
    }
}

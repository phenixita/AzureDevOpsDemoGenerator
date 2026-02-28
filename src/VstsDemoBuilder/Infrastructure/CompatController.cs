using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace VstsDemoBuilder.Infrastructure
{
    public sealed class SessionAccessor
    {
        private readonly ISession _session;

        public SessionAccessor(ISession session)
        {
            _session = session;
        }

        public object this[string key]
        {
            get => _session.GetString(key);
            set
            {
                if (value == null)
                {
                    _session.Remove(key);
                }
                else
                {
                    _session.SetString(key, value.ToString());
                }
            }
        }

        public void Clear() => _session.Clear();
    }

    public sealed class ServerAccessor
    {
        public string MapPath(string path) => AppPath.MapPath(path);
    }

    public abstract class CompatController : Controller
    {
        protected SessionAccessor Session => new SessionAccessor(HttpContext.Session);
        protected ServerAccessor Server { get; } = new ServerAccessor();
    }
}

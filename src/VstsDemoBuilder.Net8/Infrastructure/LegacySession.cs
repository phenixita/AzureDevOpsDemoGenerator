using Microsoft.AspNetCore.Http;

namespace VstsDemoBuilder.Infrastructure
{
    public sealed class LegacySession
    {
        private readonly ISession _session;

        public LegacySession(ISession session)
        {
            _session = session;
        }

        public string this[string key]
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
                    _session.SetString(key, value);
                }
            }
        }

        public void Clear() => _session.Clear();
    }
}

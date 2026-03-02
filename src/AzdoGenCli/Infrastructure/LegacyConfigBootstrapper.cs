using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Specialized;
using System.Reflection;

namespace AzdoGenCli.Infrastructure
{
    public static class LegacyConfigBootstrapper
    {
        public static void Apply(IConfigurationSection section)
        {
            if (section == null)
            {
                return;
            }

            var appSettings = System.Configuration.ConfigurationManager.AppSettings;
            SetWritable(appSettings);

            foreach (var child in section.GetChildren())
            {
                if (!string.IsNullOrWhiteSpace(child.Key) && child.Value != null)
                {
                    var appSettingKey = child.Key;
                    if (string.Equals(appSettingKey, "EmailPassword", StringComparison.OrdinalIgnoreCase))
                    {
                        appSettingKey = "password";
                    }

                    appSettings[appSettingKey] = child.Value;
                }
            }
        }

        private static void SetWritable(NameValueCollection collection)
        {
            var readOnlyField = typeof(NameObjectCollectionBase).GetField("_readOnly", BindingFlags.Instance | BindingFlags.NonPublic);
            readOnlyField?.SetValue(collection, false);
        }
    }
}

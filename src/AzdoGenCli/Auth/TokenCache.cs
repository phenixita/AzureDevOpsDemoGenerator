using System;
using System.IO;
using AzdoGenCli.Models;
using Newtonsoft.Json;
using Microsoft.Extensions.Logging;

namespace AzdoGenCli.Auth
{
    public static class TokenCache
    {
        private static string GetCachePath()
        {
            string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            string cacheDir = Path.Combine(appData, "AzdoGenCli");
            
            if (!Directory.Exists(cacheDir))
            {
                Directory.CreateDirectory(cacheDir);
            }
            
            return Path.Combine(cacheDir, "token.json");
        }

        public static void SaveToken(AccessDetails token, ILogger? logger = null)
        {
            try
            {
                string path = GetCachePath();
                string json = JsonConvert.SerializeObject(token, Formatting.Indented);
                File.WriteAllText(path, json);
                logger?.LogDebug("Token saved to cache: {Path}", path);
            }
            catch (Exception ex)
            {
                logger?.LogWarning(ex, "Failed to save token to cache");
            }
        }

        public static AccessDetails? GetToken(ILogger? logger = null)
        {
            try
            {
                string path = GetCachePath();
                if (!File.Exists(path))
                {
                    return null;
                }

                string json = File.ReadAllText(path);
                var token = JsonConvert.DeserializeObject<AccessDetails>(json);
                
                if (token == null || string.IsNullOrEmpty(token.access_token))
                {
                    return null;
                }

                // Check if token is expired (basic check, tokens usually last 1 hour)
                // AccessDetails doesn't seem to have an 'expires_at' property in the model, 
                // but it should have 'expires_in'.
                
                logger?.LogDebug("Token loaded from cache: {Path}", path);
                return token;
            }
            catch (Exception ex)
            {
                logger?.LogWarning(ex, "Failed to load token from cache");
                return null;
            }
        }

        public static void ClearToken(ILogger? logger = null)
        {
            try
            {
                string path = GetCachePath();
                if (File.Exists(path))
                {
                    File.Delete(path);
                    logger?.LogDebug("Token cache cleared");
                }
            }
            catch (Exception ex)
            {
                logger?.LogWarning(ex, "Failed to clear token cache");
            }
        }
    }
}

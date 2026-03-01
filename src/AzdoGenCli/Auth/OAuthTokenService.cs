using Newtonsoft.Json;
using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using AzdoGenCli.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace AzdoGenCli.Auth
{
    /// <summary>
    /// OAuth 2.0 token service for Azure DevOps Entra authentication
    /// Ported from VstsDemoBuilder.Services.AccountService
    /// </summary>
    public static class OAuthTokenService
    {
        /// <summary>
        /// Build token request body for Entra OAuth 2.0 authorization code exchange (public client)
        /// </summary>
        public static string GenerateRequestPostData(string clientId, string authCode, string callbackUrl, string appScope)
        {
            return string.Format(
                "client_id={0}&code={1}&redirect_uri={2}&grant_type=authorization_code&scope={3}",
                WebUtility.UrlEncode(clientId),
                WebUtility.UrlEncode(authCode),
                WebUtility.UrlEncode(callbackUrl),
                WebUtility.UrlEncode(appScope)
            );
        }

        /// <summary>
        /// Exchange authorization code for access token via Entra token endpoint
        /// </summary>
        public static AccessDetails GetAccessToken(string body, string tenantId, ILogger? logger = null)
        {
            try
            {
                string tokenEndpoint = $"https://login.microsoftonline.com/{tenantId}/oauth2/v2.0/token";

                var client = new HttpClient();
                var request = new HttpRequestMessage(HttpMethod.Post, tokenEndpoint);
                request.Content = new StringContent(body, Encoding.UTF8, "application/x-www-form-urlencoded");

                var response = client.SendAsync(request).Result;
                string result = response.Content.ReadAsStringAsync().Result;
                AccessDetails details = JsonConvert.DeserializeObject<AccessDetails>(result) ?? new AccessDetails();
                
                if (!response.IsSuccessStatusCode)
                {
                    logger?.LogError("GetAccessToken error: {Result}", result);
                }
                
                return details;
            }
            catch (Exception ex)
            {
                logger?.LogError(ex, "Token exchange exception");
                return new AccessDetails
                {
                    error = "token_exchange_exception",
                    error_description = ex.Message
                };
            }
        }

        /// <summary>
        /// Get user profile details from Azure DevOps
        /// </summary>
        public static ProfileDetails? GetProfile(AccessDetails accessDetails, string baseAddress, ILogger? logger = null)
        {
            using (var client = new HttpClient())
            {
                try
                {
                    client.BaseAddress = new Uri(baseAddress);
                    client.DefaultRequestHeaders.Accept.Clear();
                    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessDetails.access_token);
                    
                    HttpResponseMessage response = client.GetAsync("_apis/profile/profiles/me?details=true&api-version=4.1").Result;
                    
                    if (response.IsSuccessStatusCode && response.StatusCode == HttpStatusCode.OK)
                    {
                        string result = response.Content.ReadAsStringAsync().Result;
                        var profile = JsonConvert.DeserializeObject<ProfileDetails>(result);
                        return profile ?? new ProfileDetails();
                    }
                    else
                    {
                        var errorMessage = response.Content.ReadAsStringAsync().Result;
                        logger?.LogError("Get Profile error: {ErrorMessage}", errorMessage);
                    }
                }
                catch (Exception ex)
                {
                    logger?.LogError(ex, "Exception getting user profile");
                }
                return new ProfileDetails();
            }
        }

        /// <summary>
        /// Get list of Azure DevOps accounts (organizations) accessible to the user
        /// </summary>
        public static AccountsResponse.AccountList GetAccounts(string memberID, AccessDetails details, string baseAddress, ILogger? logger = null)
        {
            var client = new HttpClient();

            string requestContent = baseAddress + "/_apis/Accounts?memberId=" + memberID + "&api-version=4.1";
            var request = new HttpRequestMessage(HttpMethod.Get, requestContent);
            request.Headers.Add("Authorization", "Bearer " + details.access_token);
            
            try
            {
                var response = client.SendAsync(request).Result;
                if (response.IsSuccessStatusCode && response.StatusCode == HttpStatusCode.OK)
                {
                    string result = response.Content.ReadAsStringAsync().Result;
                    var accounts = JsonConvert.DeserializeObject<AccountsResponse.AccountList>(result);
                    return accounts ?? new AccountsResponse.AccountList();
                }
                else
                {
                    var errorMessage = response.Content.ReadAsStringAsync().Result;
                    logger?.LogError("Get Accounts error: {ErrorMessage}", errorMessage);
                }
            }
            catch (Exception ex)
            {
                logger?.LogError(ex, "Exception getting accounts");
            }
            return new AccountsResponse.AccountList();
        }

        /// <summary>
        /// Refresh access token via Entra token endpoint (public client)
        /// </summary>
        public static AccessDetails Refresh_AccessToken(string refreshToken, string tenantId, string redirectUri, string clientId, string appScope, ILogger? logger = null)
        {
            using (var client = new HttpClient())
            {
                string tokenEndpoint = $"https://login.microsoftonline.com/{tenantId}/oauth2/v2.0/token";
                var request = new HttpRequestMessage(HttpMethod.Post, tokenEndpoint);
                var requestContent = string.Format(
                    "client_id={0}&grant_type=refresh_token&refresh_token={1}&redirect_uri={2}&scope={3}",
                    WebUtility.UrlEncode(clientId),
                    WebUtility.UrlEncode(refreshToken),
                    WebUtility.UrlEncode(redirectUri),
                    WebUtility.UrlEncode(appScope)
                );

                request.Content = new StringContent(requestContent, Encoding.UTF8, "application/x-www-form-urlencoded");
                try
                {
                    var response = client.SendAsync(request).Result;
                    if (response.IsSuccessStatusCode)
                    {
                        string result = response.Content.ReadAsStringAsync().Result;
                        var accessDetails = JsonConvert.DeserializeObject<AccessDetails>(result);
                        return accessDetails ?? new AccessDetails();
                    }
                    else
                    {
                        return new AccessDetails();
                    }
                }
                catch (Exception ex)
                {
                    logger?.LogError(ex, "Exception refreshing access token");
                    return new AccessDetails();
                }
            }
        }
    }
}

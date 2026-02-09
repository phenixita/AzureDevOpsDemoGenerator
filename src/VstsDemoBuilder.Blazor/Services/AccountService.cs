using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Net;
using VstsDemoBuilder.Models;
using VstsDemoBuilder.ServiceInterfaces;

namespace VstsDemoBuilder.Services
{
    public class AccountService :IAccountService
    {
        /// <summary>
        /// Formatting the request for OAuth - Entra ID v2.0
        /// </summary>
        /// <param name="appSecret">Client secret from configuration</param>
        /// <param name="authCode">Authorization code from Entra ID</param>
        /// <param name="callbackUrl">Redirect URI</param>
        /// <returns>URL-encoded form data for token request</returns>
        public string GenerateRequestPostData(string appSecret, string authCode, string callbackUrl)
        {
            try
            {
                string clientId = VstsDemoBuilder.Infrastructure.AppSettings.Get("ClientId");
                // Modern Entra ID v2.0 OAuth2 code grant flow
                return String.Format("client_id={0}&client_secret={1}&code={2}&grant_type=authorization_code&redirect_uri={3}",
                            Uri.EscapeDataString(clientId),
                            Uri.EscapeDataString(appSecret),
                            Uri.EscapeDataString(authCode),
                            Uri.EscapeDataString(callbackUrl)
                     );
            }
            catch (Exception ex)
            {
                ProjectService.logger.Error(DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss") + "\t Error in GenerateRequestPostData: " + ex.Message + "\t" + "\n" + ex.StackTrace + "\n", ex);
            }
            return string.Empty;
        }

        /// <summary>
        /// Generate Access Token from Entra ID
        /// </summary>
        /// <param name="body">URL-encoded request body</param>
        /// <returns>Access token details</returns>
        public AccessDetails GetAccessToken(string body)
        {
            try
            {
                // Use Entra ID token endpoint
                string tokenEndpoint = "https://login.microsoftonline.com/common/oauth2/v2.0/token";
                var client = new HttpClient();
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
                var request = new HttpRequestMessage(HttpMethod.Post, tokenEndpoint);

                var requestContent = body;
                request.Content = new StringContent(requestContent, Encoding.UTF8, "application/x-www-form-urlencoded");

                ProjectService.logger.Info($"GetAccessToken - Posting to: {tokenEndpoint}");
                var response = client.SendAsync(request).Result;
                
                string result = response.Content.ReadAsStringAsync().Result;
                if (response.IsSuccessStatusCode)
                {
                    ProjectService.logger.Info($"GetAccessToken - Success");
                    AccessDetails details = Newtonsoft.Json.JsonConvert.DeserializeObject<AccessDetails>(result);
                    return details;
                }
                else
                {
                    ProjectService.logger.Error($"GetAccessToken - Error from Entra ID: {response.StatusCode} - {result}");
                }
            }
            catch (Exception ex)
            {
                ProjectService.logger.Error(DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss") + "\t Error in GetAccessToken: " + ex.Message + "\t" + "\n" + ex.StackTrace + "\n", ex);
            }
            return new AccessDetails();
        }

        /// <summary>
        /// Get Profile details
        /// </summary>
        /// <param name="accessDetails"></param>
        /// <returns></returns>
        public ProfileDetails GetProfile(AccessDetails accessDetails)
        {
            ProfileDetails profile = new ProfileDetails();
            using (var client = new HttpClient())
            {
                try
                {
                    string baseAddress = VstsDemoBuilder.Infrastructure.AppSettings.Get("BaseAddress");

                    client.BaseAddress = new Uri(baseAddress);
                    client.DefaultRequestHeaders.Accept.Clear();
                    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessDetails.access_token);
                    HttpResponseMessage response = client.GetAsync("_apis/profile/profiles/me?details=true&api-version=4.1").Result;
                    if (response.IsSuccessStatusCode && response.StatusCode == HttpStatusCode.OK)
                    {
                        string result = response.Content.ReadAsStringAsync().Result;
                        profile = JsonConvert.DeserializeObject<ProfileDetails>(result);
                        return profile;
                    }
                    else
                    {
                        var errorMessage = response.Content.ReadAsStringAsync();
                        ProjectService.logger.Info(DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss") + "\t Get Profile :" + errorMessage + "\n");
                    }
                }
                catch (Exception ex)
                {
                    ProjectService.logger.Info(DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss") + "\t" + ex.Message + "\t" + "\n" + ex.StackTrace + "\n");
                }
                return profile;
            }
        }


        /// <summary>
        /// Refresh access token
        /// </summary>
        /// <param name="refreshToken"></param>
        /// <returns></returns>
        public AccessDetails Refresh_AccessToken(string refreshToken)
        {
            using (var client = new HttpClient())
            {
                string redirectUri = VstsDemoBuilder.Infrastructure.AppSettings.Get("RedirectUri");
                string cientSecret = VstsDemoBuilder.Infrastructure.AppSettings.Get("ClientSecret");
                string baseAddress = VstsDemoBuilder.Infrastructure.AppSettings.Get("BaseAddress");

                var request = new HttpRequestMessage(HttpMethod.Post, baseAddress + "/oauth2/token");
                var requestContent = string.Format(
                    "client_assertion_type=urn:ietf:params:oauth:client-assertion-type:jwt-bearer&client_assertion={0}&grant_type=refresh_token&assertion={1}&redirect_uri={2}",
                    WebUtility.UrlEncode(cientSecret),
                    WebUtility.UrlEncode(refreshToken), redirectUri
                    );

                request.Content = new StringContent(requestContent, Encoding.UTF8, "application/x-www-form-urlencoded");
                try
                {
                    var response = client.SendAsync(request).Result;
                    if (response.IsSuccessStatusCode)
                    {
                        string result = response.Content.ReadAsStringAsync().Result;
                        AccessDetails accesDetails = JsonConvert.DeserializeObject<AccessDetails>(result);
                        return accesDetails;
                    }
                    else
                    {
                        return new AccessDetails();
                    }
                }
                catch (Exception ex)
                {
                    ProjectService.logger.Info(DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss") + "\t" + ex.Message + "\t" + "\n" + ex.StackTrace + "\n");
                    return new AccessDetails();
                }
            }
        }

        /// <summary>
        /// Get list of accounts
        /// </summary>
        /// <param name="memberID"></param>
        /// <param name="details"></param>
        /// <returns></returns>
        public AccountsResponse.AccountList GetAccounts(string memberID, AccessDetails details)
        {
            AccountsResponse.AccountList accounts = new AccountsResponse.AccountList();
            var client = new HttpClient();
            string baseAddress = VstsDemoBuilder.Infrastructure.AppSettings.Get("BaseAddress");

            string requestContent = baseAddress + "/_apis/Accounts?memberId=" + memberID + "&api-version=4.1";
            var request = new HttpRequestMessage(HttpMethod.Get, requestContent);
            request.Headers.Add("Authorization", "Bearer " + details.access_token);
            try
            {
                var response = client.SendAsync(request).Result;
                if (response.IsSuccessStatusCode && response.StatusCode == HttpStatusCode.OK)
                {
                    string result = response.Content.ReadAsStringAsync().Result;
                    accounts = JsonConvert.DeserializeObject<Models.AccountsResponse.AccountList>(result);
                    return accounts;
                }
                else
                {
                    var errorMessage = response.Content.ReadAsStringAsync();
                    ProjectService.logger.Info(DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss") + "\t Get Accounts :" + errorMessage + "\t" + "\n");
                }
            }
            catch (Exception ex)
            {
                ProjectService.logger.Info(DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss") + "\t" + ex.Message + "\t" + "\n" + ex.StackTrace + "\n");
            }
            return accounts;
        }

    }
}


using log4net;
﻿using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using AzureDevOpsDemoGenerator.Modules.Core;

namespace AzureDevOpsDemoGenerator.Modules.Account
{
    public class AccountService : IAccountService
    {
        private static readonly log4net.ILog logger = log4net.LogManager.GetLogger("ErrorLog");
        /// <summary>
        /// Build token request body for Entra OAuth 2.0 authorization code exchange
        /// </summary>
        public string GenerateRequestPostData(string appSecret, string authCode, string callbackUrl)
        {
            try
            {
                string clientId = System.Configuration.ConfigurationManager.AppSettings["ClientId"];
                string appScope = System.Configuration.ConfigurationManager.AppSettings["appScope"];

                return string.Format(
                    "client_id={0}&client_secret={1}&code={2}&redirect_uri={3}&grant_type=authorization_code&scope={4}",
                    WebUtility.UrlEncode(clientId),
                    WebUtility.UrlEncode(appSecret),
                    WebUtility.UrlEncode(authCode),
                    WebUtility.UrlEncode(callbackUrl),
                    WebUtility.UrlEncode(appScope)
                );
            }
            catch (Exception ex)
            {
                logger.Info(DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss") + "\t" + ex.Message + "\t" + "\n" + ex.StackTrace + "\n");
            }
            return string.Empty;
        }

        /// <summary>
        /// Exchange authorization code for access token via Entra token endpoint
        /// </summary>
        public AccessDetails GetAccessToken(string body)
        {
            try
            {
                string tenantId = System.Configuration.ConfigurationManager.AppSettings["TenantId"] ?? "common";
                string tokenEndpoint = $"https://login.microsoftonline.com/{tenantId}/oauth2/v2.0/token";

                var client = new HttpClient();
                var request = new HttpRequestMessage(HttpMethod.Post, tokenEndpoint);
                request.Content = new StringContent(body, Encoding.UTF8, "application/x-www-form-urlencoded");

                var response = client.SendAsync(request).Result;
                string result = response.Content.ReadAsStringAsync().Result;
                AccessDetails details = Newtonsoft.Json.JsonConvert.DeserializeObject<AccessDetails>(result) ?? new AccessDetails();
                if (!response.IsSuccessStatusCode)
                {
                    logger.Info(DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss") + "\t GetAccessToken error: " + result + "\n");
                }
                return details;
            }
            catch (Exception ex)
            {
                logger.Info(DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss") + "\t" + ex.Message + "\t" + "\n" + ex.StackTrace + "\n");
                return new AccessDetails
                {
                    error = "token_exchange_exception",
                    error_description = ex.Message
                };
            }
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
                    string baseAddress = System.Configuration.ConfigurationManager.AppSettings["BaseAddress"];

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
                        logger.Info(DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss") + "\t Get Profile :" + errorMessage + "\n");
                    }
                }
                catch (Exception ex)
                {
                    logger.Info(DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss") + "\t" + ex.Message + "\t" + "\n" + ex.StackTrace + "\n");
                }
                return profile;
            }
        }


        /// <summary>
        /// Refresh access token via Entra token endpoint
        /// </summary>
        public AccessDetails Refresh_AccessToken(string refreshToken)
        {
            using (var client = new HttpClient())
            {
                string tenantId = System.Configuration.ConfigurationManager.AppSettings["TenantId"] ?? "common";
                string redirectUri = System.Configuration.ConfigurationManager.AppSettings["RedirectUri"];
                string clientId = System.Configuration.ConfigurationManager.AppSettings["ClientId"];
                string clientSecret = System.Configuration.ConfigurationManager.AppSettings["ClientSecret"];
                string appScope = System.Configuration.ConfigurationManager.AppSettings["appScope"];

                string tokenEndpoint = $"https://login.microsoftonline.com/{tenantId}/oauth2/v2.0/token";
                var request = new HttpRequestMessage(HttpMethod.Post, tokenEndpoint);
                var requestContent = string.Format(
                    "client_id={0}&client_secret={1}&grant_type=refresh_token&refresh_token={2}&redirect_uri={3}&scope={4}",
                    WebUtility.UrlEncode(clientId),
                    WebUtility.UrlEncode(clientSecret),
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
                    logger.Info(DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss") + "\t" + ex.Message + "\t" + "\n" + ex.StackTrace + "\n");
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
            string baseAddress = System.Configuration.ConfigurationManager.AppSettings["BaseAddress"];

            string requestContent = baseAddress + "/_apis/Accounts?memberId=" + memberID + "&api-version=4.1";
            var request = new HttpRequestMessage(HttpMethod.Get, requestContent);
            request.Headers.Add("Authorization", "Bearer " + details.access_token);
            try
            {
                var response = client.SendAsync(request).Result;
                if (response.IsSuccessStatusCode && response.StatusCode == HttpStatusCode.OK)
                {
                    string result = response.Content.ReadAsStringAsync().Result;
                    accounts = JsonConvert.DeserializeObject<AccountsResponse.AccountList>(result);
                    return accounts;
                }
                else
                {
                    var errorMessage = response.Content.ReadAsStringAsync();
                    logger.Info(DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss") + "\t Get Accounts :" + errorMessage + "\t" + "\n");
                }
            }
            catch (Exception ex)
            {
                logger.Info(DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss") + "\t" + ex.Message + "\t" + "\n" + ex.StackTrace + "\n");
            }
            return accounts;
        }

    }
}

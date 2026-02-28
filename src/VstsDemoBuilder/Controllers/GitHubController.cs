using Newtonsoft.Json;
using System;
using System.Net;
using System.Net.Http;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VstsDemoBuilder.Infrastructure;
using VstsDemoBuilder.Models;
using VstsDemoBuilder.Services;

namespace VstsDemoBuilder.Controllers
{
    public class GitHubController : CompatController
    {

        private GitHubAccessDetails accessDetails = new GitHubAccessDetails();
        private const string GitHubOAuthStateSessionKey = "GitHubOAuthState";
        [AllowAnonymous]
        public ActionResult GitOauth()
        {
            //Request User GitHub Identity
            string ClientID = System.Configuration.ConfigurationManager.AppSettings["GitHubClientId"];
            string ClientSecret = System.Configuration.ConfigurationManager.AppSettings["GitHubClientSecret"];
            string RedirectUrl = System.Configuration.ConfigurationManager.AppSettings["GitHubRedirectUrl"];
            string Scope = System.Configuration.ConfigurationManager.AppSettings["GitHubScope"];
            string state = Guid.NewGuid().ToString("N");
            Session[GitHubOAuthStateSessionKey] = state;
            string url = string.Format("https://github.com/login/oauth/authorize?client_id={0}&scope={1}&redirect_uri={2}&state={3}", ClientID, Scope, RedirectUrl, state);
            return Redirect(url);
        }
        [AllowAnonymous]
        public ActionResult Redirect()
        {
            ServicePointManager.Expect100Continue = true;
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
            // Here we get the Code in the Query String, using that we can get access token
            string code = Request.Query["code"];
            string requestState = Request.Query["state"];
            string expectedState = Session[GitHubOAuthStateSessionKey]?.ToString();
            if (string.IsNullOrEmpty(requestState) || string.IsNullOrEmpty(expectedState) || !string.Equals(requestState, expectedState, StringComparison.Ordinal))
            {
                return RedirectToAction("Issue");
            }

            if (!string.IsNullOrEmpty(code))
            {

                string reqUrl = FormatRequestUrl(code, requestState);
                // Getting access token, if access token is null, will return to Index page [relogin takes place]
                GitHubAccessDetails _accessDetails = GetAccessToken(reqUrl);
                if (_accessDetails.access_token != null)
                {
                    Session["GitHubToken"] = _accessDetails.access_token;
                    ViewBag.Response = _accessDetails.access_token;
                    return RedirectToAction("Status");
                }
                else
                {
                    return RedirectToAction("Issue");
                }
            }
            return RedirectToAction("index", "home");
        }

        public string FormatRequestUrl(string code, string state)
        {
            string ClientID = System.Configuration.ConfigurationManager.AppSettings["GitHubClientId"];
            string ClientSecret = System.Configuration.ConfigurationManager.AppSettings["GitHubClientSecret"];
            string RedirectUrl = System.Configuration.ConfigurationManager.AppSettings["GitHubRedirectUrl"];
            string Scope = System.Configuration.ConfigurationManager.AppSettings["GitHubScope"];
            string requestUrl = string.Format("?client_id={0}&client_secret={1}&code={2}&redirect_uri={3}&state={4}", ClientID, ClientSecret, code, RedirectUrl, state);
            return requestUrl;
        }

        // Formatting the POST URL
        // Get the access token
        public GitHubAccessDetails GetAccessToken(string body)
        {
            try
            {
                using (var client = new HttpClient())
                {
                    client.DefaultRequestHeaders.Accept.Clear();
                    client.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));
                    var request = new HttpRequestMessage(HttpMethod.Post, string.Format("https://github.com/login/oauth/access_token/{0}", body));
                    HttpResponseMessage response = client.SendAsync(request).Result;
                    if (response.IsSuccessStatusCode)
                    {
                        accessDetails = JsonConvert.DeserializeObject<GitHubAccessDetails>(response.Content.ReadAsStringAsync().Result);
                        return accessDetails;
                    }
                }
            }
            catch (Exception ex)
            {
                ProjectService.logger.Info(DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss") + "\t" + ex.Message + "\t" + "\n" + ex.StackTrace + "\n");
            }
            return new GitHubAccessDetails();
        }
        [AllowAnonymous]
        public ActionResult Status()
        {
            return View();
        }
    }
}

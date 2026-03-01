using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using VstsDemoBuilder.Models;
using VstsDemoBuilder.ServiceInterfaces;

namespace VstsDemoBuilder.Services
{
    public class AccountService : IAccountService
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;
        private readonly ILogger<AccountService> _logger;

        public AccountService(IHttpClientFactory httpClientFactory, IConfiguration configuration, ILogger<AccountService> logger)
        {
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
            _logger = logger;
        }

        /// <summary>
        /// Build token request body for Entra OAuth 2.0 authorization code exchange
        /// </summary>
        public string GenerateRequestPostData(string appSecret, string authCode, string callbackUrl)
        {
            try
            {
                string clientId = _configuration["ClientId"];
                string appScope = _configuration["appScope"];

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
                _logger.LogError(ex, "Failed to generate OAuth request body.");
            }

            return string.Empty;
        }

        /// <summary>
        /// Exchange authorization code for access token via Entra token endpoint
        /// </summary>
        public async Task<AccessDetails> GetAccessTokenAsync(string body, CancellationToken cancellationToken = default)
        {
            try
            {
                string tenantId = _configuration["TenantId"] ?? "common";
                string tokenEndpoint = $"https://login.microsoftonline.com/{tenantId}/oauth2/v2.0/token";

                using var request = new HttpRequestMessage(HttpMethod.Post, tokenEndpoint)
                {
                    Content = new StringContent(body, Encoding.UTF8, "application/x-www-form-urlencoded")
                };

                var client = _httpClientFactory.CreateClient("entra-oauth");
                using var response = await client.SendAsync(request, cancellationToken).ConfigureAwait(false);
                string result = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
                AccessDetails details = JsonConvert.DeserializeObject<AccessDetails>(result) ?? new AccessDetails();
                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning("GetAccessToken error: {Result}", result);
                }

                return details;
            }
            catch (OperationCanceledException)
            {
                _logger.LogWarning("Token exchange canceled.");
                return new AccessDetails { error = "token_exchange_canceled", error_description = "Token exchange was canceled." };
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "Token exchange HTTP request failed.");
                return new AccessDetails { error = "token_exchange_http_error", error_description = ex.Message };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Token exchange failed.");
                return new AccessDetails { error = "token_exchange_exception", error_description = ex.Message };
            }
        }

        /// <summary>
        /// Get Profile details
        /// </summary>
        public async Task<ProfileDetails> GetProfileAsync(AccessDetails accessDetails, CancellationToken cancellationToken = default)
        {
            ProfileDetails profile = new ProfileDetails();

            try
            {
                string baseAddress = _configuration["BaseAddress"];
                var client = _httpClientFactory.CreateClient("azure-devops-account");
                client.BaseAddress = new Uri(baseAddress);

                using var request = new HttpRequestMessage(HttpMethod.Get, "_apis/profile/profiles/me?details=true&api-version=4.1");
                request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessDetails.access_token);

                using var response = await client.SendAsync(request, cancellationToken).ConfigureAwait(false);
                string result = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
                if (response.IsSuccessStatusCode && response.StatusCode == HttpStatusCode.OK)
                {
                    profile = JsonConvert.DeserializeObject<ProfileDetails>(result);
                    return profile;
                }

                _logger.LogWarning("Get Profile failed: {ErrorMessage}", result);
            }
            catch (OperationCanceledException)
            {
                _logger.LogWarning("Get profile canceled.");
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "Get profile HTTP request failed.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to fetch profile.");
            }

            return profile;
        }

        /// <summary>
        /// Refresh access token via Entra token endpoint
        /// </summary>
        public async Task<AccessDetails> RefreshAccessTokenAsync(string refreshToken, CancellationToken cancellationToken = default)
        {
            string tenantId = _configuration["TenantId"] ?? "common";
            string redirectUri = _configuration["RedirectUri"];
            string clientId = _configuration["ClientId"];
            string clientSecret = _configuration["ClientSecret"];
            string appScope = _configuration["appScope"];

            string tokenEndpoint = $"https://login.microsoftonline.com/{tenantId}/oauth2/v2.0/token";
            using var request = new HttpRequestMessage(HttpMethod.Post, tokenEndpoint);
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
                var client = _httpClientFactory.CreateClient("entra-oauth");
                using var response = await client.SendAsync(request, cancellationToken).ConfigureAwait(false);
                if (!response.IsSuccessStatusCode)
                {
                    return new AccessDetails();
                }

                string result = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
                AccessDetails accessDetails = JsonConvert.DeserializeObject<AccessDetails>(result);
                return accessDetails ?? new AccessDetails();
            }
            catch (OperationCanceledException)
            {
                _logger.LogWarning("Token refresh canceled.");
                return new AccessDetails();
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "Token refresh HTTP request failed.");
                return new AccessDetails();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Token refresh failed.");
                return new AccessDetails();
            }
        }

        /// <summary>
        /// Get list of accounts
        /// </summary>
        public async Task<AccountsResponse.AccountList> GetAccountsAsync(string memberID, AccessDetails details, CancellationToken cancellationToken = default)
        {
            AccountsResponse.AccountList accounts = new AccountsResponse.AccountList();

            string baseAddress = _configuration["BaseAddress"];
            string requestContent = baseAddress + "/_apis/Accounts?memberId=" + memberID + "&api-version=4.1";

            using var request = new HttpRequestMessage(HttpMethod.Get, requestContent);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", details.access_token);

            try
            {
                var client = _httpClientFactory.CreateClient("azure-devops-account");
                using var response = await client.SendAsync(request, cancellationToken).ConfigureAwait(false);
                string result = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
                if (response.IsSuccessStatusCode && response.StatusCode == HttpStatusCode.OK)
                {
                    accounts = JsonConvert.DeserializeObject<AccountsResponse.AccountList>(result);
                    return accounts;
                }

                _logger.LogWarning("Get Accounts failed: {ErrorMessage}", result);
            }
            catch (OperationCanceledException)
            {
                _logger.LogWarning("Get accounts canceled.");
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "Get accounts HTTP request failed.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to fetch accounts.");
            }

            return accounts;
        }
    }
}

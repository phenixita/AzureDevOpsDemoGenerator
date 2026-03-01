using Newtonsoft.Json;
using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;

namespace VstsRestAPI.Services
{
    public class HttpServices
    {
        private static readonly HttpClient Client = new HttpClient();
        private readonly Configuration oConfiguration = new Configuration();

        public HttpServices(Configuration config)
        {
            oConfiguration.UriString = config.UriString;
            oConfiguration.Project = config.Project;
            oConfiguration.PersonalAccessToken = Convert.ToBase64String(System.Text.ASCIIEncoding.ASCII.GetBytes(string.Format("{0}:{1}", "", config.PersonalAccessToken)));
            oConfiguration.UriParams = config.UriParams;
            oConfiguration.RequestBody = config.RequestBody;
            oConfiguration.VersionNumber = config.VersionNumber;

            Client.Timeout = TimeSpan.FromSeconds(30);
        }

        private HttpRequestMessage CreateRequest(HttpMethod method, string requestUri, HttpContent content = null)
        {
            var request = new HttpRequestMessage(method, requestUri);
            request.Headers.Accept.Clear();
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            request.Headers.Authorization = new AuthenticationHeaderValue("Basic", oConfiguration.PersonalAccessToken);
            if (content != null)
            {
                request.Content = content;
            }

            return request;
        }

        public HttpResponseMessage PatchBasic()
        {
            var patchValue = new StringContent(oConfiguration.RequestBody, Encoding.UTF8, "application/json-patch+json");
            var requestUri = oConfiguration.UriString + "/" + oConfiguration.Project + oConfiguration.UriParams + oConfiguration.VersionNumber;
            using var request = CreateRequest(new HttpMethod("PATCH"), requestUri, patchValue);
            return Client.Send(request);
        }

        public HttpResponseMessage Post(string json, string uriparams)
        {
            var jsonContent = new StringContent(json, Encoding.UTF8, "application/json");
            using var request = CreateRequest(HttpMethod.Post, oConfiguration.UriString + uriparams, jsonContent);
            return Client.Send(request);
        }

        public HttpResponseMessage Get(string request)
        {
            using var httpRequest = CreateRequest(HttpMethod.Get, oConfiguration.UriString + request);
            return Client.Send(httpRequest);
        }

        public HttpResponseMessage Put()
        {
            var patchValue = new StringContent(JsonConvert.SerializeObject(oConfiguration.RequestBody), Encoding.UTF8, "application/json-patch+json");
            using var request = CreateRequest(new HttpMethod("PATCH"), oConfiguration.UriString + oConfiguration.UriParams, patchValue);
            return Client.Send(request);
        }
    }
}

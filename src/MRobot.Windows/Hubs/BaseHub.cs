using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNet.SignalR.Client;

namespace MRobot.Windows.Hubs
{
    using System.Net.Http;
    using System.Net.Http.Headers;

    public class BaseHub
    {
        public const int RequestTimeoutSeconds = 3600;

        protected IHubProxy HubProxy;

        public BaseHub(string hubName, HubConnection connection)
        {
            HubProxy = connection.CreateHubProxy(hubName);
        }

        protected static HttpClient CreateHttpClient(Uri baseAddress = null)
        {
            var handler = new WebRequestHandler()
            {
                MaxRequestContentBufferSize = int.MaxValue,
                MaxResponseHeadersLength = int.MaxValue,
                ReadWriteTimeout = RequestTimeoutSeconds * 1000,
                AllowAutoRedirect = false
            };

            var client = new HttpClient(handler) { BaseAddress = baseAddress ?? new Uri(App.MrSettings.WebsiteBaseUrl) };

            client.DefaultRequestHeaders.Accept.Add(
                new MediaTypeWithQualityHeaderValue("application/json"));

            return client;
        }
    }
}

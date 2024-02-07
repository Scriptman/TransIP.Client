using TransIP.Client.Endpoints;
using TransIP.Client.Enums;

namespace TransIP.Client
{
    public class TransIpApi
    {
        private string _url = "https://api.transip.nl/v6/";

        private readonly BaseClient _client;

        public TransIpApi(string username, string privateKey, ClientMode clientMode = ClientMode.ReadOnly, bool onlyWhiteListedIps = false, string labelPrefix = "net.lib-")
        {
            _client = new BaseClient (_url, username, privateKey, clientMode, onlyWhiteListedIps, labelPrefix);
        }

        public void OverrideURL(string url)
        {
            if (string.IsNullOrWhiteSpace(url))
            {
                throw new ArgumentNullException(nameof(url), "Please provide an URL for the TransIP api.");
            }

            if (!url.EndsWith("/"))
            {
                url += "/";
            }

            _url = url;
        }

        public IDomainService domainService() => new DomainService(_client);
    }
}

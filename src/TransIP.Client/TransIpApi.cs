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

        public IDomainService domainService(string domainName = "") => new DomainService(_client, domainName);
    }
}

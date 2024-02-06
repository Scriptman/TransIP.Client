using TransIP.Client.DataTransferObjects;
using TransIP.Client.Enums;
using TransIP.Client.Models;

namespace TransIP.Client.Endpoints
{
    public interface IDomainService
    {
        Task<IEnumerable<Domain>> GetAllDomainsAsync(AdditionalData? addData);
    }
    public class DomainService : IDomainService, IEndpoint
    {
        private readonly BaseClient _baseClient;
        protected string endpoint = "domains";

        public DomainService(BaseClient client)
        {
            _baseClient = client;
            _baseClient.SetEndpoint(endpoint);
        }

        public async Task<IEnumerable<Domain>> GetAllDomainsAsync(AdditionalData? addData)
        {
            string addUrl = string.Empty;

            // Want to show some additional data?
            if (addData != null)
            {

                var additionalData = _parseAdditionalData((AdditionalData)addData);

                if (additionalData != null)
                {
                    addUrl += "&include=" + string.Join(",", additionalData);
                }
            }

            var response = await _baseClient.GetAsync<DomainsDto>(!string.IsNullOrEmpty(addUrl) ? "?" + addUrl.TrimStart('&') : "", null);

            if (response != null)
            {
                return response.Domains;
            }

            return new List<Domain>();
        }

        private List<string>? _parseAdditionalData(AdditionalData additionalData)
        {
            List<string> additionalDataList = new();

            if ((additionalData & AdditionalData.Nameservers) == AdditionalData.Nameservers)
            {
                additionalDataList.Add(AdditionalData.Nameservers.ToString().ToLower());
            }

            if ((additionalData & AdditionalData.Contacts) == AdditionalData.Contacts)
            {
                additionalDataList.Add(AdditionalData.Contacts.ToString().ToLower());
            }

            return additionalDataList.Count > 0 ? additionalDataList : null;
        }
    }
}

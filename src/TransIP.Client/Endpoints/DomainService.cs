using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using TransIP.Client.DataTransferObjects;
using TransIP.Client.Enums;
using TransIP.Client.Models;

namespace TransIP.Client.Endpoints
{
    public interface IDomainService
    {
        Task<IEnumerable<Domain>> GetAllDomainsAsync(AdditionalData? addData = null);
        Task<Domain> GetDomainAsync(string domainName, AdditionalData? addData = null);
        Task<IEnumerable<Nameserver>> GetNameservers(string domainName);
        Task<bool> SetNameservers(string domainName, IEnumerable<Nameserver> nameservers);
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

        public async Task<IEnumerable<Domain>> GetAllDomainsAsync(AdditionalData? addData = null)
        {
            Dictionary<string,string> urlQueryParameters = new Dictionary<string, string>();

            // Want to show some additional data?
            if (addData != null)
            {
                var additionalData = _parseAdditionalData((AdditionalData)addData);
                urlQueryParameters.Add("include", string.Join(",", additionalData));
            }

            var response = await _baseClient.GetAsync(urlParameters: urlQueryParameters);

            if (response.IsSuccessStatusCode)
            {
                string jsonResult = await response.Content.ReadAsStringAsync();

                var domainsDto = JsonSerializer.Deserialize<DomainsDto>(jsonResult, _baseClient.JsonOptions);
                return domainsDto?.Domains ?? new List<Domain>();
            }
            
            throw new Exception($"Received status code {response.StatusCode}, please refer to the TransIP API Documentation about this statuscode.");
        }

        public async Task<Domain> GetDomainAsync(string domainName, AdditionalData? addData = null)
        {
            Dictionary<string, string> urlQueryParameters = new Dictionary<string, string>();

            // Want to show some additional data?
            if (addData != null)
            {
                var additionalData = _parseAdditionalData((AdditionalData)addData);
                urlQueryParameters.Add("include", string.Join(",", additionalData));
            }

            var response = await _baseClient.GetAsync(domainName, urlQueryParameters);

            if (response.IsSuccessStatusCode)
            {
                string jsonResult = await response.Content.ReadAsStringAsync();

                var domainDto = JsonSerializer.Deserialize<DomainDto>(jsonResult, _baseClient.JsonOptions);
                return domainDto?.Domain;
            }
           
            throw new Exception($"Received status code {response.StatusCode}, please refer to the TransIP API Documentation about this statuscode.");
        }

        public async Task<IEnumerable<Nameserver>> GetNameservers(string domainName)
        {
            var response = await _baseClient.GetAsync(domainName + "/nameservers");

            if (response.IsSuccessStatusCode)
            {
                string jsonResult = await response.Content.ReadAsStringAsync();

                var nameserversDto = JsonSerializer.Deserialize<NameserversDto>(jsonResult, _baseClient.JsonOptions);
                return nameserversDto?.Nameservers ?? new List<Nameserver>();
            }
            
            throw new Exception($"Received status code {response.StatusCode}, please refer to the TransIP API Documentation about this statuscode.");
        }

        public async Task<bool> SetNameservers(string domainName, IEnumerable<Nameserver> nameservers)
        {
            var response = await _baseClient.PutAsync(domainName + "/nameservers", new NameserversDto { Nameservers = nameservers });

            if (response.IsSuccessStatusCode) // 204 Success, no content.
            {
                return true;
            }

            return false; // All other status codes.
        }

        private List<string> _parseAdditionalData(AdditionalData additionalData)
        {
            List<string> additionalDataList = new List<string>();

            if ((additionalData & AdditionalData.Nameservers) == AdditionalData.Nameservers)
            {
                additionalDataList.Add(AdditionalData.Nameservers.ToString().ToLower());
            }

            if ((additionalData & AdditionalData.Contacts) == AdditionalData.Contacts)
            {
                additionalDataList.Add(AdditionalData.Contacts.ToString().ToLower());
            }

            return additionalDataList;
        }
    }
}

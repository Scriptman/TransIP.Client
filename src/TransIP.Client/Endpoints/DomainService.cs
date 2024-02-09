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
        void SetDomain(string domain);
        Task<IEnumerable<Domain>> GetAllDomainsAsync(AdditionalData? addData = null);
        Task<Domain> GetDomainAsync(AdditionalData? addData = null);
        Task<IEnumerable<Nameserver>> GetNameserversAsync();
        Task<bool> SetNameserversAsync(IEnumerable<Nameserver> nameservers);
        Task<List<DnsEntry>> GetDnsEntriesAsync();
        Task<bool> AddDnsEntryAsync(DnsEntry dnsEntry);
        Task<bool> UpdateDnsEntryAsync(DnsEntry dnsEntry);
        Task<bool> DeleteDnsEntryAsync(DnsEntry dnsEntry);
    }
    public class DomainService : IDomainService, IEndpoint
    {
        private readonly BaseClient _baseClient;
        protected string _endpoint = "domains";

        private string _domainName = "";

        public DomainService(BaseClient client, string domainName = "")
        {
            _baseClient = client;
            _baseClient.SetEndpoint(_endpoint);

            _domainName = domainName;
        }

        public void SetDomain (string domainName)
        {
            _domainName = domainName;
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
            
            throw new Exception($"DomainService.GetAllDomainsAsync failed for domain {_domainName}. Received status code {response.StatusCode}, please refer to the TransIP API Documentation about this statuscode.");
        }

        public async Task<Domain> GetDomainAsync(AdditionalData? addData = null)
        {
            Dictionary<string, string> urlQueryParameters = new Dictionary<string, string>();

            // Want to show some additional data?
            if (addData != null)
            {
                var additionalData = _parseAdditionalData((AdditionalData)addData);
                urlQueryParameters.Add("include", string.Join(",", additionalData));
            }

            var response = await _baseClient.GetAsync(_domainName, urlQueryParameters);

            if (response.IsSuccessStatusCode)
            {
                string jsonResult = await response.Content.ReadAsStringAsync();

                var domainDto = JsonSerializer.Deserialize<DomainDto>(jsonResult, _baseClient.JsonOptions);
                return domainDto!.Domain;
            }
           
            throw new Exception($"DomainService.GetDomainAsync failed for domain {_domainName}. Received status code {response.StatusCode}, please refer to the TransIP API Documentation about this statuscode.");
        }

        public async Task<IEnumerable<Nameserver>> GetNameserversAsync()
        {
            var response = await _baseClient.GetAsync(_domainName + "/nameservers");

            if (response.IsSuccessStatusCode)
            {
                string jsonResult = await response.Content.ReadAsStringAsync();

                var nameserversDto = JsonSerializer.Deserialize<NameserversDto>(jsonResult, _baseClient.JsonOptions);
                return nameserversDto?.Nameservers ?? new List<Nameserver>();
            }
            
            throw new Exception($"DomainService.GetNameserversAsync failed for domain {_domainName}. Received status code {response.StatusCode}, please refer to the TransIP API Documentation about this statuscode.");
        }

        public async Task<bool> SetNameserversAsync(IEnumerable<Nameserver> nameservers)
        {
            var response = await _baseClient.PutAsync(_domainName + "/nameservers", new NameserversDto { Nameservers = nameservers });

            if (response.IsSuccessStatusCode) // 204 Success, no content.
            {
                return true;
            }

            return false; // All other status codes.
        }

        public async Task<List<DnsEntry>> GetDnsEntriesAsync()
        {
            var response = await _baseClient.GetAsync(_domainName + "/dns");

            if (response.IsSuccessStatusCode)
            {
                string jsonResult = await response.Content.ReadAsStringAsync();

                var dnsEntriesDto = JsonSerializer.Deserialize<DnsEntriesDto>(jsonResult, _baseClient.JsonOptions);
                return dnsEntriesDto?.DnsEntries ?? new List<DnsEntry>();
            }

            throw new Exception($"DomainService.GetDnsEntriesAsync failed for domain {_domainName}. Received status code {response.StatusCode}, please refer to the TransIP API Documentation about this statuscode.");
        }

        public async Task<bool> AddDnsEntryAsync(DnsEntry dnsEntry)
        {
            var response = await _baseClient.PostAsync(_domainName + "/dns", dnsEntry);

            if (response.IsSuccessStatusCode)
            {
                return true;
            }

            throw new Exception($"DomainService.AddDnsEntryAsync failed for domain {_domainName}. Received status code {response.StatusCode}, please refer to the TransIP API Documentation about this statuscode.");
        }

        public async Task<bool> UpdateDnsEntryAsync(DnsEntry dnsEntry)
        {
            var response = await _baseClient.PatchAsync(_domainName + "/dns", dnsEntry);

            if (response.IsSuccessStatusCode)
            {
                return true;
            }

            throw new Exception($"DomainService.UpdateDnsEntryAsync failed for domain {_domainName}. Received status code {response.StatusCode}, please refer to the TransIP API Documentation about this statuscode.");
        }

        public async Task<bool> ReplaceAllDnsEntriesAsync(IEnumerable<DnsEntry> dnsEntries)
        {
            var response = await _baseClient.PutAsync(_domainName + "/dns", dnsEntries);

            if (response.IsSuccessStatusCode)
            {
                return true;
            }

            throw new Exception($"DomainService.ReplaceAllDnsEntriesAsync failed for domain {_domainName}. Received status code {response.StatusCode}, please refer to the TransIP API Documentation about this statuscode.");
        }

        public async Task<bool> DeleteDnsEntryAsync(DnsEntry dnsEntry)
        {
            var response = await _baseClient.PatchAsync(_domainName + "/dns", dnsEntry);

            if (response.IsSuccessStatusCode)
            {
                return true;
            }

            throw new Exception($"DomainService.DeleteDnsEntryAsync failed for domain {_domainName}. Received status code {response.StatusCode}, please refer to the TransIP API Documentation about this statuscode.");
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

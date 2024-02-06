using System.Text.Json.Serialization;
using TransIP.Client.Enums;
using TransIP.Client.JsonConverters;

namespace TransIP.Client.Models
{
    public class Domain
    {
        public string Name { get; set; }
        public IEnumerable<Nameserver>? Nameservers { get; set; }
        public IEnumerable<Contact>? Contacts { get; set; }
        public string? AuthCode { get; set; }
        public bool IsTransferLocked { get; set; }
        public DateTime RegistrationDate { get; set; }
        public DateTime RenewalDate { get; set; }
        public bool IsWhitelabel { get; set; }
        public string? CancellationDate { get; set; }
        [JsonConverter(typeof(StringNullableEnumConverter<CancellationStatus>))]
        public CancellationStatus? CancellationStatus { get; set; }
        public bool IsDnsOnly { get; set; }
        public IEnumerable<string> Tags { get; set; }
        public bool CanEditDns { get; set; }
        public bool HasAutoDns { get; set; }
        public bool HasDnsSec { get; set; }
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public DomainStatus Status { get; set; }
    }
}

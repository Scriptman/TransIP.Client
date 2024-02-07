using System.Text.Json.Serialization;
using TransIP.Client.Enums;

namespace TransIP.Client.Models
{
    public class Contact
    {
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public ContactType Type { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string? CompanyName { get; set; }
        public string? CompanyKvk { get; set; }
        public string? CompanyType { get; set; }
        public string Street { get; set; }
        public string Number { get; set; }
        public string PostalCode { get; set; }
        public string City { get; set; }
        public string PhoneNumber { get; set; }
        public string? FaxNumber { get; set; }
        public string Email { get; set; }
        public string Country { get; set; }
    }
}

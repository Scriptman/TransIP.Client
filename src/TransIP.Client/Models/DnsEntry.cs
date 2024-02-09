using System.Text.Json.Serialization;
using TransIP.Client.Enums;

namespace TransIP.Client.Models
{
    public class DnsEntry
    {
        public string Name { get; set; }
        public int Expire { get; set; }
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public DnsEntryType Type { get; set; }
        public string Content { get; set; }
    }
}

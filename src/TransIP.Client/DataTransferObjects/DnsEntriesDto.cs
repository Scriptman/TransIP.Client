using System.Collections.Generic;
using TransIP.Client.Models;

namespace TransIP.Client.DataTransferObjects
{
    public class DnsEntriesDto
    {
        public IEnumerable<DnsEntry> DnsEntries { get; set; }
    }
}

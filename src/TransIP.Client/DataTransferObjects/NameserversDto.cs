using System.Collections.Generic;
using TransIP.Client.Models;

namespace TransIP.Client.DataTransferObjects
{
    public class NameserversDto
    {
        public IEnumerable<Nameserver> Nameservers { get; set; }
    }
}

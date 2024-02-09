namespace TransIP.Client.Models
{
    public class DnsEntry
    {
        public string Name { get; set; }
        public int Expire { get; set; }
        public string Type { get; set; }
        public string Content { get; set; }
    }
}

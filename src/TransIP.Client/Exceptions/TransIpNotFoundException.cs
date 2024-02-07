using System;

namespace TransIP.Client.Exceptions
{
    public class TransIpNotFoundException : Exception
    {
        public TransIpNotFoundException(string? message) : base(message)
        {
        }
    }
}
